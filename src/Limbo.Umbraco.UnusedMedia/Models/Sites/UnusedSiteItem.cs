// [CHANGE: Umbraco 17 upgrade - System.Text.Json] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Text.Json.Serialization;

namespace Limbo.Umbraco.UnusedMedia.Models.Sites;

public class UnusedSiteItem {

    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("key")]
    public required Guid Key { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("mediaFolderId")]
    public int? MediaFolderId { get; init; }

    public UnusedSiteItem() { }

    public UnusedSiteItem(int id, Guid key, string name, int? mediaFolderId) {
        Id = id;
        Key = key;
        Name = name;
        MediaFolderId = mediaFolderId;
    }

}
