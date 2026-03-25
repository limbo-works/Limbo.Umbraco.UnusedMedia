using Limbo.Umbraco.UnusedMedia.Models;
using Limbo.Umbraco.UnusedMedia.Providers;
using Microsoft.Extensions.Logging;
using Skybrud.Essentials.Time;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Services;

public class UnusedMediaService {

    private readonly UnusedMediaServiceDependencies _dependencies;

    protected ILogger<UnusedMediaService> Logger => _dependencies.Logger;

    protected AppCaches AppCaches => _dependencies.AppCaches;

    protected IMediaService MediaService => _dependencies.MediaService;

    #region Constructors

    public UnusedMediaService(UnusedMediaServiceDependencies dependencies) {
        _dependencies = dependencies;
    }

    #endregion

    #region Member methods

    /// <summary>
    /// Returns the media with the specified GUID <paramref name="key"/>. If no media is found with the specified key, <see langword="null"/> will be returned.
    /// </summary>
    /// <param name="key">The GUID key of the media.</param>
    /// <returns>An instance of <see cref="IMedia"/> if successful; otherwise, <see langword="null"/>.</returns>
    public IMedia? GetMedia(Guid key) {
        return MediaService.GetById(key);
    }

    /// <summary>
    /// Move the specified <paramref name="media"/> to the recycle bin. The <paramref name="user"/> performing the action is required for auditing purposes.
    /// </summary>
    /// <param name="media">The media to be trashed.</param>
    /// <param name="user">The user performing the action.</param>
    public void TrashMedia(IMedia media, IUser user) {
        try {
            MediaService.MoveToRecycleBin(media, userId: user.Id);
            Logger.LogInformation("Media with ID {MediaId} trashed by user with ID {UserId}.", media.Id, user.Id);
        } catch (Exception ex) {
            Logger.LogError(ex, "An error occurred while trying to trash media with ID {MediaId} by user with ID {UserId}.", media.Id, user.Id);
            throw;
        }
    }


    public UsedMediaProvider? GetUsedMediaProvider(string alias) {
        return GetUsedMediaProviders().FirstOrDefault(x => x.GetType().GetFullNameWithAssembly() == alias);
    }

    /// <summary>
    /// Returns a list of registered <see cref="UsedMediaProvider"/>.
    /// </summary>
    /// <returns></returns>
    public virtual IReadOnlyList<UsedMediaProvider> GetUsedMediaProviders() {
        return [.. _dependencies.Providers];
    }

    /// <summary>
    /// Returns a list of unused media reports for the registered providers. If a report for a provider is not found in the cache, it will be generated and added to the cache before being returned.
    /// </summary>
    /// <returns>A list of <see cref="IUsedMediaReport"/>.</returns>
    public IReadOnlyList<IUsedMediaReport> GetUsedMediaReports() {

        List<IUsedMediaReport> temp = [];

        foreach (UsedMediaProvider provider in GetUsedMediaProviders()) {
            IUsedMediaReport report = (IUsedMediaReport) AppCaches.RuntimeCache.Get(GetCacheKey(provider), () => {
                HashSet<Guid> keys = provider.ScanForUsedMediaKeys();
                return new UsedMediaReport(provider, EssentialsTime.UtcNow, keys);
            }, TimeSpan.FromMinutes(10))!;
            temp.Add(report);
        }

        return temp;

    }

    /// <summary>
    /// Builds a new report for the specified <paramref name="provider"/>. If a report for the provider is already
    /// found in the cache, it will be returned instead of generating a new one. The generated report will be added to
    /// the cache before being returned.
    /// </summary>
    /// <param name="provider">The provider.</param>
    /// <returns>An instance of <see cref="IUsedMediaReport"/> representing the build report.</returns>
    public virtual IUsedMediaReport BuildUsedMediaReport(UsedMediaProvider provider) {
        IUsedMediaReport report = BuildUsedMediaReportInternal(provider);
        AppCaches.RuntimeCache.Insert(GetCacheKey(provider), () => report, TimeSpan.FromMinutes(10));
        return report;
    }

    protected virtual IUsedMediaReport BuildUsedMediaReportInternal(UsedMediaProvider provider) {
        HashSet<Guid> keys = provider.ScanForUsedMediaKeys();
        return new UsedMediaReport(provider, EssentialsTime.UtcNow, keys);
    }

    protected virtual string GetCacheKey(UsedMediaProvider provider) {
        return $"UnusedMediaReport_{provider.GetType().FullName}";
    }

    #endregion

}