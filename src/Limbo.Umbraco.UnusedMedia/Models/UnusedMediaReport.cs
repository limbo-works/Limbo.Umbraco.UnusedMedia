using System;
using System.Collections.Generic;

namespace Limbo.Umbraco.UnusedMedia.Models;

public class UnusedMediaReport {

    public List<UnusedMediaItem> MediaItems { get; set; }

    public DateTime? ScanDate { get; set; }

    public int MediaFolderCount { get; set; }

    public UnusedMediaReport(List<UnusedMediaItem> mediaItems, DateTime? scanDate, int mediaFolderCount) {
        MediaItems = mediaItems;
        ScanDate = scanDate;
        MediaFolderCount = mediaFolderCount;
    }

}
