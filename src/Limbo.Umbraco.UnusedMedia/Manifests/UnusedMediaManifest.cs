using Umbraco.Cms.Core.Manifest;

namespace Limbo.Umbraco.UnusedMedia.Manifests;

public class UnusedMediaManifest : IManifestFilter {

    public void Filter(List<PackageManifest> manifests) {
        manifests.Add(new PackageManifest {
            AllowPackageTelemetry = true,
            PackageId = UnusedMediaPackage.Alias,
            PackageName = UnusedMediaPackage.Name,
            Version = UnusedMediaPackage.InformationalVersion,
            BundleOptions = BundleOptions.Independent,
            Scripts = new[] {
                $"/App_Plugins/Skybrud.Essentials/Scripts/App.js",
                $"/App_Plugins/{UnusedMediaPackage.Alias}/Scripts/Controllers/UnusedMediaDashboard.js"
            },
            Stylesheets = new[] {
                $"/App_Plugins/{UnusedMediaPackage.Alias}/Styles/Default.css"
            }
        });
    }

}