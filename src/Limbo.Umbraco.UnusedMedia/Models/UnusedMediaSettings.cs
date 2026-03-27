namespace Limbo.Umbraco.UnusedMedia.Models;

/// <summary>
/// Class representing the settings for the unused media detection in Umbraco. This class can be used to configure various aspects of the unused media detection, such as dashboard settings and folders to ignore during scanning.
/// </summary>
public class UnusedMediaSettings {

    /// <summary>
    /// Gets or sets settings specified to the dashboard.
    /// </summary>
    public UnusedMediaDashboardSettings Dashboard { get; set; } = new();

    /// <summary>
    /// Gets or sets a list of folder IDs that should be ignored when scanning for unused media. If a media item has one of these IDs in its path, it will be excluded from the list of unused media.
    /// </summary>
    public HashSet<int> IgnoredFolderIds { get; set; } = [];

}