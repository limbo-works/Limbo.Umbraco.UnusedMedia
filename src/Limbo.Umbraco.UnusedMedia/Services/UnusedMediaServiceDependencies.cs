using Limbo.Umbraco.UnusedMedia.Providers;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Services;

namespace Limbo.Umbraco.UnusedMedia.Services;

public class UnusedMediaServiceDependencies {

    public ILogger<UnusedMediaService> Logger { get; }

    public AppCaches AppCaches { get; }

    public IMediaService MediaService { get; }
    public UsedMediaProviderCollection Providers { get; }

    public UnusedMediaServiceDependencies(ILogger<UnusedMediaService> logger, AppCaches appCaches, IMediaService mediaService, UsedMediaProviderCollection providers) {
        Logger = logger;
        AppCaches = appCaches;
        MediaService = mediaService;
        Providers = providers;
    }

}