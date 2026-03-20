using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Dashboards;
using Limbo.Umbraco.UnusedMedia.Models;

namespace Limbo.Umbraco.UnusedMedia.Dashboards;

public class UnusedMediaDashboard : IDashboard {

    private readonly UnusedMediaSettings _settings;

    public UnusedMediaDashboard(IOptions<UnusedMediaSettings> settings) {
        _settings = settings.Value;
    }

    public string Alias => "LimboUnusedMedia";

    public string[] Sections => ["content"];

    public string View => $"/App_Plugins/Limbo.Umbraco.UnusedMedia/Views/UnusedMediaDashboard.html?v={UnusedMediaPackage.SemVersion}";

    public IAccessRule[] AccessRules {
        get {
            if (_settings.AdminGroups.Count == 0) return [];
            return [
                new AccessRule {
                    Type = AccessRuleType.Grant,
                    Value = string.Join(",", _settings.AdminGroups)
                }
            ];
        }
    }

}