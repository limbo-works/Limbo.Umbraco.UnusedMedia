using System;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Models;

public class UnusedMediaItem {

    public int Id { get; set; }
    public Guid Key { get; set; }
    public string? Name { get; set; }
    public string? Path { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
    public long TotalBytes { get; set; }

    public UnusedMediaItem(IMedia media) {
        Id = media.Id;
        Key = media.Key;
        Name = media.Name;
        Path = media.Path;
        CreateDate = media.CreateDate;
        UpdateDate = media.UpdateDate;
        //TotalBytes = media.GetValue<long>(Constants.PropertyAliases.UmbracoBytes);
    }

}
