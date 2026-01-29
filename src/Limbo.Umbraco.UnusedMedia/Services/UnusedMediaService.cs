using System.Collections.Concurrent;
using System.Diagnostics;
using Limbo.Umbraco.UnusedMedia.Helpers;
using Limbo.Umbraco.UnusedMedia.Models;
using Limbo.Umbraco.UnusedMedia.Providers;
using Limbo.Umbraco.UnusedMedia.Scheduling;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Services;

public class UnusedMediaService {

    private readonly ILogger<UnusedMediaService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IMediaService _mediaService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly SqlHelper _sqlHelper;
    private readonly DeepScanProvider _deepScanProvider;
    private readonly RedirectsProvider _redirectsProvider;
    private readonly IUserService _userService;


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
                              RedirectsProvider redirectsProvider,
                              IUserService userService) {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _backgroundTaskQueue = backgroundTaskQueue;
        _mediaService = mediaService;
        _contentTypeService = contentTypeService;
        _umbracoContextFactory = umbracoContextFactory;
        _sqlHelper = sqlHelper;
        _deepScanProvider = deepScanProvider;
        _redirectsProvider = redirectsProvider;
        _userService = userService;
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

                // 1. Gather all explicitly used media UDIs/GUIDs from providers upfront
                var explicitUsedGuids = new HashSet<Guid>();

                var deepScanUdis = _deepScanProvider.GetUsedMediaUdis();
                foreach (var udiStr in deepScanUdis) {
                    if (UdiParser.TryParse(udiStr, out Udi? udi) && udi is GuidUdi guidUdi) {
                        explicitUsedGuids.Add(guidUdi.Guid);
                    }
                }

                var redirectUdis = _redirectsProvider.GetUsedMediaUdis();
                foreach (var udiStr in redirectUdis) {
                    if (UdiParser.TryParse(udiStr, out Udi? udi) && udi is GuidUdi guidUdi) {
                        explicitUsedGuids.Add(guidUdi.Guid);
                    }
                }

                // Get all media GUIDs from the SQL helper
                var allMediaGuids = _sqlHelper.GetAllMediaGuids();
                status.Total = allMediaGuids.Count;
                _logger.LogInformation("Starting scan of {TotalCount} media items. Found {ExplicitCount} explicitly used items.", status.Total, explicitUsedGuids.Count);

                // Candidates for deletion (we will filter these against used folders later)
                var unusedCandidates = new List<UnusedMediaItem>();

                // Paths of folders that are EXPLICITLY used. 
                // Any media residing in these paths should be considered used.
                var usedFolderPaths = new List<string>();

                int processedCount = 0;
                int mediaFolderCount = 0;
                int filteredByRelations = 0;
                int filteredByExplicitUsage = 0;
                int filteredFolders = 0;

                var userCache = new Dictionary<int, string?>();
                string? GetUserName(int userId) {
                    //if (userId == Constants.Security.SuperUserId) return "Admin";
                    //if (userId < 0) return "System";
                    if (userCache.TryGetValue(userId, out string? name)) return name;
                    name = _userService.GetUserById(userId)?.Name;
                    userCache[userId] = name;
                    return name;
                }

                foreach (var mediaGuid in allMediaGuids) {
                    processedCount++;
                    status.Progress = processedCount;

                    if (processedCount % 50 == 0 || processedCount == status.Total) {
                        status.ProcessedMedia.Add($"Processing item {processedCount}/{status.Total}");
                    }

                    IMedia? mediaItem = _mediaService.GetById(mediaGuid);
                    if (mediaItem == null) {
                        status.Errors.Add($"Media item with GUID {mediaGuid} not found.");
                        continue;
                    }

                    if (mediaItem.Trashed) {
                        // Skip items in recycle bin
                        continue;
                    }

                    bool isFolder = mediaItem.ContentType.Alias == "Folder";
                    if (isFolder) mediaFolderCount++;

                    string mediaUdi = mediaItem.GetUdi().ToString();

                    // Check 1: Explicitly used by Content or Redirects?
                    if (explicitUsedGuids.Contains(mediaGuid)) {
                        filteredByExplicitUsage++;

                        // IMPORTANT: If this used item is a Folder, store its path.
                        // We will use this to protect its children later.
                        if (isFolder && !string.IsNullOrEmpty(mediaItem.Path)) {
                            usedFolderPaths.Add(mediaItem.Path + ","); // Append comma to ensure exact path matching (avoid matching -1,10 vs -1,100)
                        }
                        continue;
                    }

                    // Check 2: Relations (e.g. tracking references)
                    if (_sqlHelper.IsMediaUsedInRelations(mediaGuid)) {
                        filteredByRelations++;
                        // If a folder is used in a relation, we should arguably protect its children too
                        if (isFolder && !string.IsNullOrEmpty(mediaItem.Path)) {
                            usedFolderPaths.Add(mediaItem.Path + ",");
                        }
                        continue;
                    }

                    // If it is a folder and not used, we skip adding it to the list 
                    // (The dashboard is mainly for deleting files, deleting empty folders is less critical/risky)
                    if (isFolder) {
                        filteredFolders++;
                        continue;
                    }

                    // If we get here, the item is not explicitly used, and not a relation.
                    // Add to candidates. We will check parent folder usage after the loop.
                    string? creatorName = GetUserName(mediaItem.CreatorId);
                    string? writerName = GetUserName(mediaItem.WriterId);
                    unusedCandidates.Add(new UnusedMediaItem(mediaItem, creatorName, writerName));
                }

                // FINAL PASS: Filter candidates based on Used Folder Paths
                var finalUnusedItems = new List<UnusedMediaItem>();
                int filteredByParentFolder = 0;

                foreach (var candidate in unusedCandidates) {
                    if (string.IsNullOrEmpty(candidate.Path)) {
                        finalUnusedItems.Add(candidate);
                        continue;
                    }

                    // Check if the candidate's path starts with any of the used folder paths
                    // Example: Used Folder Path: "-1,1000,"
                    //          Candidate Path:   "-1,1000,1005,1006" -> STARTS WITH -> PROTECT IT
                    bool isImplicitlyUsed = usedFolderPaths.Any(folderPath => candidate.Path.StartsWith(folderPath));

                    if (isImplicitlyUsed) {
                        filteredByParentFolder++;
                    } else {
                        finalUnusedItems.Add(candidate);
                    }
                }

                _logger.LogInformation(
                    "Scan completed: {TotalCount} total media. " +
                    "{FilteredByExplicit} explicit usage, {FilteredByRelations} relations. " +
                    "{FilteredByParentFolder} implicitly used (in used folders). " +
                    "{UnusedCount} final unused items found.",
                    status.Total, filteredByExplicitUsage, filteredByRelations, filteredByParentFolder, finalUnusedItems.Count);

                _lastUnusedMediaReport = new UnusedMediaReport(finalUnusedItems, DateTime.Now, mediaFolderCount);
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
                _lastUnusedMediaReport.ScanDate = DateTime.Now;
            }
        } else {
            _logger.LogWarning("Attempted to delete non-existent media with ID: {MediaId}", mediaId);
        }
    }

}