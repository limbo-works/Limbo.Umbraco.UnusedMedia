namespace Limbo.Umbraco.UnusedMedia.Models;

public class UnusedMediaSettings {

    public List<string> AdminGroups { get; set; } = [];

    /// <summary>
    /// Gets or sets the name of the dashboard element. Change this if you want to use your own dashboard element instead of the default one provided by this package - e.g. if your goal is to replace or customize the default dashboard.
    /// </summary>
    public string DashboardElementName { get; set; } = "limbo-unused-media-dashboard";

}