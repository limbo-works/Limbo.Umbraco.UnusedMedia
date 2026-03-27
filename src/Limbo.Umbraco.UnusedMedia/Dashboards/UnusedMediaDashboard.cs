using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Dashboards;
using Limbo.Umbraco.UnusedMedia.Models;

namespace Limbo.Umbraco.UnusedMedia.Dashboards;

public class UnusedMediaDashboard : IDashboard {

    private readonly UnusedMediaSettings _settings;

    public UnusedMediaDashboard(IOptions<UnusedMediaSettings> settings) {
        _settings = settings.Value;
    }

    public string Alias => "unused-media";

    public string[] Sections => ["content"];

    public string View => $"/App_Plugins/Limbo.Umbraco.UnusedMedia/Views/Dashboard.html?v={UnusedMediaPackage.InformationalVersion}";

    public IAccessRule[] AccessRules {
        get {
            if (_settings.Dashboard.AllowedGroups.Count == 0) return [];
            return [
                new AccessRule {
                    Type = AccessRuleType.Grant,
                    Value = string.Join(",", _settings.Dashboard.AllowedGroups)
                }
            ];
        }
    }

}