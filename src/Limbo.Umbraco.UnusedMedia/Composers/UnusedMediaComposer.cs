using Limbo.Umbraco.UnusedMedia.Dashboards;
using Limbo.Umbraco.UnusedMedia.Helpers;
using Limbo.Umbraco.UnusedMedia.Manifests;
using Limbo.Umbraco.UnusedMedia.Models;
using Limbo.Umbraco.UnusedMedia.Providers;
using Limbo.Umbraco.UnusedMedia.Scheduling; // Add this using statement
using Limbo.Umbraco.UnusedMedia.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Limbo.Umbraco.UnusedMedia.Composers;

public class UnusedMediaComposer : IComposer {
    public void Compose(IUmbracoBuilder builder) {
        builder.Services.AddTransient<SqlHelper>();
        builder.Services.AddSingleton<DeepScanProvider>();
        builder.Services.AddSingleton<RedirectsProvider>();

        if (builder.Config != null) {
            var section = builder.Config.GetSection("Limbo:UnusedMedia");
            if (section.Exists()) {
                builder.Services.Configure<UnusedMediaSettings>(section);
            }
        }
        
        // Register UnusedMediaService and scheduling services
        builder.Services.AddSingleton<UnusedMediaService>();
        builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        builder.Services.AddHostedService<QueuedHostedService>();

        // Register the MediaCleanupBackgroundService as a hosted service
        builder.Services.AddHostedService<MediaCleanupBackgroundService>();

        builder.Dashboards().Add<UnusedMediaDashboard>();
        builder.ManifestFilters().Append<UnusedMediaManifest>();
    }
}