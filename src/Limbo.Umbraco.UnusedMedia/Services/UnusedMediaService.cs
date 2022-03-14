using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Limbo.Umbraco.UnusedMedia.Extensions;
using Limbo.Umbraco.UnusedMedia.Models;
using Limbo.Umbraco.UnusedMedia.Models.References;
using Limbo.Umbraco.UnusedMedia.Models.Used;
using Lucene.Net.Support;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Skybrud.Essentials.Json;
using Skybrud.Essentials.Json.Extensions;
using Skybrud.Essentials.Strings.Extensions;
using Skybrud.Essentials.Time;
using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Editors;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using Umbraco.Web;

// ReSharper disable AssignNullToNotNullAttribute

namespace Limbo.Umbraco.UnusedMedia.Services {
    
    public class UnusedMediaService {
        
        private readonly IRelationService _relationService;
        private readonly Lazy<PropertyEditorCollection> _propertyEditors;
        private readonly DataValueReferenceFactoryCollection _dataValueReferenceFactories;
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        #region Constructors

        public UnusedMediaService(IRelationService relationService,
            Lazy<PropertyEditorCollection> propertyEditors,
            DataValueReferenceFactoryCollection dataValueReferenceFactories,
            IUmbracoContextAccessor umbracoContextAccessor) {
            
            _relationService = relationService;
            _propertyEditors = propertyEditors;
            _dataValueReferenceFactories = dataValueReferenceFactories;
            _umbracoContextAccessor = umbracoContextAccessor;

        }

        #endregion

        #region Member methods

        /// <summary>
        /// Returns an new <see cref="ContentCacheUsedMediaReport"/> based on the media references in the content
        /// cache. This report tracks which media are currently in use - which can then later be used for determining
        /// which media are not in use.
        ///
        /// For complex, we can't rely on Umbraco internal media tracking, so this method is a replacement (or an
        /// alternative) for that. As running through the content cache may take too long for it to be acceptable to
        /// use directly in the dashboard, the report is saved to disk, and then loaded for each time time the
        /// dashboard requests a list of unused media.
        /// </summary>
        /// <returns>An instance of <see cref="ContentCacheUsedMediaReport"/> representing the generated report.</returns>
        public virtual ContentCacheUsedMediaReport BuildReportFromContentCache() {
            
            EssentialsTime start = EssentialsTime.UtcNow;

            Stopwatch sw1 = Stopwatch.StartNew();

            Dictionary<Guid, HashSet<Guid>> mediaToContent = new Dictionary<Guid, HashSet<Guid>>();

            // Start by iterating through the root nodes of the content cache
            foreach (IPublishedContent root in _umbracoContextAccessor.UmbracoContext.Content.GetAtRoot()) {

                // Get all descendant nodes, including the rode node it self
                foreach (IPublishedContent content in root.DescendantsOrSelf()) {

                    // Call the GetAllReferences method from this package to find all media references in "content"
                    foreach (UmbracoEntityReference reference in GetAllReferences(content)) {

                        // For now, this logic only support GuidUdi's, so we should throw an exception if we encounter any other types
                        if (reference.Udi is not GuidUdi guidUdi) throw new Exception($"UDI of type {reference.Udi.GetType()} not supported on page with key {content.Key}.");

                        switch (reference.Udi.EntityType) {

                            case Constants.UdiEntityType.Media:
                                if (!mediaToContent.TryGetValue(guidUdi.Guid, out HashSet<Guid> list)) {
                                    mediaToContent.Add(guidUdi.Guid, list = new HashSet<Guid>());
                                }
                                list.Add(content.Key);
                                break;
                            
                        }

                    }

                }

            }
            
            sw1.Stop();

            EssentialsTime completed = EssentialsTime.UtcNow;

            // Initialize a new report from the information gathered above
            ContentCacheUsedMediaReport report = new ContentCacheUsedMediaReport(start, completed, sw1.Elapsed, mediaToContent);

            // TODO: Should the file name include a timestamp so the history is kept on disk?

            string path = IOHelper.MapPath($"{UnusedMediaConstans.Directories.AppData}/ContentCacheUnusedMediaReport.json");

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            JsonUtils.SaveJsonObject(path, JObject.FromObject(report), Formatting.Indented);

            return report;

        }

        /// <summary>
        /// Loads the most recent <see cref="ContentCacheUsedMediaReport"/>. If a report has not yet been generated, a <see cref="FileNotFoundException"/> will be thrown.
        /// </summary>
        /// <returns>An instance of <see cref="ContentCacheUsedMediaReport"/> representing most recent report.</returns>
        public virtual ContentCacheUsedMediaReport LoadContentCacheMediaReport() {
            
            string path = IOHelper.MapPath($"{UnusedMediaConstans.Directories.AppData}/ContentCacheUnusedMediaReport.json");

            if (!System.IO.File.Exists(path)) BuildReportFromContentCache();

            return JsonUtils.LoadJsonObject(path, x => {
                EssentialsTime start = x.GetString("start", EssentialsTime.Parse);
                EssentialsTime completed = x.GetString("completed", EssentialsTime.Parse);
                TimeSpan duration = x.GetDouble("duration", TimeSpan.FromSeconds);
                Dictionary<Guid, HashSet<Guid>> media = x.GetValue("media").ToObject<Dictionary<Guid, HashSet<Guid>>>();
                return new ContentCacheUsedMediaReport(start, completed, duration, media);
            });

        }

        /// <summary>
        /// Returns a hash set containing the numeric IDs of all media that are currently in use.
        ///
        /// By default, result only looks at content to media relations. If a given solution contains other references
        /// to media - eg. via our redirects package - this method should be overridden to handle that.
        /// </summary>
        /// <returns>An instance of <see cref="HashSet{T}"/>.</returns>
        public virtual HashSet<int> GetAllMediaIdsInUse() {

            // Look up the build-in relation type
            IRelationType releationType = _relationService.GetRelationTypeByAlias("umbMedia");
            if (releationType == null) throw new Exception("Relation type with alias \"umbMedia\" not found.");
            
            // Create a new hash set of media that are already in use
            HashSet<int> usedMedia = _relationService
                .GetAllRelationsByRelationType(releationType.Id)
                .GroupBy(x => x.ChildId)
                .ToHashSet(x => x.Key);

            return usedMedia;

        }

        public virtual IUsedMediaReport GetUsedMediaReport() {

            return new UsedMediaReport(LoadContentCacheMediaReport());

        }

        public virtual UnusedMediaResult GetUnusedMedia(UnusedMediaOptions options) {

            options ??= new UnusedMediaOptions();
            
            // Load a report for 
            IUsedMediaReport report = GetUsedMediaReport();

            int total = 0;

            List<IPublishedContent> temp = new List<IPublishedContent>();

            foreach (IPublishedContent media in _umbracoContextAccessor.UmbracoContext.Media.GetAtRoot()) {

                // Skip media if a part of their path is ignored
                if (options.IgnoredFolderIds is { Count: > 0 }) {
                    if (media.Path.ToInt32Array().Any(x => options.IgnoredFolderIds.Contains(x))) {
                        continue;
                    }
                }

                // Handle non-folder media types at the root level
                if (IsMatch(media, options)) {

                    // Increment the total count regardless if the media is in use or not
                    total++;

                    // Append the media to the list of not in use
                    if (!report.IsInUse(media)) temp.Add(media);

                }

                // Iterate through all the descendants
                foreach (IPublishedContent descendant in media.Descendants()) {

                    if (!IsMatch(descendant, options)) continue;

                    // Skip if a folder
                    if (descendant.ContentType.Alias == Constants.Conventions.MediaTypes.Folder) continue;
                    
                    // Increment the total count regardless of if the media is in use or not
                    total++;

                    // Skip if in use
                    if (report.IsInUse(descendant)) continue;

                    temp.Add(descendant);

                }

            }

            int limit = options.Limit;
            int unused = temp.Count;
            int pages = (int) Math.Ceiling(unused / (double) limit);
            int page = Math.Max(options.Page, 1);

            int offset = (page - 1) * options.Limit;


            IEnumerable<UnusedMediaItem> items = temp.Skip(offset).Take(options.Limit).Select(CreateItem);

            var summary = new UserMediaReportSummary(report);

            return new UnusedMediaResult(total, unused, limit, offset, page, pages, summary, items);

        }

        /// <summary>
        /// Virtual method for determing which media items match the specified <paramref name="options"/>.
        /// </summary>
        /// <param name="media">The media to check.</param>
        /// <param name="options">The options.</param>
        /// <returns><c>true</c> if <paramref name="media"/> matches <paramref name="options"/>; otherwise <c>false</c>.</returns>
        protected virtual bool IsMatch(IPublishedContent media, UnusedMediaOptions options) {
            
            // Always ignore folders
            if (media.ContentType.Alias == Constants.Conventions.MediaTypes.Folder) return false;

            // Ignore media not within "Path" if the filter is specified
            if (options.HasPath &&  !media.Path.ToInt32Array().Any(options.IsInPath)) return false;

            if (options.HasCreatorIds && !options.HasCreator(media.CreatorId)) return false;
            if (options.HasWriterIds && !options.HasWriter(media.WriterId)) return false;
            
            // Ignore media whose names does not include the specified text
            if (!string.IsNullOrWhiteSpace(options.Text) && !media.Name.InvariantContains(options.Text)) return false;

            return true;

        }

        protected virtual UnusedMediaItem CreateItem(IPublishedContent media) {
            return new UnusedMediaItem(media);
        }

        public IEnumerable<UmbracoEntityReference> GetAllReferences(IContent content) {
            return _dataValueReferenceFactories.GetAllReferences(content.Properties, _propertyEditors.Value);
        }

        /// <summary>
        /// Returns a colletion of all media UDI references of the specified <paramref name="content"/> item.
        /// </summary>
        /// <param name="content">The content item to check for media UDI references.</param>
        /// <returns>An instance of <see cref="IEnumerable{UmbracoEntityReference}"/> with the found references.</returns>
        public virtual IEnumerable<UmbracoEntityReference> GetAllReferences(IPublishedElement content) {

            List<UmbracoEntityReference> references = new List<UmbracoEntityReference>();

            foreach (var property in content.Properties) {

                GetReferences(content, property, references);

            }

            return references;

        }

        /// <summary>
        /// Appends any media UDI references in the value of <paramref name="property"/> to <paramref name="references"/>.
        /// </summary>
        /// <param name="owner">The parent <see cref="IPublishedElement"/> of <paramref name="property"/>.</param>
        /// <param name="property">The property to check for media UDI references.</param>
        /// <param name="references">The list to which the references should be added.</param>
        /// <remarks>The default implemention of this method will convert the property value to a string value, and
        /// then processs it for UDI references. If you need to handle advanced property editors, you can override this
        /// method - eg. such as:
        ///
        /// <code>
        /// protected override void GetReferences(IPublishedElement owner, IPublishedProperty property, List&lt;UmbracoEntityReference&gt; references) {
        ///     switch (property.PropertyType.EditorAlias) {
        ///         case Umbraco.Core.Constants.PropertyEditors.Aliases.Grid:
        ///             GetReferencesFromGrid(owner, property, references);
        ///             break;
        ///         default:
        ///             base.GetReferences(owner, property, references);
        ///             break;
        ///     }
        /// }
        /// </code>
        /// </remarks>
        protected virtual void GetReferences(IPublishedElement owner, IPublishedProperty property, List<UmbracoEntityReference> references) {
            string value = property.GetSourceValue()?.ToString() ?? string.Empty;
            GetReferences(value, references);
        }

        /// <summary>
        /// Appends any media UDI references in <paramref name="value"/> to <paramref name="references"/>.
        /// </summary>
        /// <param name="value">The string value to check for media UDI references.</param>
        /// <param name="references">The list to which the references should be added.</param>
        protected virtual void GetReferences(string value, List<UmbracoEntityReference> references) {
        
            if (string.IsNullOrWhiteSpace(value)) return;

            foreach (Match image in Regex.Matches(value, @"(umb:\/\/media\/[0-9A-Fa-f]{32})")) {
                if (Udi.TryParse(image.Value, out Udi udi)) {
                    references.Add(new UmbracoEntityReference(udi));
                }
            }

        }
        
        /// <summary>
        /// Returns a list of content items which references the specified media<paramref name="child"/>.
        /// </summary>
        /// <param name="child">The media child.</param>
        /// <returns>An array of <see cref="ContentReference"/> representing the content items that references <paramref name="child"/>.</returns>
        public virtual ContentReference[] GetContentReferencesByChild(IMedia child) {

            // Get the relations tracked by Umbraco
            IEnumerable<IPublishedContent> umbracoRelations = _relationService
                .GetByChildId(child.Id, Constants.Conventions.RelationTypes.RelatedMediaAlias)
                .Select(x => _umbracoContextAccessor.UmbracoContext.Content.GetById(x.ParentId))
                .WhereNotNull();

            // Gets the references from our unused media package
            IEnumerable<IPublishedContent> limboReferences =  LoadContentCacheMediaReport()
                .GetContentKeys(child).Select(x => _umbracoContextAccessor.UmbracoContext.Content.GetById(x))
                .WhereNotNull();

            // Merge the two result sets
            return umbracoRelations.Union(limboReferences)
                .Select(x => new ContentReference(x))
                .ToArray();

        }

        public virtual ReferenceResult GetReferencesByChild(IMedia child, IUser user) {

            List<ReferenceGroup> temp = new EquatableList<ReferenceGroup>();

            ContentReference[] content = GetContentReferencesByChild(child);

            if (content.Any()) {
                temp.Add(new ReferenceGroup {
                    Type = "content",
                    Name = "Content",
                    View = $"{UnusedMediaConstans.Urls.AppPlugins}Views/References/Content.html?v={UnusedMediaConstans.Version}",
                    References = content
                });
            }
            
            return new ReferenceResult(temp);

        }

        #endregion

    }

}