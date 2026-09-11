using Limbo.Umbraco.UnusedMedia.Api;
using Limbo.Umbraco.UnusedMedia.BlockList;
using Limbo.Umbraco.UnusedMedia.Helpers;
using Limbo.Umbraco.UnusedMedia.Manifests;
using Limbo.Umbraco.UnusedMedia.Models.Settings;
using Limbo.Umbraco.UnusedMedia.Providers;
using Limbo.Umbraco.UnusedMedia.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Skybrud.Essentials.Umbraco.Composing;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Limbo.Umbraco.UnusedMedia.Composers;

public class UnusedMediaComposer : IComposer {

    public void Compose(IUmbracoBuilder builder) {

        // Add the package manifest reader for this package
        builder.AddPackageManifestReader<UnusedMediaPackageManifestReader>();

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

        // Configure the package for Swagger/OpenAPI
        builder.Services.ConfigureOptions<UnusedMediaSwaggerGenOptions>();

    }

}