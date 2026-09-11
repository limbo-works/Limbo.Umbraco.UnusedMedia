using Skybrud.Essentials.Umbraco.Constants;
using Skybrud.Essentials.Umbraco.Manifests.Extensions;
using Skybrud.Essentials.Umbraco.Manifests.Extensions.Dashboards;
using Skybrud.Essentials.Umbraco.Manifests.Extensions.Localization;
using Skybrud.Essentials.Umbraco.Manifests.Extensions.PropertyEditors;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;
using static Limbo.Umbraco.UnusedMedia.UnusedMediaPackage;

namespace Limbo.Umbraco.UnusedMedia.Manifests;

public class UnusedMediaPackageManifestReader : IPackageManifestReader {

    public async Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync() {

        PackageManifest manifest = new() {
            Id = Alias,
            Name = Name,
            AllowTelemetry = true,
            Version = InformationalVersion,
            Extensions = [
                ..GetLocalizationExtensions(),
                ..GetDashboardExtensions()
            ]
        };

        return await Task.FromResult(new List<PackageManifest> { manifest });

    }

    private static IEnumerable<IExtension> GetLocalizationExtensions() {

        yield return new LocalizationExtension {
            Alias = $"{Alias}.Localization.EnUs",
            Name = $"{Name}: English (US)",
            Js = $"/App_Plugins/{Alias}/Localization/en-US.js",
            Meta = new LocalizationMeta {
                Culture = "en"
            }
        };

        yield return new LocalizationExtension {
            Alias = $"{Alias}.Localization.DaDk",
            Name = $"{Name}: Danish (DK)",
            Js = $"/App_Plugins/{Alias}/Localization/da-DK.js",
            Meta = new LocalizationMeta {
                Culture = "da"
            }
        };

    }

    private static IEnumerable<IExtension> GetDashboardExtensions() {

        yield return new DashboardExtension {
            Alias = $"{Alias}.Dashboard",
            Name = $"{Name}: Dashboard",
            Element = $"/App_Plugins/{Alias}/Elements/Dashboard.js",
            ElementName = "limbo-unused-media-dashboard",
            Weight = 20,
            Meta = new DashboardMeta {
                Label = "#unusedMedia_title",
                Pathname = "unused-media"
            }
        };

    }

}