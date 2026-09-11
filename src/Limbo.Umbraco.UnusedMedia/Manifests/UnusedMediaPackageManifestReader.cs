using Limbo.Umbraco.UnusedMedia.Models.Settings;
using Microsoft.Extensions.Options;
using Skybrud.Essentials.Strings;
using Skybrud.Essentials.Umbraco.Manifests.Extensions;
using Skybrud.Essentials.Umbraco.Manifests.Extensions.Dashboards;
using Skybrud.Essentials.Umbraco.Manifests.Extensions.Localization;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;
using static Limbo.Umbraco.UnusedMedia.UnusedMediaPackage;

namespace Limbo.Umbraco.UnusedMedia.Manifests;

public class UnusedMediaPackageManifestReader : IPackageManifestReader {

    private readonly IOptions<UnusedMediaSettings> _settings;

    public UnusedMediaPackageManifestReader(IOptions<UnusedMediaSettings> settings) {
        _settings = settings;
    }

    public async Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync() {

        PackageManifest manifest = new() {
            Id = Alias,
            Name = Name,
            AllowTelemetry = true,
            Version = InformationalVersion,
            Extensions = [
                ..GetLocalizationExtensions(),
                ..GetDashboardExtensions()
            ],
            Importmap = new PackageManifestImportmap {
                Imports = new Dictionary<string, string> {
                    {"@limbo/unused-media/dashboard", $"/App_Plugins/{Alias}/Elements/Dashboard.js"}
                }
            }
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

    private IEnumerable<IExtension> GetDashboardExtensions() {

        yield return new DashboardExtension {
            Alias = $"{Alias}.Dashboard",
            Name = $"{Name}: Dashboard",
            Element = StringUtils.FirstWithValue(_settings.Value.Dashboard.Element, $"/App_Plugins/{Alias}/Elements/Dashboard.js"),
            Weight = _settings.Value.Dashboard.Weight,
            Meta = new DashboardMeta {
                Label = StringUtils.FirstWithValue(_settings.Value.Dashboard.Label, "#unusedMedia_title"),
                Pathname = StringUtils.FirstWithValue(_settings.Value.Dashboard.PathName, "unused-media")
            }
        };

    }

}