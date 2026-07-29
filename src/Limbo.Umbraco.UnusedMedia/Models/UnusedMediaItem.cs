// [CHANGE: Umbraco 17 upgrade - System.Text.Json + DateTimeOffset] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Text.Json.Serialization;
using Limbo.Umbraco.UnusedMedia.Helpers;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Models;

public class UnusedMediaItem {

    [JsonPropertyName("id")]
    public int Id { get; }

    [JsonPropertyName("key")]
    public Guid Key { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("url")]
    public string Url { get; }

    [JsonPropertyName("path")]
    public string[] Path { get; }

    [JsonPropertyName("createDate")]
    public DateTimeOffset CreateDate { get; }

    [JsonPropertyName("updateDate")]
    public DateTimeOffset UpdateDate { get; }

    [JsonPropertyName("creatorId")]
    public int CreatorId { get; }

    [JsonPropertyName("creatorName")]
    public string? CreatorName { get; }

    [JsonPropertyName("writerId")]
    public int WriterId { get; }

    [JsonPropertyName("writerName")]
    public string? WriterName { get; }

    [JsonPropertyName("cells")]
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
