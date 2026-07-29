// [CHANGE: Umbraco 17 upgrade - ElementName is no longer honoured by the server] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

namespace Limbo.Umbraco.UnusedMedia.Models.Settings;

/// <summary>
/// Class representing settings related to the unused media dashboard in Umbraco. These settings can be used to configure access to the dashboard.
/// </summary>
public class UnusedMediaDashboardSettings {

    /// <summary>
    /// Gets or sets the aliases of the user groups that should have access to the dashboard. If empty (default), all
    /// users with access to the content section will have access.
    ///
    /// As of Umbraco 17, this is enforced by the Management API controller rather than by the removed server side
    /// dashboard - meaning it now restricts the endpoints as well, and not just the UI.
    /// </summary>
    public List<string> AllowedGroups { get; set; } = [];

    /// <summary>
    /// Gets or sets the name of the dashboard element.
    /// </summary>
    [Obsolete("No longer used. The dashboard and its element name are declared in the package's umbraco-package.json as of Umbraco 17. To use your own element, register your own dashboard extension and exclude the 'Limbo.UnusedMedia.Dashboard' extension.")]
    public string ElementName { get; set; } = "limbo-unused-media-dashboard";

    /// <summary>
    /// Gets or sets the amount of media to show on each page. Default is <c>15</c>.
    /// </summary>
    public int PerPage { get; set; } = 15;

}
