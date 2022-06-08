using Limbo.Umbraco.UnusedMedia.Helpers;
using Umbraco.Cms.Core.Dashboards;

namespace Limbo.Umbraco.UnusedMedia.Dashboards {

    public class UnusedMediaDashboard : IDashboard {

        private readonly UnusedMediaBackOfficeHelper _backoffice;

        public string Alias => "unusedMedia";

        public string[] Sections => new[] { "content" };

        public string View => $"/App_Plugins/Limbo.Umbraco.UnusedMedia/Views/Dashboard.html?v={_backoffice.GetCacheBuster()}";

        public IAccessRule[] AccessRules => _backoffice.GetDashboardAccessRules();

        public UnusedMediaDashboard(UnusedMediaBackOfficeHelper backoffice) {
            _backoffice = backoffice;
        }

    }

}