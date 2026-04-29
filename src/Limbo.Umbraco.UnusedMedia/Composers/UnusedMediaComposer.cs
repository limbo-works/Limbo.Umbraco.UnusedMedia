using Limbo.Umbraco.UnusedMedia.BlockList;
using Limbo.Umbraco.UnusedMedia.Dashboards;
using Limbo.Umbraco.UnusedMedia.Helpers;
using Limbo.Umbraco.UnusedMedia.Manifests;
using Limbo.Umbraco.UnusedMedia.Models.Settings;
using Limbo.Umbraco.UnusedMedia.Notifications.Handlers;
using Limbo.Umbraco.UnusedMedia.Providers;
using Limbo.Umbraco.UnusedMedia.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

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

        builder.Dashboards().Add<UnusedMediaDashboard>();
        builder.ManifestFilters().Append<UnusedMediaManifest>();
        builder.AddNotificationHandler<ServerVariablesParsingNotification, ServerVariablesParsingHandler>();

    }

}