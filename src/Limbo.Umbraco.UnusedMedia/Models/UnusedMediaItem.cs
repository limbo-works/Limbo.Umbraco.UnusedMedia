using Newtonsoft.Json;
using Umbraco.Cms.Core.Models;

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

    [JsonProperty("creatorName")]
    public string? CreatorName { get; set; }

    [JsonProperty("writerName")]
    public string? WriterName { get; set; }

    public UnusedMediaItem(IMedia media, string? creatorName, string? writerName) {
        Id = media.Id;
        Key = media.Key;
        Name = media.Name;
        Path = media.Path;
        CreateDate = media.CreateDate;
        UpdateDate = media.UpdateDate;
        TotalBytes = media.GetValue<long>("umbracoBytes");
        CreatorName = creatorName;
        WriterName = writerName;
    }

}
