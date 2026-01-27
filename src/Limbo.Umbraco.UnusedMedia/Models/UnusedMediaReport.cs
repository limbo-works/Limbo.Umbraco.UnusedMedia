using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Limbo.Umbraco.UnusedMedia.Models;

public class UnusedMediaReport {

    [JsonProperty("media")]
    public List<UnusedMediaItem> MediaItems { get; set; }

    [JsonProperty("scanDate")]
    public DateTime? ScanDate { get; set; }

    [JsonProperty("mediaFolderCount")]
    public int MediaFolderCount { get; set; }

    public UnusedMediaReport(List<UnusedMediaItem> mediaItems, DateTime? scanDate, int mediaFolderCount) {
        MediaItems = mediaItems;
        ScanDate = scanDate;
        MediaFolderCount = mediaFolderCount;
    }

}
