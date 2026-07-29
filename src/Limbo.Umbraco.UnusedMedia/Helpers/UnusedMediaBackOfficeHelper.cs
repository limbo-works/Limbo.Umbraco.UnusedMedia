// [CHANGE: Umbraco 17 upgrade - Management API, System.Text.Json, ensured UmbracoContext, client side localization] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Text.Json.Serialization;
using Examine;
using Limbo.Umbraco.UnusedMedia.Models;
using Limbo.Umbraco.UnusedMedia.Models.Filters;
using Limbo.Umbraco.UnusedMedia.Models.Reports;
using Limbo.Umbraco.UnusedMedia.Models.Settings;
using Limbo.Umbraco.UnusedMedia.Models.Sites;
using Limbo.Umbraco.UnusedMedia.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Skybrud.Essentials.Collections;
using Skybrud.Essentials.Collections.Enumerables.Extensions;
using Skybrud.Essentials.Guids;
using Skybrud.Essentials.Strings;
using Skybrud.Essentials.Strings.Extensions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Helpers;

public class UnusedMediaBackOfficeHelper {

    private readonly UnusedMediaBackOfficeHelperDependencies _dependencies;

    private readonly IUserService _userService;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly UnusedMediaService _unusedMediaService;
    private readonly IExamineManager _examineManager;

    #region Properties

    public UnusedMediaSettings Settings => _dependencies.Settings;

    #endregion

    #region Constructors

    public UnusedMediaBackOfficeHelper(UnusedMediaBackOfficeHelperDependencies dependencies) {
        _dependencies = dependencies;
        _userService = dependencies.UserService;
        _umbracoContextFactory = dependencies.UmbracoContextFactory;
        _serviceScopeFactory = dependencies.ServiceScopeFactory;
        _unusedMediaService = dependencies.UnusedMediaService;
        _examineManager = dependencies.ExamineManager;
    }

    #endregion

    #region Public member methods

    public virtual IReadOnlyList<UnusedSiteItem> GetSites() {
        return [];
    }

    /// <summary>
    /// Returns whether the specified <paramref name="user"/> is allowed to use the unused media dashboard, according
    /// to the <c>Dashboard:AllowedGroups</c> setting. If no groups are configured, all backoffice users with access
    /// to the content section are allowed.
    ///
    /// In Umbraco 13 this was enforced through <c>IDashboard.AccessRules</c>, which only hid the dashboard in the UI.
    /// That interface no longer exists in Umbraco 17, so the check now lives here and is enforced by the Management
    /// API controller on every endpoint.
    /// </summary>
    /// <param name="user">The user to check.</param>
    /// <returns><see langword="true"/> if <paramref name="user"/> is allowed; otherwise <see langword="false"/>.</returns>
    public virtual bool IsAllowed(IUser user) {
        List<string> allowedGroups = Settings.Dashboard.AllowedGroups;
        if (allowedGroups.Count == 0) return true;
        return user.Groups.Any(x => allowedGroups.Contains(x.Alias, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Initializes and returns a new instance of <see cref="UnusedMediaOptions"/> based on the specified HTTP <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The current HTTP request.</param>
    /// <param name="currentUser">The current user.</param>
    /// <returns>An instance of <see cref="UnusedMediaOptions"/>.</returns>
    public virtual UnusedMediaOptions CreateOptions(HttpRequest request, IUser currentUser) {

        // [CHANGE: code review fix - a "Dashboard:PerPage" of 0 (or negative) made "GetUnusedMedia" divide by zero
        // when calculating the page count, and "Take(0)" returned an empty list] Related: Controllers/BackOffice/UnusedMediaBackOfficeController.cs
        int limit = GetInt32(request, "limit");
        if (limit <= 0) limit = Settings.Dashboard.PerPage;
        if (limit <= 0) limit = 15;

        int page = Math.Max(GetInt32(request, "page"), 1);

        // Columns carry a localization key and an English fallback. Server side localization via
        // ILocalizedTextService was dropped in the Umbraco 17 upgrade - the new backoffice localizes in the client.
        List<UnusedMediaColumn> columns = [
            new("name", "Name", "unusedMedia_name", UnusedMediaColumnType.Name, allowSort: true, defaultOrder: SortOrder.Ascending),
            new("updateDate", "Last updated", "unusedMedia_updateDate", UnusedMediaColumnType.DateTime, allowSort: true, defaultOrder: SortOrder.Descending),
            new("creatorId", "Created by", "unusedMedia_creator", UnusedMediaColumnType.User),
            new("writerId", "Changed by", "unusedMedia_writer", UnusedMediaColumnType.User),
            new("size", "Size", "unusedMedia_size", UnusedMediaColumnType.Bytes, allowSort: true, defaultOrder: SortOrder.Descending)
        ];

        return new UnusedMediaOptions {
            Text = request.Query["text"],
            SiteIds = StringUtils.ParseInt32Set(request.Query["siteId"]),
            Path = StringUtils.ParseInt32Set(request.Query["path"]),
            CreatorIds = StringUtils.ParseInt32Set(request.Query["creatorId"]),
            WriterIds = StringUtils.ParseInt32Set(request.Query["writerId"]),
            Limit = limit,
            Page = page,
            SortField = GetString(request, "sortField"),
            SortOrder = GetString(request, "sortOrder") is "desc" or "descending" ? SortOrder.Descending : SortOrder.Ascending,
            Columns = columns,
            IgnoredFolderIds = Settings.IgnoredFolderIds
        };

    }

    public UnusedMediaResult GetUnusedMedia(UnusedMediaOptions options) {

        // Load the reports for media that are currently in use - this is used to determine which media are not in use
        IReadOnlyList<IUsedMediaReport> reports = _unusedMediaService.GetUsedMediaReports();

        HashSet<Guid> keys = [];
        foreach (IUsedMediaReport report in reports) {
            keys.UnionWith(report.Keys);
        }

        int total = 0;

        List<IPublishedContent> temp = [];

        // Management API requests don't have an ambient Umbraco context, so ensure one before touching the
        // published media cache. "IPublishedContentQuery" is scoped, hence the extra service scope.
        using (UmbracoContextReference contextReference = _umbracoContextFactory.EnsureUmbracoContext())
        using (IServiceScope scope = _serviceScopeFactory.CreateScope()) {

            IPublishedContentQuery publishedContentQuery = scope.ServiceProvider.GetRequiredService<IPublishedContentQuery>();

            foreach (IPublishedContent media in publishedContentQuery.MediaAtRoot()) {

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
                    if (!keys.Contains(media.Key)) temp.Add(media);

                }

                // Iterate through all the descendants
                foreach (IPublishedContent descendant in media.Descendants()) {

                    if (!IsMatch(descendant, options)) {
                        continue;
                    }

                    // Skip if a folder
                    if (descendant.ContentType.Alias == Constants.Conventions.MediaTypes.Folder) {
                        continue;
                    }

                    // Increment the total count regardless of if the media is in use or not
                    total++;

                    // Skip if in use
                    if (keys.Contains(descendant.Key)) continue;

                    temp.Add(descendant);

                }

            }

            int limit = options.Limit;
            int unused = temp.Count;
            int pages = (int) Math.Ceiling(unused / (double) limit);
            int page = Math.Max(options.Page, 1);
            int offset = (page - 1) * options.Limit;
            UnusedMediaColumn? sortField = options.Columns.FirstOrDefault(x => x.Alias.InvariantEquals(options.SortField));
            SortOrder sortOrder = options.SortOrder ?? sortField?.DefaultOrder ?? SortOrder.Ascending;
            string? sortFieldAlias = sortField?.Alias;

            // Sort the results based on the specified sort field and order
            IEnumerable<IPublishedContent> meh;
            switch (sortField?.Alias) {
                case "name":
                    meh = temp.OrderBy(x => x.Name, sortOrder);
                    break;
                case "updateDate":
                    meh = temp.OrderBy(x => x.UpdateDate, sortOrder);
                    break;
                case "size":
                    // "GetInt32" came from Skybrud.Essentials.Umbraco, which has no stable Umbraco 17 release
                    meh = temp.OrderBy(x => x.Value<int>("umbracoBytes"), sortOrder);
                    break;
                default:
                    sortFieldAlias = "updateDate";
                    sortOrder = SortOrder.Descending;
                    meh = temp.OrderBy(x => x.UpdateDate, sortOrder);
                    break;
            }

            // The items are materialized inside the Umbraco context scope, as building each item reads properties
            // and URLs off the published cache
            List<UnusedMediaItem> items = meh.Skip(offset).Take(options.Limit).Select(x => CreateItem(x, options)).ToList();

            IReadOnlyList<UsedMediaReportSummary> summaries = reports.Select(x => new UsedMediaReportSummary(x)).ToList();

            return new UnusedMediaResult(total, unused, limit, offset, page, pages, sortFieldAlias, sortOrder, summaries, options.Columns, items);

        }

    }

    /// <summary>
    /// Virtual method for determining which media items match the specified <paramref name="options"/>.
    /// </summary>
    /// <param name="media">The media to check.</param>
    /// <param name="options">The options.</param>
    /// <returns><c>true</c> if <paramref name="media"/> matches <paramref name="options"/>; otherwise <c>false</c>.</returns>
    protected virtual bool IsMatch(IPublishedContent media, UnusedMediaOptions options) {

        // Always ignore folders
        if (media.ContentType.Alias == Constants.Conventions.MediaTypes.Folder) {
            return false;
        }

        // Ignore media not within "SiteIds" if the filter is specified
        if (options.HasSiteIds && !media.Path.ToInt32Array().Any(options.IsInSiteId)) {
            return false;
        }

        int[] path = media.Path.ToInt32Array();

        // Ignore media if a part of their path is ignored
        if (!options.IncludeIgnored && path.Any(x => options.IgnoredFolderIds.Contains(x))) {
            return false;
        }

        // Ignore media not within "Path" if the filter is specified
        if (options.HasPath && !path.Any(options.IsInPath)) {
            return false;
        }

        if (options.HasCreatorIds && !options.HasCreator(media.CreatorId)) {
            return false;
        }

        if (options.HasWriterIds && !options.HasWriter(media.WriterId)) {
            return false;
        }

        // Ignore media whose names does not include the specified text
        if (!string.IsNullOrWhiteSpace(options.Text) && !media.Name.InvariantContains(options.Text)) {
            return false;
        }

        return true;

    }

    protected virtual UnusedMediaItemCell CreateCell(IPublishedContent media, UnusedMediaColumn column, UnusedMediaOptions options) {

        switch (column.Type) {

            case UnusedMediaColumnType.Name:
                return new UnusedMediaItemCell(column.Alias, column.Name, value: media.Name);

            case UnusedMediaColumnType.DateTime:
                return column.Alias switch {
                    "createDate" => new UnusedMediaItemCell(column.Alias, column.Name, value: media.CreateDate, text: media.CreateDate.ToString("yyyy-MM-dd HH:mm")),
                    "updateDate" => new UnusedMediaItemCell(column.Alias, column.Name, value: media.UpdateDate, text: media.UpdateDate.ToString("yyyy-MM-dd HH:mm")),
                    _ => new UnusedMediaItemCell(column.Alias, column.Name, value: null)
                };

            case UnusedMediaColumnType.Bytes:
                int umbracoBytes = media.Value<int>("umbracoBytes");
                return new UnusedMediaItemCell(column.Alias, column.Name, value: umbracoBytes, text: StringUtils.FormatFileSize(umbracoBytes));

            case UnusedMediaColumnType.User:
                switch (column.Alias) {
                    case "creatorId":
                        return new UnusedMediaItemCell(column.Alias, column.Name, value: media.CreatorId,
                            text: media.CreatorName());
                    case "writerId":
                        return new UnusedMediaItemCell(column.Alias, column.Name, value: media.WriterId,
                            text: media.WriterName());
                    default:
                        object? value = media.Value(column.Alias);
                        string? valueName = null;
                        if (value is GuidUdi udi) {
                            if (udi.EntityType is "user") {
                                int userId = udi.Guid.ToInt32();
                                valueName = _userService.GetUserById(userId)?.Name; // TODO: cache user lookups
                            } else if (udi.EntityType == Constants.UdiEntityType.Member) {
                                valueName = GetMemberName(udi.Guid);
                            }
                        }
                        return new UnusedMediaItemCell(column.Alias, column.Name, value: value, text: valueName);
                }

            default:
                return new UnusedMediaItemCell(column.Alias, column.Name);

        }

    }

    protected virtual string? GetMemberName(Guid key) {

        // "Skybrud.Essentials.Umbraco" has no stable Umbraco 17 release, so both the "ExamineIndexes.MembersIndex"
        // constant and the "GetRequiredIndex" extension method it provided have been swapped for Umbraco/Examine's
        // own equivalents
        if (!_examineManager.TryGetIndex(Constants.UmbracoIndexes.MembersIndexName, out IIndex? index)) return null;

        return index
            .Searcher
            .CreateQuery()
            .NativeQuery($"__Key:\"{key}\"")
            .Execute()
            .FirstOrDefault()?
            .Values
            .GetValueOrDefault("nodeName");

    }

    protected virtual UnusedMediaItem CreateItem(IPublishedContent media, UnusedMediaOptions options) {

        List<UnusedMediaItemCell> cells = [];
        foreach (UnusedMediaColumn column in options.Columns) {
            cells.Add(CreateCell(media, column, options));
        }

        return new UnusedMediaItem(media, cells);

    }

    public virtual List<UnusedMediaFilter> CreateFilters(HttpRequest request, IUser user) {

        List<UnusedMediaFilter> filters = [];

        AppendTextFilter(request, user, filters);
        AppendFoldersFilters(request, user, filters);
        AppendCreatorsAndWritersFilters(request, user, filters);

        return filters;

    }

    /// <summary>
    /// Appends the folders filter to <paramref name="filters"/>. The method can be overriden to change the default behaviour.
    /// </summary>
    /// <param name="request">The current HTTP request.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="filters">The list of filters.</param>
    protected virtual void AppendFoldersFilters(HttpRequest request, IUser currentUser, List<UnusedMediaFilter> filters) {

        // Initialize the list with an item for an empty selection
        List<UnusedMediaFilterItem> items = [new("", "Select folder...", "unusedMedia_selectFolder")];

        using (UmbracoContextReference contextReference = _umbracoContextFactory.EnsureUmbracoContext())
        using (IServiceScope scope = _serviceScopeFactory.CreateScope()) {

            IPublishedContentQuery publishedContentQuery = scope.ServiceProvider.GetRequiredService<IPublishedContentQuery>();

            // Iterate through all media at the root level
            foreach (IPublishedContent level1 in publishedContentQuery.MediaAtRoot()) {

                // Ignore if not a folder
                if (level1.ContentType.Alias != Constants.Conventions.MediaTypes.Folder) {
                    continue;
                }

                // Append an item for the folder
                items.Add(new UnusedMediaFilterItem(level1.Id, level1.Name));

                // Append child folders as well
                AppendChildren(items, level1, 2);

            }

        }

        // Initialize and append the filter
        filters.Add(new UnusedMediaDropDownFilter("path") {
            Items = items
        });

    }

    /// <summary>
    /// Appends child folders of <paramref name="parent"/> to <paramref name="items"/>. The method is recursive,
    /// meaning that this method will also be called for each child until <paramref name="levels"/> is reached.
    ///
    /// The method can be overriden to change the default behaviour.
    /// </summary>
    /// <param name="items">The list of items to which the children will be added.</param>
    /// <param name="parent">The parent media.</param>
    /// <param name="levels">The maximum level or depth to append folders for.</param>
    protected virtual void AppendChildren(List<UnusedMediaFilterItem> items, IPublishedContent parent, int levels) {

        if (parent.Level == levels) {
            return;
        }

        foreach (IPublishedContent child in parent.Children()) {

            // Skip if not a folder
            if (child.ContentType.Alias != Constants.Conventions.MediaTypes.Folder) {
                continue;
            }

            string name = child.Name;

            // Prepend dashes to the name to visualize the tree structure
            for (int i = 2; i <= child.Level; i++) {
                name = "-- " + name;
            }

            items.Add(new UnusedMediaFilterItem(child.Id, name));

            // Run through the child's children
            AppendChildren(items, child, levels);

        }

    }

    #endregion

    #region Protected member methods

    /// <summary>
    /// Appends the text filter to <paramref name="filters"/>. The method can be overriden to change the default behaviour.
    /// </summary>
    /// <param name="request">The current HTTP request.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="filters">The list of filters.</param>
    protected virtual void AppendTextFilter(HttpRequest request, IUser currentUser, List<UnusedMediaFilter> filters) {
        filters.Add(new UnusedMediaTextFilter("text") {
            Placeholder = "Type to search...",
            PlaceholderKey = "general_typeToSearch"
        });
    }

    /// <summary>
    /// Appends both a creators filter and a writers filter to <paramref name="filters"/>.
    /// </summary>
    /// <param name="request">The current HTTP request.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="filters">The list of filters.</param>
    protected virtual void AppendCreatorsAndWritersFilters(HttpRequest request, IUser currentUser, List<UnusedMediaFilter> filters) {

        List<UnusedMediaFilterItem> creators = [];
        List<UnusedMediaFilterItem> writers = [];

        creators.Add(new UnusedMediaFilterItem("", "Created by", "unusedMedia_creator"));
        creators.Add(new UnusedMediaFilterItem(currentUser.Id, "Me", "unusedMedia_me"));

        writers.Add(new UnusedMediaFilterItem("", "Changed by", "unusedMedia_writer"));
        writers.Add(new UnusedMediaFilterItem(currentUser.Id, "Me", "unusedMedia_me"));

        foreach (IUser user in GetUsers(request, currentUser)) {
            if (currentUser.Id == user.Id) {
                continue;
            }

            creators.Add(new UnusedMediaFilterItem(user.Id, user.Name ?? string.Empty));
            writers.Add(new UnusedMediaFilterItem(user.Id, user.Name ?? string.Empty));
        }

        filters.Add(new UnusedMediaDropDownFilter("creatorId") {
            Items = creators
        });

        filters.Add(new UnusedMediaDropDownFilter("writerId") {
            Items = writers
        });

    }

    /// <summary>
    /// Returns a list of users to be shown in the unsued media dashboard.
    ///
    /// Override the method to control which users are shown. Default is all active users, sorted by their name in
    /// ascending order.
    /// </summary>
    /// <param name="request">The current HTTP request.</param>
    /// <param name="currentUser">The current user.</param>
    /// <returns>An instance of <see cref="IEnumerable{IUser}"/> containing the users to be shown.</returns>
    protected virtual IEnumerable<IUser> GetUsers(HttpRequest request, IUser currentUser) {
        return _userService
            .GetAll(0, int.MaxValue, out _)
            .Where(x => x.UserState == UserState.Active)
            .OrderBy(x => x.Name);
    }

    /// <summary>
    /// Returns the value of the query string parameter with the specified <paramref name="key"/> as an integer, or
    /// <c>0</c> if the parameter isn't present or doesn't hold a valid integer.
    ///
    /// Replaces the equivalent helper of <c>Skybrud.Essentials.AspNetCore</c>, which was dropped as part of the
    /// Umbraco 17 upgrade as it pulls in the Newtonsoft based ASP.NET Core MVC packages.
    /// </summary>
    protected static int GetInt32(HttpRequest request, string key) {
        return int.TryParse(request.Query[key], out int result) ? result : 0;
    }

    /// <summary>
    /// Returns the value of the query string parameter with the specified <paramref name="key"/>, or
    /// <see langword="null"/> if the parameter isn't present or is empty.
    /// </summary>
    protected static string? GetString(HttpRequest request, string key) {
        string? value = request.Query[key];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    #endregion

}

public class UnusedMediaItemCell {

    [JsonPropertyName("alias")]
    public string Alias { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("value")]
    public object? Value { get; }

    [JsonPropertyName("text")]
    public string? Text { get; }

    public UnusedMediaItemCell(UnusedMediaColumn column, string name, object? value = null, string? text = null) {
        Alias = column.Alias;
        Name = name;
        Value = value;
        Text = text;
    }

    public UnusedMediaItemCell(string alias, string name, object? value = null, string? text = null) {
        Alias = alias;
        Name = name;
        Value = value;
        Text = text;
    }

}
