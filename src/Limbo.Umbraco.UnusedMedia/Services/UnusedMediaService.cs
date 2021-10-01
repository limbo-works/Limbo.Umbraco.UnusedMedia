using System;
using System.Collections.Generic;
using System.Linq;
using Limbo.Umbraco.UnusedMedia.Extensions;
using Limbo.Umbraco.UnusedMedia.Models;
using Skybrud.Essentials.Strings.Extensions;
using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;

namespace Limbo.Umbraco.UnusedMedia.Services {
    
    public class UnusedMediaService {
        
        private readonly IRelationService _relationService;
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public UnusedMediaService(IRelationService relationService, IUmbracoContextAccessor umbracoContextAccessor) {
            _relationService = relationService;
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        public virtual UnusedMediaResult GetUnusedMedia(UnusedMediaOptions options) {

            if (options == null) options = new UnusedMediaOptions();

            // Convert the "Path" option to a hash set
            HashSet<int> path = new HashSet<int>(options.Path ?? Array.Empty<int>());
            HashSet<int> creatorIds = new HashSet<int>(options.CreatorIds ?? Array.Empty<int>());
            HashSet<int> writerIds = new HashSet<int>(options.WriterIds ?? Array.Empty<int>());
            
            // Get the "Text" parameter
            string text = options?.Text;

            // Create a new hash set of media that are already in use
            HashSet<int> usedMedia = _relationService
                .GetAllRelationsByRelationType(4)
                .GroupBy(x => x.ChildId)
                .ToHashSet(x => x.Key);

            int total = 0;

            List<IPublishedContent> temp = new List<IPublishedContent>();

            foreach (IPublishedContent media in _umbracoContextAccessor.UmbracoContext.Media.GetAtRoot()) {

                // Handle non-folder media types at the root level
                if (IsMatch(media, path, creatorIds, writerIds, text)) {

                    // Increment the total count regardless if the media is in use or not
                    total++;

                    // Append the media to the list of not in use
                    if (!usedMedia.Contains(media.Id)) temp.Add(media);

                }

                // Iterate through all the descendants
                foreach (IPublishedContent descendant in media.Descendants()) {

                    if (!IsMatch(descendant, path, creatorIds, writerIds, text)) continue;

                    // Skip if a folder
                    if (descendant.ContentType.Alias == Constants.Conventions.MediaTypes.Folder) continue;
                    
                    // Increment the total count regardless if the media is in use or not
                    total++;

                    // Skip if in use
                    if (usedMedia.Contains(descendant.Id)) continue;

                    temp.Add(descendant);

                }

            }

            int limit = options.Limit;
            int pages = (int) Math.Ceiling(total / (double) limit);
            int page = Math.Max(options.Page, 1);

            int offset = (page - 1) * options.Limit;

            int unused = temp.Count;

            IEnumerable<UnusedMediaItem> items = temp.Skip(offset).Take(options.Limit).Select(CreateItem);

            return new UnusedMediaResult(total, unused, limit, offset, page, pages, items);

        }

        protected virtual bool IsMatch(IPublishedContent media, HashSet<int> path, HashSet<int> creatorIds, HashSet<int> writerIds, string text) {
            
            // Always ignore folders
            if (media.ContentType.Alias == Constants.Conventions.MediaTypes.Folder) return false;

            // Ignore media not within "Path" if the filter is specified
            if (path.Count > 0 && !media.Path.ToInt32Array().Any(path.Contains)) return false;

            if (creatorIds.Count > 0 && !creatorIds.Contains(media.CreatorId)) return false;
            if (writerIds.Count > 0 && !writerIds.Contains(media.WriterId)) return false;
            
            // Ignore media whose names does not include the specified text
            if (!string.IsNullOrWhiteSpace(text) && !media.Name.InvariantContains(text)) return false;

            return true;

        }

        protected virtual UnusedMediaItem CreateItem(IPublishedContent media) {
            return new UnusedMediaItem(media);
        }

    }

}