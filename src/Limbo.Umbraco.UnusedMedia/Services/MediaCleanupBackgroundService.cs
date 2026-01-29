using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.HostedServices;

namespace Limbo.Umbraco.UnusedMedia.Services;

public class MediaCleanupBackgroundService : RecurringHostedServiceBase {
    private readonly IRuntimeState _runtimeState;
    private readonly IServerRoleAccessor _serverRoleAccessor;
    private readonly ILogger<MediaCleanupBackgroundService> _logger;
    private readonly UnusedMediaService _unusedMediaService; // New dependency

    public MediaCleanupBackgroundService(
        IRuntimeState runtimeState,
        IServerRoleAccessor serverRoleAccessor,
        ILogger<MediaCleanupBackgroundService> logger,
        UnusedMediaService unusedMediaService) // New dependency
        : base(logger, TimeSpan.FromHours(24), TimeSpan.FromMinutes(1)) {
        _runtimeState = runtimeState;
        _serverRoleAccessor = serverRoleAccessor;
        _logger = logger;
        _unusedMediaService = unusedMediaService; // Initialize new dependency
    }

    public override Task PerformExecuteAsync(object? state) {
        if (ShouldExecute() is false) {
            return Task.CompletedTask;
        }

        _logger.LogInformation("MediaCleanupBackgroundService is running. Triggering unused media scan.");

        // Trigger the scan process in UnusedMediaService
        _unusedMediaService.StartScan();

        return Task.CompletedTask;
    }

    private bool ShouldExecute() {
        if (_runtimeState.Level != global::Umbraco.Cms.Core.RuntimeLevel.Run) {
            return false;
        }

        switch (_serverRoleAccessor.CurrentServerRole) {
            case ServerRole.Subscriber:
                _logger.LogDebug("Does not run on subscriber servers.");
                return false;
            case ServerRole.Unknown:
                _logger.LogDebug("Does not run on servers with unknown role.");
                return false;
        }

        return true;
    }
}