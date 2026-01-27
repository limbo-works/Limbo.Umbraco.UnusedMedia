using Umbraco.Cms.Core.Dashboards;

namespace Limbo.Umbraco.UnusedMedia.Dashboards;

public class UnusedMediaDashboard : IDashboard {

    public string Alias => "LimboUnusedMedia";

    public string[] Sections => new[] { "content" };

    public string View => $"/App_Plugins/Limbo.Umbraco.UnusedMedia/Views/UnusedMediaDashboard.html?v={UnusedMediaPackage.SemVersion}";

    public IAccessRule[] AccessRules => Array.Empty<IAccessRule>();

}