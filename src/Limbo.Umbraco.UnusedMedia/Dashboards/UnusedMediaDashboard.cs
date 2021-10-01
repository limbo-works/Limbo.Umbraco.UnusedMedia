using System;
using Limbo.Umbraco.UnusedMedia.Helpers;
using Umbraco.Core.Dashboards;

namespace Limbo.Umbraco.UnusedMedia.Dashboards {
    
    public class UnusedMediaDashboard : IDashboard {
        
        private readonly UnusedMediaBackOfficeHelper _backoffice;
        
        public string Alias => "unusedMedia";

        public string[] Sections => new[] { "content" };

        public string View => $"/App_Plugins/Limbo.Umbraco.UnusedMedia/Views/Dashboard.html?v={_backoffice.GetCacheBuster()}";

        public IAccessRule[] AccessRules => Array.Empty<IAccessRule>();

        public UnusedMediaDashboard(UnusedMediaBackOfficeHelper backoffice) {
            _backoffice = backoffice;
        }

    }

}