using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Limbo.Umbraco.UnusedMedia.Models;
using Limbo.Umbraco.UnusedMedia.Providers;
using Limbo.Umbraco.UnusedMedia.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using Constants = Umbraco.Cms.Core.Constants;
using Limbo.Umbraco.UnusedMedia.Helpers; // Added for SqlHelper

namespace Limbo.Umbraco.UnusedMedia.Services;

public class UnusedMediaService {

    private readonly ILogger<UnusedMediaService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IMediaService _mediaService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly SqlHelper _sqlHelper; // Added SqlHelper
    private readonly DeepScanProvider _deepScanProvider; // Added DeepScanProvider
    private readonly RedirectsProvider _redirectsProvider; // Added RedirectsProvider


    // A concurrent dictionary to store the progress of each task
    private static readonly ConcurrentDictionary<Guid, UnusedMediaScanStatus> _taskStatuses = new();
    private static UnusedMediaReport? _lastUnusedMediaReport;
    private static DateTime? _lastScanDate;

    public UnusedMediaService(ILogger<UnusedMediaService> logger,
                              IServiceProvider serviceProvider,
                              IBackgroundTaskQueue backgroundTaskQueue,
                              IMediaService mediaService,
                              IContentTypeService contentTypeService,
                              IUmbracoContextFactory umbracoContextFactory,
                              SqlHelper sqlHelper,
                              DeepScanProvider deepScanProvider,
                              RedirectsProvider redirectsProvider) {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _backgroundTaskQueue = backgroundTaskQueue;
        _mediaService = mediaService;
        _contentTypeService = contentTypeService;
        _umbracoContextFactory = umbracoContextFactory;
        _sqlHelper = sqlHelper; // Initialized SqlHelper
        _deepScanProvider = deepScanProvider; // Initialized DeepScanProvider
        _redirectsProvider = redirectsProvider; // Initialized RedirectsProvider
    }

    public UnusedMediaReport GetUnusedMediaReport() {
        return _lastUnusedMediaReport ?? new UnusedMediaReport(new List<UnusedMediaItem>(), null, 0);
    }

    public void ClearScan() {
        _lastUnusedMediaReport = null;
        _lastScanDate = null;
    }

    public Guid StartScan() {
        Guid taskId = Guid.NewGuid();
        _taskStatuses.TryAdd(taskId, new UnusedMediaScanStatus {
            TaskId = taskId,
            Status = "Queued",
            Progress = 0,
            Total = 0,
            ProcessedMedia = new List<string>(),
            Errors = new List<string>()
        });

        _backgroundTaskQueue.QueueBackgroundWorkItem(token => {
            RunScanProcess(taskId);
            return Task.CompletedTask;
        });

        return taskId;
    }

    public UnusedMediaScanStatus? GetScanStatus(Guid taskId) {
        _taskStatuses.TryGetValue(taskId, out UnusedMediaScanStatus? status);
        return status;
    }

    public void RunScanProcess(Guid taskId) {
        _taskStatuses.TryGetValue(taskId, out UnusedMediaScanStatus? status);
        if (status == null) {
            _logger.LogError("Task status not found for task ID {TaskId}", taskId);
            return;
        }

        status.Status = "In Progress";
        Stopwatch stopwatch = Stopwatch.StartNew();

        try {
            using (UmbracoContextReference umbracoContextReference = _umbracoContextFactory.EnsureUmbracoContext()) {
                if (umbracoContextReference.UmbracoContext == null) {
                    throw new InvalidOperationException("Umbraco context is null.");
                }

                // Get all media GUIDs from the SQL helper
                var allMediaGuids = _sqlHelper.GetAllMediaGuids();
                status.Total = allMediaGuids.Count;
                _logger.LogInformation("Starting scan of {TotalCount} media items", status.Total);

                var unusedMediaItems = new List<UnusedMediaItem>();
                int processedCount = 0;
                int mediaFolderCount = 0;
                int filteredByRelations = 0;
                int filteredByDeepScan = 0;
                int filteredByRedirects = 0;

                foreach (var mediaGuid in allMediaGuids) {
                    processedCount++;
                    status.Progress = processedCount;
                    
                    if (processedCount % 10 == 0 || processedCount == status.Total) {
                         status.ProcessedMedia.Add($"Processing media: {mediaGuid}");
                    }
                    _logger.LogDebug("Processing media with GUID: {MediaGuid}", mediaGuid);

                    IMedia? mediaItem = _mediaService.GetById(mediaGuid);
                    if (mediaItem == null) {
                        status.Errors.Add($"Media item with GUID {mediaGuid} not found.");
                        _logger.LogWarning("Media item with GUID {MediaGuid} not found.", mediaGuid);
                        continue;
                    }

                    string mediaUdi = mediaItem.GetUdi().ToString();

                    // Check if media is used by relations
                    if (_sqlHelper.IsMediaUsedInRelations(mediaGuid)) {
                        _logger.LogDebug("Media {MediaUdi} is used in relations.", mediaUdi);
                        filteredByRelations++;
                        continue;
                    }

                    // Check if media is used by DeepScanProvider
                    if (_deepScanProvider.IsMediaUsed(mediaUdi)) {
                        _logger.LogDebug("Media {MediaUdi} is used by DeepScanProvider.", mediaUdi);
                        filteredByDeepScan++;
                        continue;
                    }

                    // Check if media is used by RedirectsProvider
                    if (_redirectsProvider.IsMediaUsed(mediaUdi)) {
                        _logger.LogDebug("Media {MediaUdi} is used by RedirectsProvider.", mediaUdi);
                        filteredByRedirects++;
                        continue;
                    }
                    
                    // Add other providers here if needed
                    
                    unusedMediaItems.Add(new UnusedMediaItem(mediaItem));
                    _logger.LogDebug("Media {MediaUdi} is identified as unused.", mediaUdi);
                }

                _logger.LogInformation(
                    "Scan completed: {TotalCount} total media, {FilteredByRelations} filtered by relations, " +
                    "{FilteredByDeepScan} filtered by content scan, {FilteredByRedirects} filtered by redirects, " +
                    "{UnusedCount} unused media found",
                    status.Total, filteredByRelations, filteredByDeepScan, filteredByRedirects, unusedMediaItems.Count);

                _lastUnusedMediaReport = new UnusedMediaReport(unusedMediaItems, DateTime.Now, mediaFolderCount);
                _lastScanDate = DateTime.Now;

                status.Status = "Completed";
                status.Progress = status.Total;
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "Error during unused media scan for task ID {TaskId}", taskId);
            status.Status = "Failed";
            status.Message = ex.Message;
        } finally {
            stopwatch.Stop();
            _logger.LogInformation("Unused media scan for task ID {TaskId} finished in {Elapsed}ms", taskId, stopwatch.ElapsedMilliseconds);
        }
    }

    public void DeleteMedia(int mediaId) {
        var media = _mediaService.GetById(mediaId);
        if (media != null) {
            _mediaService.Delete(media);
            _logger.LogInformation("Deleted media with ID: {MediaId}", mediaId);
            // Refresh the report after deletion
            if (_lastUnusedMediaReport != null) {
                _lastUnusedMediaReport.MediaItems.RemoveAll(x => x.Id == mediaId);
                _lastUnusedMediaReport.ScanDate = DateTime.Now; // Update scan date as content has changed
            }
        } else {
            _logger.LogWarning("Attempted to delete non-existent media with ID: {MediaId}", mediaId);
        }
    }

}