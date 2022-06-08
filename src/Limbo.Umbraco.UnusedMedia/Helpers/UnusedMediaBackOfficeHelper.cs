using System;
using System.Collections.Generic;
using System.Linq;
using Limbo.Umbraco.UnusedMedia.Models.Api;
using Limbo.Umbraco.UnusedMedia.Models.References;
using Limbo.Umbraco.UnusedMedia.Services;
using Microsoft.AspNetCore.Http;
using Skybrud.Essentials.Reflection;
using Skybrud.Essentials.Strings;
using Skybrud.Forms.Models.Fields;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Dashboards;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Helpers {

    public class UnusedMediaBackOfficeHelper {

        private readonly IUserService _userService;
        private readonly ILocalizedTextService _localizedTextService;
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly UnusedMediaService _unusedMediaService;

        #region Properties

        public int DefaultListLimit { get; protected set; }

        #endregion

        #region Constructors

        public UnusedMediaBackOfficeHelper(IUserService userService, ILocalizedTextService localizedTextService, IUmbracoContextAccessor umbracoContextAccessor, UnusedMediaService unusedMediaService) {

            _userService = userService;
            _localizedTextService = localizedTextService;
            _umbracoContextAccessor = umbracoContextAccessor;
            _unusedMediaService = unusedMediaService;

            DefaultListLimit = 15;

        }

        #endregion

        #region Public member methods

        /// <summary>
        /// Returns a cache busting value that can be used for views and other resources from this package throughout
        /// the backoffice.
        /// </summary>
        /// <returns>The cache busting value.</returns>
        public string GetCacheBuster() {
            return ReflectionUtils.GetInformationalVersion(GetType().Assembly);
        }

        /// <summary>
        /// Returns the access rules for <see cref="UnusedMediaBackOfficeHelper"/>.
        /// </summary>
        /// <returns>An array of <see cref="IAccessRule"/>.</returns>
        public virtual IAccessRule[] GetDashboardAccessRules() {
            return Array.Empty<IAccessRule>();
        }

        /// <summary>
        /// Returns a list of the filters to shown in the dashboard.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="currentUser">The current user.</param>
        /// <returns>A collection of <see cref="FieldBase"/> representing the filters.</returns>
        public virtual IEnumerable<FieldBase> GetFilters(HttpContext context, IUser currentUser) {

            List<FieldBase> filters = new List<FieldBase>();

            AppendTextFilter(context, currentUser, filters);
            AppendFoldersFilters(context, currentUser, filters);
            AppendCreatorsAndWritersFilters(context, currentUser, filters);

            return filters;

        }

        /// <summary>
        /// Initializes and returns a new instance of <see cref="UnusedMediaOptions"/> based on the specified HTTP <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="currentUser">The current user.</param>
        /// <returns>An instance of <see cref="UnusedMediaOptions"/>.</returns>
        public virtual UnusedMediaOptions GetOptions(HttpContext context, IUser currentUser) {

            int limit = StringUtils.ParseInt32(context.Request.Query["limit"]);
            if (limit <= 0) {
                limit = DefaultListLimit;
            }

            int page = Math.Max(StringUtils.ParseInt32(context.Request.Query["page"]), 1);

            return new UnusedMediaOptions {
                Text = context.Request.Query["text"],
                Path = StringUtils.ParseInt32Array(context.Request.Query["path"]),
                CreatorIds = StringUtils.ParseInt32Array(context.Request.Query["creatorIds"]),
                WriterIds = StringUtils.ParseInt32Array(context.Request.Query["writerIds"]),
                Limit = limit,
                Page = page
            };

        }

        #endregion

        #region Protected member methods

        /// <summary>
        /// Appends the text filter to <paramref name="filters"/>. The method can be overriden to change the default behaviour.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="currentUser">The current user.</param>
        /// <param name="filters">The list of filters.</param>
        protected virtual void AppendTextFilter(HttpContext context, IUser currentUser, List<FieldBase> filters) {
            filters.Add(new TextField("text") {
                Placeholder = _localizedTextService.Localize(null, "typeToSearch")
            });
        }

        /// <summary>
        /// Appends the folders filter to <paramref name="filters"/>. The method can be overriden to change the default behaviour.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="currentUser">The current user.</param>
        /// <param name="filters">The list of filters.</param>
        protected virtual void AppendFoldersFilters(HttpContext context, IUser currentUser, List<FieldBase> filters) {

            // Initialize the list with an item for an empty selection
            List<ListItem> items = new List<ListItem> {
                new ListItem("", _localizedTextService.Localize("unusedMedia", "selectFolder"))
            };

            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext)) {
                return;
            }

            // Iterate through all media at the root level
            foreach (IPublishedContent level1 in umbracoContext.Media.GetAtRoot()) {

                // Ignore if not a folder
                if (level1.ContentType.Alias != Constants.Conventions.MediaTypes.Folder) {
                    continue;
                }

                // Append an item for the folder
                items.Add(new ListItem(level1.Id, level1.Name));

                // Append child folders as well
                AppendChildren(items, level1, 2);

            }

            // Initialize and append the filter
            filters.Add(new DropDownList("path") {
                Items = items
            });

        }

        /// <summary>
        /// Appends child folders of <paramref name="parent"/> to <paramref name="items"/>. The method is recursive,
        /// meaning that this method will also be called for each child until <paramref name="levels"/> is reached.
        ///
        /// The method can be overriden to change the default behaviour.
        /// </summary>
        /// <param name="items">The list of items to which the children will be aded.</param>
        /// <param name="parent">The parent media.</param>
        /// <param name="levels">The maximum level or depth to append folders for.</param>
        protected virtual void AppendChildren(List<ListItem> items, IPublishedContent parent, int levels) {

            if (parent.Level == levels) {
                return;
            }

            foreach (IPublishedContent child in parent.Children) {

                // Skip if not a folder
                if (child.ContentType.Alias != Constants.Conventions.MediaTypes.Folder) {
                    continue;
                }

                string name = child.Name;

                // Prepend dashes to the name to visualize the tree structure
                for (int i = 2; i <= child.Level; i++) {
                    name = "-- " + name;
                }

                items.Add(new ListItem(child.Id, name));

                // Run through the child's children
                AppendChildren(items, child, levels);

            }

        }

        /// <summary>
        /// Appends both a creators filter and a writers filter to <paramref name="filters"/>.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="currentUser">The current user.</param>
        /// <param name="filters">The list of filters.</param>
        protected virtual void AppendCreatorsAndWritersFilters(HttpContext context, IUser currentUser, List<FieldBase> filters) {

            List<ListItem> creators = new List<ListItem>();
            List<ListItem> writers = new List<ListItem>();

            creators.Add(new ListItem("", _localizedTextService.Localize("unusedMedia", "createdBy")));
            creators.Add(new ListItem(currentUser.Id, _localizedTextService.Localize("unusedMedia", "me")));

            writers.Add(new ListItem("", _localizedTextService.Localize("unusedMedia", "updatedBy")));
            writers.Add(new ListItem(currentUser.Id, _localizedTextService.Localize("unusedMedia", "me")));

            foreach (IUser user in GetUsers(context, currentUser)) {
                if (currentUser.Id == user.Id) {
                    continue;
                }

                creators.Add(new ListItem(user.Id, user.Name));
                writers.Add(new ListItem(user.Id, user.Name));
            }

            filters.Add(new DropDownList("creatorIds") {
                Items = creators
            });

            filters.Add(new DropDownList("writerIds") {
                Items = writers
            });

        }

        /// <summary>
        /// Returns a list of users to be shown in the unsued media dashboard.
        ///
        /// Override the method to control which users are shown. Default is all active users, sorted by their name in
        /// ascending order.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="currentUser">The current user.</param>
        /// <returns>An instance of <see cref="IEnumerable{IUser}"/> containing the users to be shown.</returns>
        protected virtual IEnumerable<IUser> GetUsers(HttpContext context, IUser currentUser) {
            return _userService
                .GetAll(0, int.MaxValue, out _)
                .Where(x => x.UserState == UserState.Active)
                .OrderBy(x => x.Name);
        }

        /// <summary>
        /// Returns the response of a request to get references of the specified <paramref name="child"/> media.
        /// </summary>
        /// <param name="child">The child of the relation/reference.</param>
        /// <param name="user">The current backoffice user.</param>
        /// <returns>An instance of <see cref="DeleteMediaResponse"/>.</returns>
        public virtual DeleteMediaResponse GetReferencesByChild(IMedia child, IUser user) {

            ReferenceResult result = _unusedMediaService.GetReferencesByChild(child, user);

            return new DeleteMediaResponse(result);

        }

        #endregion

    }
}