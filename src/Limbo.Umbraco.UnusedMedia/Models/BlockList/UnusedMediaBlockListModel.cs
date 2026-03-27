using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Limbo.Umbraco.UnusedMedia.Models.BlockList;

public class UnusedMediaBlockListModel : UnusedMediaJsonObjectBase {

    [JsonProperty("layout")]
    public UnusedMediaBlockListLayout Layout { get; }

    [JsonProperty("contentData")]
    public IReadOnlyList<UnusedMediaBlockListContentData> ContentData { get; }

    [JsonProperty("settingsData")]
    public IReadOnlyList<UnusedMediaBlockListContentData> SettingsData { get; }

    [JsonIgnore]
    public List<UnusedMediaBlockListItem> Blocks { get; }

    public UnusedMediaBlockListModel(UnusedMediaBlockListLayout layout, IReadOnlyList<UnusedMediaBlockListContentData> contentData, IReadOnlyList<UnusedMediaBlockListContentData> settingsData, List<UnusedMediaBlockListItem> blocks, JObject json) : base(json) {
        Layout = layout;
        ContentData = contentData;
        SettingsData = settingsData;
        Blocks = blocks;
    }

}