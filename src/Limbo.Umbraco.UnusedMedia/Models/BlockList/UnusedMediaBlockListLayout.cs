// [CHANGE: Umbraco 17 upgrade - System.Text.Json] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Limbo.Umbraco.UnusedMedia.Models.BlockList;

public class UnusedMediaBlockListLayout : UnusedMediaJsonObjectBase {

    [JsonPropertyName("Umbraco.BlockList")]
    public IReadOnlyList<UnusedMediaBlockListLayoutItem> Items { get; }

    public UnusedMediaBlockListLayout(IReadOnlyList<UnusedMediaBlockListLayoutItem> items, JsonObject json) : base(json) {
        Items = items;
    }

}
