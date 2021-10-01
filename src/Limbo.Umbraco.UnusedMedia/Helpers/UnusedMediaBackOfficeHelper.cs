using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Limbo.Umbraco.UnusedMedia.Services;
using Skybrud.Essentials.Strings;
using Skybrud.Forms.Models.Fields;
using Umbraco.Core;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;

namespace Limbo.Umbraco.UnusedMedia.Helpers {
    
    public class UnusedMediaBackOfficeHelper {

        private readonly IUserService _userService;
        private readonly ILocalizedTextService _localizedTextService;

        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public UnusedMediaBackOfficeHelper(IUserService userService, ILocalizedTextService localizedTextService, IUmbracoContextAccessor umbracoContextAccessor) {
            _userService = userService;
            _localizedTextService = localizedTextService;
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        public virtual IEnumerable<FieldBase> GetFilters(HttpContextBase context, IUser currentUser) {

            List<FieldBase> fields = new List<FieldBase> {
                new TextField("text") {
                    Placeholder = _localizedTextService.Localize("typeToSearch")
                }
            };

            if (TryGetFolders(context, currentUser, out List<ListItem> items)) {
                fields.Add(new DropDownList("path") {
                    Items = items
                });
            }
            
            List<ListItem> creators = new List<ListItem>();
            List<ListItem> writers = new List<ListItem>();
            
            creators.Add(new ListItem("", _localizedTextService.Localize("unusedMedia/createdBy")));
            creators.Add(new ListItem(currentUser.Id, _localizedTextService.Localize("unusedMedia/me")));
            
            writers.Add(new ListItem("", _localizedTextService.Localize("unusedMedia/updatedBy")));
            writers.Add(new ListItem(currentUser.Id, _localizedTextService.Localize("unusedMedia/me")));

            foreach (IUser user in GetUsers(context, currentUser)) {
                if (currentUser.Id == user.Id) continue;
                creators.Add(new ListItem(user.Id, user.Name));
                writers.Add(new ListItem(user.Id, user.Name));
            }
            
            fields.Add(new DropDownList("creatorIds") {
                Items = creators
            });

            fields.Add(new DropDownList("writerIds") {
                Items = writers
            });

            return fields;

        }

        public string GetCacheBuster() {
            return Guid.NewGuid().ToString();
        }

        public virtual UnusedMediaOptions GetOptions(HttpContextBase context, IUser currentUser) {

            int limit = StringUtils.ParseInt32(context.Request.QueryString["limit"]);
            if (limit <= 0) limit = 15;

            int page = Math.Max(StringUtils.ParseInt32(context.Request.QueryString["page"]), 1);
            
            return new UnusedMediaOptions {
                Text = context.Request.QueryString["text"],
                Path = StringUtils.ParseInt32Array(context.Request.QueryString["path"]),
                CreatorIds = StringUtils.ParseInt32Array(context.Request.QueryString["creatorIds"]),
                WriterIds = StringUtils.ParseInt32Array(context.Request.QueryString["writerIds"]),
                Limit = limit,
                Page = page
            };

        }

        protected virtual bool TryGetFolders(HttpContextBase context, IUser currentUser, out List<ListItem> result) {
            
            result = new List<ListItem> {
                new ListItem("", _localizedTextService.Localize("unusedMedia/selectFolder"))
            };

            foreach (var level1 in _umbracoContextAccessor.UmbracoContext.Media.GetAtRoot()) {

                if (level1.ContentType.Alias != Constants.Conventions.MediaTypes.Folder) continue;

                result.Add(new ListItem(level1.Id, level1.Name));

                AppendChildren(result, level1, 2);

            }
            
            result.AddRange(_umbracoContextAccessor.UmbracoContext.Media.GetAtRoot().Select(x => new ListItem(x.Id, x.Name)));

            return true;

        }

        protected virtual void AppendChildren(List<ListItem> items, IPublishedContent parent, int levels) {

            if (parent.Level == levels) return;

            foreach (IPublishedContent child in parent.Children) {

                // Skip if not a folder
                if (child.ContentType.Alias != Constants.Conventions.MediaTypes.Folder) continue;

                string name = child.Name;

                // Prepend dashes to the name to visualize the tree structure
                for (int i = 2; i <= child.Level; i++) name = "-- " + name;
                
                items.Add(new ListItem(child.Id, name));

                // Run through the child's children
                AppendChildren(items, child, levels);

            }

        }

        protected virtual IEnumerable<IUser> GetUsers(HttpContextBase context, IUser currentUser) {
            return _userService
                .GetAll(0, int.MaxValue, out _)
                .Where(x => x.UserState == UserState.Active)
                .OrderBy(x => x.Name);
        }

    }

}