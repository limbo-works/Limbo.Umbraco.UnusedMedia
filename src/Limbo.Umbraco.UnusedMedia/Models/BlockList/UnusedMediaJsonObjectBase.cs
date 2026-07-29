// [CHANGE: Umbraco 17 upgrade - System.Text.Json] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Limbo.Umbraco.UnusedMedia.Models.BlockList;

/// <summary>
/// Base class for models wrapping a <see cref="JsonObject"/>.
///
/// Prior to the Umbraco 17 upgrade this derived from Skybrud.Essentials' Newtonsoft based <c>JsonObjectBase</c>.
/// </summary>
public class UnusedMediaJsonObjectBase {

    /// <summary>
    /// Gets the JSON object this model was parsed from.
    /// </summary>
    [JsonIgnore]
    public JsonObject JsonObject { get; }

    /// <summary>
    /// Initializes a new instance from the specified <paramref name="json"/>.
    /// </summary>
    /// <param name="json">The JSON object.</param>
    protected UnusedMediaJsonObjectBase(JsonObject json) {
        JsonObject = json;
    }

}
