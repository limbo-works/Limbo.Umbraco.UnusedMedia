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
    /// Gets or sets the URL of the dashboard element. Use this to override the default URL of <c>/App_Plugins/Limbo.UnusedMedia/Elements/Dashboard.js</c>.
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// Gets or sets the amount of media to show on each page. Default is <c>15</c>.
    /// </summary>
    public int PerPage { get; set; } = 15;

    /// <summary>
    /// Gets or sets the weight of the dashboard. Default is <c>20</c>.
    /// </summary>
    public int Weight { get; set; } = 20;

    /// <summary>
    /// Gets or sets the label of the dashboard. Default is <c>#unusedMedia_title</c>.
    /// </summary>
    public string Label { get; set; } = "#unusedMedia_title";

    /// <summary>
    /// Gets or sets the path name of the dashboard. Default is <c>unused-media</c>.
    /// </summary>
    public string PathName { get; set; } = "unused-media";

}