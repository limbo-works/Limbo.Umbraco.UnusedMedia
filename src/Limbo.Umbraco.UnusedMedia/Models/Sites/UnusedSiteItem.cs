using Newtonsoft.Json;

namespace Limbo.Umbraco.UnusedMedia.Models.Sites;

public class UnusedSiteItem {

    [JsonProperty("id")]
    public required int Id { get; init; }

    [JsonProperty("key")]
    public required Guid Key { get; init; }

    [JsonProperty("name")]
    public required string Name { get; init; }

    [JsonProperty("mediaFolderId")]
    public int? MediaFolderId { get; init; }

    public UnusedSiteItem() { }

    public UnusedSiteItem(int id, Guid key, string name, int? mediaFolderId) {
        Id = id;
        Key = key;
        Name = name;
        MediaFolderId = mediaFolderId;
    }

}