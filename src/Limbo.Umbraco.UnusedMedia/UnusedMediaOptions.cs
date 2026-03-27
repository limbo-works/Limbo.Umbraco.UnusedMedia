using Limbo.Umbraco.UnusedMedia.Models;
using Skybrud.Essentials.Collections;

namespace Limbo.Umbraco.UnusedMedia;

public class UnusedMediaOptions {

    // TODO: move me to a sub-folder somewhere ¯\_(ツ)_/¯

    #region Properties

    /// <summary>
    /// Gets or sets a text based query the returned results should match.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets one or more site IDs which the returned media should have in its path.
    /// </summary>
    public HashSet<int> SiteIds { get; set; } = [];

    public bool HasSiteIds => SiteIds is { Count: > 0 };

    /// <summary>
    /// Gets or sets one or more IDs which the returned media should have in its path.
    /// </summary>
    public HashSet<int> Path { get; set; } = [];

    public bool HasPath => Path is { Count: > 0 };

    /// <summary>
    /// Gets or sets an array of creator IDs the returned results should match.
    /// </summary>
    public HashSet<int> CreatorIds { get; set; } = [];

    public bool HasCreatorIds => CreatorIds is { Count: > 0 };

    /// <summary>
    /// Gets or sets an array of writer IDs the returned results should match.
    /// </summary>
    public HashSet<int> WriterIds { get; set; } = [];

    public bool HasWriterIds => WriterIds is { Count: > 0 };

    /// <summary>
    /// Gets or sets a collection of folder IDs that should be ignored. If one of these IDs are in the path of a
    /// given media, the media will be excluded in the list of unused media.
    /// </summary>
    public HashSet<int> IgnoredFolderIds { get; set; } = [];

    /// <summary>
    /// Gets whether any ignored folder IDs have been specified.
    /// </summary>
    public bool HasIgnoredFolderIds => IgnoredFolderIds is { Count: > 0 };

    /// <summary>
    /// Gets whether ignored media should be included in the results. If <see langword="true"/>, media that are in a path containing any of the IDs specified in <see cref="IgnoredFolderIds"/> will be included in the results; if <see langword="false"/>, such media will be excluded. Default is <see langword="false"/>.
    /// </summary>
    public bool IncludeIgnored { get; set; }

    /// <summary>
    /// Gets or sets the maximum amount of results to be returned.
    /// </summary>
    public int Limit { get; set; } = 15;

    /// <summary>
    /// Gets or sets the page to be returned.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets a list of the columns to be shown in the dashboard.
    /// </summary>
    public List<UnusedMediaColumn> Columns { get; set; } = [];

    public string? SortField { get; set; }

    public SortOrder? SortOrder { get; set; }

    #endregion

    #region Constructors

    public UnusedMediaOptions() { }

    public UnusedMediaOptions(UnusedMediaOptions source) { Text = source.Text;
        SiteIds = new HashSet<int>(source.SiteIds);
        Path = new HashSet<int>(source.Path);
        CreatorIds = new HashSet<int>(source.CreatorIds);
        WriterIds = new HashSet<int>(source.WriterIds);
        IgnoredFolderIds = new HashSet<int>(source.IgnoredFolderIds);
        Limit = source.Limit;
        Page = source.Page;
        Columns = new List<UnusedMediaColumn>(source.Columns);
        SortField = source.SortField;
        SortOrder = source.SortOrder;
    }

    #endregion

    #region Member methods

    public bool IsInSiteId(int id) {
        return SiteIds.Contains(id);
    }

    public bool IsInPath(int id) {
        return Path.Contains(id);
    }

    public bool HasCreator(int id) {
        return CreatorIds.Contains(id);
    }

    public bool HasWriter(int id) {
        return WriterIds.Contains(id);
    }

    #endregion

}
