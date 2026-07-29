// [CHANGE: Umbraco 17 upgrade - System.Text.Json + Umbraco 14 block list format] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Text.Json.Nodes;
using Limbo.Umbraco.UnusedMedia.Json;
using Limbo.Umbraco.UnusedMedia.Models.BlockList;

namespace Limbo.Umbraco.UnusedMedia.BlockList;

/// <summary>
/// Parser for the raw JSON value of a block list property.
///
/// The format changed in Umbraco 14: layout items reference <c>contentKey</c>/<c>settingsKey</c> instead of
/// <c>contentUdi</c>/<c>settingsUdi</c>, entries in <c>contentData</c>/<c>settingsData</c> are identified by
/// <c>key</c> instead of <c>udi</c>, and their property values live in a <c>values</c> array instead of being
/// flattened onto the entry. Nested block values are now nested JSON objects rather than JSON encoded strings.
/// </summary>
public class UnusedMediaBlockListParser {

    /// <summary>
    /// Parses the specified block list <paramref name="json"/>.
    /// </summary>
    /// <param name="json">The raw JSON value of the block list property.</param>
    /// <returns>An instance of <see cref="UnusedMediaBlockListModel"/>.</returns>
    public virtual UnusedMediaBlockListModel ParseBlockList(JsonObject json) {

        JsonObject layoutJson = json["layout"] as JsonObject ?? throw new JsonParseException("Block list value doesn't have a 'layout' object.");

        UnusedMediaBlockListLayout layout = ParseBlockListLayout(layoutJson);

        IReadOnlyList<UnusedMediaBlockListContentData> contentData = json.GetArrayItems("contentData", ParseBlockListContentData);
        IReadOnlyList<UnusedMediaBlockListContentData> settingsData = json.GetArrayItems("settingsData", ParseBlockListContentData);

        Dictionary<Guid, UnusedMediaBlockListContentData> contentDataLookup = [];
        foreach (UnusedMediaBlockListContentData content in contentData) contentDataLookup.TryAdd(content.Key, content);

        Dictionary<Guid, UnusedMediaBlockListContentData> settingsDataLookup = [];
        foreach (UnusedMediaBlockListContentData settings in settingsData) settingsDataLookup.TryAdd(settings.Key, settings);

        List<UnusedMediaBlockListItem> blocks = [];

        foreach (UnusedMediaBlockListLayoutItem item in layout.Items) {

            // A layout item referencing content that isn't in "contentData" means the value is corrupt. Skip the
            // block rather than failing the entire scan - a single bad property shouldn't hide every unused media.
            if (!contentDataLookup.TryGetValue(item.ContentKey, out UnusedMediaBlockListContentData? content)) continue;

            UnusedMediaBlockListContentData? settings = null;
            if (item.SettingsKey is { } settingsKey) settingsDataLookup.TryGetValue(settingsKey, out settings);

            blocks.Add(new UnusedMediaBlockListItem(item, content, settings));

        }

        return new UnusedMediaBlockListModel(layout, contentData, settingsData, blocks, json);

    }

    /// <summary>
    /// Parses the <c>layout</c> object of a block list value.
    /// </summary>
    public virtual UnusedMediaBlockListLayout ParseBlockListLayout(JsonObject json) {
        IReadOnlyList<UnusedMediaBlockListLayoutItem> items = json.GetArrayItems("Umbraco.BlockList", ParseBlockListLayoutItem);
        return new UnusedMediaBlockListLayout(items, json);
    }

    /// <summary>
    /// Parses a single item of the <c>Umbraco.BlockList</c> layout array.
    /// </summary>
    public virtual UnusedMediaBlockListLayoutItem ParseBlockListLayoutItem(JsonObject json) {
        Guid contentKey = json.GetRequiredGuid("contentKey");
        Guid? settingsKey = json.GetGuid("settingsKey");
        return new UnusedMediaBlockListLayoutItem(contentKey, settingsKey);
    }

    /// <summary>
    /// Parses a single entry of the <c>contentData</c> or <c>settingsData</c> array.
    /// </summary>
    public virtual UnusedMediaBlockListContentData ParseBlockListContentData(JsonObject json) {

        Guid contentTypeKey = json.GetRequiredGuid("contentTypeKey");
        Guid key = json.GetRequiredGuid("key");

        Dictionary<string, object?> properties = [];
        Dictionary<string, string?> editorAliases = [];

        foreach (JsonNode? item in json.GetArray("values") ?? []) {

            if (item is not JsonObject value) continue;

            string? alias = value.GetString("alias");
            if (string.IsNullOrWhiteSpace(alias)) continue;

            editorAliases[alias] = value.GetString("editorAlias");

            JsonNode? node = value["value"];

            // A nested block list is now a nested JSON object rather than a JSON encoded string
            if (node is JsonObject nested && nested["layout"] is JsonObject nestedLayout && nestedLayout.ContainsKey("Umbraco.BlockList")) {
                properties[alias] = ParseBlockList(nested);
                continue;
            }

            properties[alias] = node;

        }

        return new UnusedMediaBlockListContentData(contentTypeKey, key, properties, editorAliases);

    }

}
