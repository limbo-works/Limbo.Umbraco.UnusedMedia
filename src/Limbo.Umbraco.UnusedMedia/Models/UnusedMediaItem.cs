using System;
using Newtonsoft.Json;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Models;

public class UnusedMediaItem {

    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonProperty("key")]
    public Guid Key { get; set; }
    
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [JsonProperty("path")]
    public string? Path { get; set; }
    
    [JsonProperty("createDate")]
    public DateTime CreateDate { get; set; }
    
    [JsonProperty("updateDate")]
    public DateTime UpdateDate { get; set; }
    
    [JsonProperty("totalBytes")]
    public long TotalBytes { get; set; }

    public UnusedMediaItem(IMedia media) {
        Id = media.Id;
        Key = media.Key;
        Name = media.Name;
        Path = media.Path;
        CreateDate = media.CreateDate;
        UpdateDate = media.UpdateDate;
        TotalBytes = media.GetValue<long>("umbracoBytes");
    }

}
