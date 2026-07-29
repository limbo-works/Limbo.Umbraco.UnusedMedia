// [CHANGE: Umbraco 17 upgrade - System.Text.Json] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Limbo.Umbraco.UnusedMedia.Models.BlockList;

public class UnusedMediaBlockListModel : UnusedMediaJsonObjectBase {

    [JsonPropertyName("layout")]
    public UnusedMediaBlockListLayout Layout { get; }

    [JsonPropertyName("contentData")]
    public IReadOnlyList<UnusedMediaBlockListContentData> ContentData { get; }

    [JsonPropertyName("settingsData")]
    public IReadOnlyList<UnusedMediaBlockListContentData> SettingsData { get; }

    [JsonIgnore]
    public List<UnusedMediaBlockListItem> Blocks { get; }

    public UnusedMediaBlockListModel(UnusedMediaBlockListLayout layout, IReadOnlyList<UnusedMediaBlockListContentData> contentData, IReadOnlyList<UnusedMediaBlockListContentData> settingsData, List<UnusedMediaBlockListItem> blocks, JsonObject json) : base(json) {
        Layout = layout;
        ContentData = contentData;
        SettingsData = settingsData;
        Blocks = blocks;
    }

}
