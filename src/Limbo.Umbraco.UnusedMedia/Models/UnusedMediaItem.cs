using Limbo.Umbraco.UnusedMedia.Helpers;
using Newtonsoft.Json;
using Skybrud.Essentials.Time;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Models;

public class UnusedMediaItem {

    [JsonProperty("id")]
    public int Id { get; }

    [JsonProperty("key")]
    public Guid Key { get; }

    [JsonProperty("name")]
    public string Name { get; }

    [JsonProperty("url")]
    public string Url { get; }

    [JsonProperty("path")]
    public string[] Path { get; }

    [JsonProperty("createDate")]
    public EssentialsTime CreateDate { get; }

    [JsonProperty("updateDate")]
    public EssentialsTime UpdateDate { get; }

    [JsonProperty("creatorId")]
    public int CreatorId { get; }

    [JsonProperty("creatorName")]
    public string? CreatorName { get; }

    [JsonProperty("writerId")]
    public int WriterId { get; }

    [JsonProperty("writerName")]
    public string? WriterName { get; }

    [JsonProperty("cells")]
    public IReadOnlyList<UnusedMediaItemCell> Cells { get; }

    public UnusedMediaItem(IPublishedContent media, IReadOnlyList<UnusedMediaItemCell> cells) {
        Id = media.Id;
        Key = media.Key;
        Name = media.Name;
        Url = media.Url();
        Path = media.Ancestors().Select(x => x.Name).Reverse().ToArray();
        CreateDate = media.CreateDate;
        UpdateDate = media.UpdateDate;
        CreatorId = media.CreatorId;
        CreatorName = media.CreatorName();
        WriterId = media.WriterId;
        WriterName = media.WriterName();
        Cells = cells;
    }

}