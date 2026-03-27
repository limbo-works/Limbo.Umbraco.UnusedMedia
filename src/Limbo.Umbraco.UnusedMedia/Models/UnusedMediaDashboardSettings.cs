namespace Limbo.Umbraco.UnusedMedia.Models;

/// <summary>
/// Class representing settings related to the unused media dashboard in Umbraco. These settings can be used to configure access to the dashboard and specify a custom dashboard element if desired.
/// </summary>
public class UnusedMediaDashboardSettings {

    /// <summary>
    /// Gets or sets the aliases of the user groups that should have access to the dashboard. If empty (default), all users will have access.
    /// </summary>
    public List<string> AllowedGroups { get; set; } = [];

    /// <summary>
    /// Gets or sets the name of the dashboard element. Change this if you want to use your own dashboard element instead of the default one provided by this package - e.g. if your goal is to replace or customize the default dashboard. Default is <c>limbo-unused-media-dashboard</c>.
    /// </summary>
    public string ElementName { get; set; } = "limbo-unused-media-dashboard";

}