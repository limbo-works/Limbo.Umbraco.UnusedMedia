using Examine;
using Limbo.Forms.Models.Fields;
using Limbo.Umbraco.UnusedMedia.Models;
using Limbo.Umbraco.UnusedMedia.Models.Sites;
using Limbo.Umbraco.UnusedMedia.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Skybrud.Essentials.AspNetCore;
using Skybrud.Essentials.Collections;
using Skybrud.Essentials.Collections.Extensions;
using Skybrud.Essentials.Exceptions;
using Skybrud.Essentials.Guids;
using Skybrud.Essentials.Strings;
using Skybrud.Essentials.Strings.Extensions;
using Skybrud.Essentials.Umbraco;
using Skybrud.Essentials.Umbraco.Constants;
using Skybrud.Essentials.Umbraco.Examine;
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
    private readonly ILocalizedTextService _localizedTextService;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly UnusedMediaService _unusedMediaService;
    private readonly IExamineManager _examineManager;

    #region Properties

    public int DefaultListLimit { get; protected set; }

    #endregion

    #region Constructors

    public UnusedMediaBackOfficeHelper(UnusedMediaBackOfficeHelperDependencies dependencies) {
        _dependencies = dependencies;

        _userService = dependencies.UserService;
        _localizedTextService = dependencies.LocalizedTextService;
        _umbracoContextAccessor = dependencies.UmbracoContextAccessor;
        _unusedMediaService = dependencies.UnusedMediaService;
        _examineManager = dependencies.ExamineManager;

        DefaultListLimit = 15;

    }

    #endregion

    #region Public member methods

    /// <summary>
    /// Returns a cache buster value based both on Umbraco's own cache buster and the current version of
    /// this package. This ensures a new cache buster value when either the ClientDependency version is bumped or
    /// the package is updated.
    /// </summary>
    /// <returns>The cache buster value.</returns>
    public virtual string GetCacheBuster() {
        string version1 = _dependencies.RuntimeState.SemanticVersion.ToSemanticString();
        string version2 = UnusedMediaPackage.InformationalVersion;
        return $"{version1}.{_dependencies.RuntimeState.Level}.{version2}".ToSHA1();
    }

    /// <summary>
    /// Returns a dictionary with server variables for this package, available through <c>Umbraco.Sys.ServerVariables.limbo.unusedMedia</c> in the backoffice.
    /// </summary>
    /// <returns>An instance of <see cref="Dictionary{TKey,TValue}"/>.</returns>
    public virtual Dictionary<string, object> GetServerVariables() {

        // Append the "redirects" dictionary to "skybrud"
        return new Dictionary<string, object> {
            {"cacheBuster", GetCacheBuster()},
            {"version", UnusedMediaPackage.InformationalVersion},
            {"dashboardElementName", _dependencies.Settings.DashboardElementName}
        };

    }

    public virtual IReadOnlyList<UnusedSiteItem> GetSites() {
        return [];
    }

    /// <summary>
    /// Initializes and returns a new instance of <see cref="UnusedMediaOptions"/> based on the specified HTTP <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The current HTTP request.</param>
    /// <param name="currentUser">The current user.</param>
    /// <returns>An instance of <see cref="UnusedMediaOptions"/>.</returns>
    public virtual UnusedMediaOptions CreateOptions(HttpRequest request, IUser currentUser) {

        int limit = request.Query.GetInt32("limit");
        if (limit <= 0) limit = DefaultListLimit;

        int page = Math.Max(request.Query.GetInt32("page"), 1);

        string name = Localize("name");
        string updateDate = Localize("updateDate");
        string creator = Localize("creator");
        string writer = Localize("writer");
        string size = Localize("size");

        List<UnusedMediaColumn> columns = [];
        columns.Add(new UnusedMediaColumn("name", name, UnusedMediaColumnType.Name, allowSort: true, defaultOrder: SortOrder.Ascending));
        columns.Add(new UnusedMediaColumn("updateDate", updateDate, UnusedMediaColumnType.DateTime, allowSort: true, defaultOrder: SortOrder.Descending));
        columns.Add(new UnusedMediaColumn("creatorId", creator, UnusedMediaColumnType.User));
        columns.Add(new UnusedMediaColumn("writerId", writer, UnusedMediaColumnType.User));
        columns.Add(new UnusedMediaColumn("size", size, UnusedMediaColumnType.Bytes, allowSort: true, defaultOrder: SortOrder.Descending));

        return new UnusedMediaOptions {
            Text = request.Query["text"],
            SiteIds = StringUtils.ParseInt32Set(request.Query["siteId"]),
            Path = StringUtils.ParseInt32Set(request.Query["path"]),
            CreatorIds = StringUtils.ParseInt32Set(request.Query["creatorId"]),
            WriterIds = StringUtils.ParseInt32Set(request.Query["writerId"]),
            Limit = limit,
            Page = page,
            SortField = request.Query.GetString("sortField"),
            SortOrder = request.Query.GetString("sortOrder") is "desc" or "descending" ? SortOrder.Descending : SortOrder.Ascending,
            Columns = columns
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

        if (!_umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? umbracoContext)) throw new Exception("Failed getting current Umbraco context.");
        if (umbracoContext.Media is null) throw new BjernerSaysNoException();

        foreach (IPublishedContent media in umbracoContext.Media.GetAtRoot()) {

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
                meh = temp.OrderBy(x => x.GetInt32("umbracoBytes"), sortOrder);
                break;
            default:
                sortFieldAlias = "updateDate";
                sortOrder = SortOrder.Descending;
                meh = temp.OrderBy(x => x.UpdateDate, sortOrder);
                break;
        }

        IEnumerable<UnusedMediaItem> items = meh.Skip(offset).Take(options.Limit).Select(x => CreateItem(x, options));

        IReadOnlyList<UsedMediaReportSummary> summaries = reports.Select(x => new UsedMediaReportSummary(x)).ToList();

        return new UnusedMediaResult(total, unused, limit, offset, page, pages, sortFieldAlias, sortOrder, summaries, options.Columns, items);

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

        // Ignore media not within "Path" if the filter is specified
        if (options.HasPath && !media.Path.ToInt32Array().Any(options.IsInPath)) {
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
                            } else if (udi.EntityType == UmbracoEntityTypes.Member) {
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
        return _examineManager
            .GetRequiredIndex(ExamineIndexes.MembersIndex)
            .GetSearcher()
            .CreateQuery()
            .NativeQuery($"__Key:\"{key}\"")
            .Execute()
            .FirstOrDefault()?
            .GetString("nodeName");
    }

    protected virtual UnusedMediaItem CreateItem(IPublishedContent media, UnusedMediaOptions options) {

        List<UnusedMediaItemCell> cells = [];
        foreach (UnusedMediaColumn column in options.Columns) {
            cells.Add(CreateCell(media, column, options));
        }

        return new UnusedMediaItem(media, cells);

    }

    public virtual List<FieldBase> CreateFilters(HttpRequest request, IUser user) {

        List<FieldBase> filters = [];

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
    protected virtual void AppendFoldersFilters(HttpRequest request, IUser currentUser, List<FieldBase> filters) {

        // Initialize the list with an item for an empty selection
        List<ListItem> items = [new("", Localize("selectFolder"))];

        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext)) {
            return;
        }

        // Iterate through all media at the root level
        foreach (IPublishedContent level1 in umbracoContext.Media!.GetAtRoot()) {

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
    /// <param name="items">The list of items to which the children will be added.</param>
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

    public string Localize(string alias) {
        return _localizedTextService.Localize("unusedMediaDashboard", alias);
    }

    public string Localize(string? area, string alias) {
        return _localizedTextService.Localize(area, alias);
    }

    #endregion

    #region Protected member methods

    /// <summary>
    /// Appends the text filter to <paramref name="filters"/>. The method can be overriden to change the default behaviour.
    /// </summary>
    /// <param name="request">The current HTTP request.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="filters">The list of filters.</param>
    protected virtual void AppendTextFilter(HttpRequest request, IUser currentUser, List<FieldBase> filters) {
        filters.Add(new TextField("text") {
            Placeholder = Localize(null, "typeToSearch")
        });
    }

    /// <summary>
    /// Appends both a creators filter and a writers filter to <paramref name="filters"/>.
    /// </summary>
    /// <param name="request">The current HTTP request.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="filters">The list of filters.</param>
    protected virtual void AppendCreatorsAndWritersFilters(HttpRequest request, IUser currentUser, List<FieldBase> filters) {

        List<ListItem> creators = [];
        List<ListItem> writers = [];

        creators.Add(new ListItem("", Localize("createdBy")));
        creators.Add(new ListItem(currentUser.Id, Localize("me")));

        writers.Add(new ListItem("", Localize("updatedBy")));
        writers.Add(new ListItem(currentUser.Id, Localize("me")));

        foreach (IUser user in GetUsers(request, currentUser)) {
            if (currentUser.Id == user.Id) {
                continue;
            }

            creators.Add(new ListItem(user.Id, user.Name ?? string.Empty));
            writers.Add(new ListItem(user.Id, user.Name ?? string.Empty));
        }

        filters.Add(new DropDownList("creatorId") {
            Items = creators
        });

        filters.Add(new DropDownList("writerId") {
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

    #endregion

}

public class UnusedMediaItemCell {

    [JsonProperty("alias")]
    public string Alias { get; }

    [JsonProperty("name")]
    public string Name { get; }

    [JsonProperty("value")]
    public object? Value { get; }

    [JsonProperty("text")]
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