// [CHANGE: Umbraco 17 upgrade - dashboard/manifest/server variables now live in wwwroot/umbraco-package.json] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using Limbo.Umbraco.UnusedMedia.BlockList;
using Limbo.Umbraco.UnusedMedia.Helpers;
using Limbo.Umbraco.UnusedMedia.Models.Settings;
using Limbo.Umbraco.UnusedMedia.Providers;
using Limbo.Umbraco.UnusedMedia.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Limbo.Umbraco.UnusedMedia.Composers;

public class UnusedMediaComposer : IComposer {

    public void Compose(IUmbracoBuilder builder) {

        builder
            .WithCollectionBuilder<UsedMediaProviderCollectionBuilder>()
            .Add<ContentCacheUsedMediaProvider>()
            .Add<RedirectsUsedMediaProvider>()
            .Add<UmbracoRelationsUsedMediaProvider>();

        builder.Services.AddTransient<SqlHelper>();

        IConfigurationSection section = builder.Config.GetSection("Limbo:UnusedMedia");
        if (section.Exists()) builder.Services.Configure<UnusedMediaSettings>(section);

        // Register misc dependencies
        builder.Services.AddSingleton<UnusedMediaService>();
        builder.Services.AddSingleton<UnusedMediaServiceDependencies>();
        builder.Services.AddSingleton<UnusedMediaBackOfficeHelper>();
        builder.Services.AddSingleton<UnusedMediaBackOfficeHelperDependencies>();
        builder.Services.AddSingleton<UnusedMediaBlockListParser>();

        // As of Umbraco 14, "IDashboard", "IManifestFilter" and the server variables notification no longer exist -
        // the dashboard, its element and its localizations are declared in "wwwroot/umbraco-package.json" instead.

    }

}
