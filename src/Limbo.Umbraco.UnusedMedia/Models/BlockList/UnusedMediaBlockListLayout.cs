using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Limbo.Umbraco.UnusedMedia.Models.BlockList;

public class UnusedMediaBlockListLayout : UnusedMediaJsonObjectBase {

    [JsonProperty("Umbraco.BlockList")]
    public IReadOnlyList<UnusedMediaBlockListLayoutItem> Items { get; }

    public UnusedMediaBlockListLayout(IReadOnlyList<UnusedMediaBlockListLayoutItem> items, JObject json) : base(json) {
        Items = items;
    }

}