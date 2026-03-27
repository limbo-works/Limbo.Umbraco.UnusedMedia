using Newtonsoft.Json.Linq;
using Skybrud.Essentials.Exceptions;
using Skybrud.Essentials.Json.Newtonsoft.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Models.BlockList;

public class UnusedMediaBlockListUtils {

    public static UnusedMediaBlockListModel ParseBlockList(JObject json) {

        UnusedMediaBlockListLayout layout = json.GetRequiredObject("layout", ParseBlockListLayout);

        IReadOnlyList<UnusedMediaBlockListContentData> contentData = json.GetRequiredArray("contentData", ParseBlockListContentData);
        IReadOnlyList<UnusedMediaBlockListContentData> settingsData = json.GetRequiredArray("settingsData", ParseBlockListContentData);

        var contentDataLookup = contentData.ToDictionary(x => x.Udi);
        var settingsDataLookup = settingsData.ToDictionary(x => x.Udi);

        List<UnusedMediaBlockListItem> blocks = [];

        foreach (UnusedMediaBlockListLayoutItem item in layout.Items) {

            if (!contentDataLookup.TryGetValue(item.ContentUdi, out var content)) throw new WtfException();
            settingsDataLookup.TryGetValue(item.SettingsUdi ?? "", out var settings);

            blocks.Add(new UnusedMediaBlockListItem(item, content, settings));

        }

        return new UnusedMediaBlockListModel(layout, contentData, settingsData, blocks, json);

    }

    public static UnusedMediaBlockListLayout ParseBlockListLayout(JObject json) {
        UnusedMediaBlockListLayoutItem[] items = json.GetRequiredArray("Umbraco.BlockList", ParseBlockListLayoutItem);
        return new UnusedMediaBlockListLayout(items, json);
    }

    public static UnusedMediaBlockListLayoutItem ParseBlockListLayoutItem(JObject json) {
        var contentUdi = json.GetRequiredString("contentUdi");
        var settingsUdi = json.GetString("settingsUdi");
        return new UnusedMediaBlockListLayoutItem(contentUdi, settingsUdi);
    }

    public static UnusedMediaBlockListContentData ParseBlockListContentData(JObject json) {

        Guid contentTypeKey = json.GetRequiredGuid("contentTypeKey");
        string udi = json.GetRequiredString("udi");

        Dictionary<string, object?> properties = [];

        foreach (var property in json.Properties()) {
            if (property.Name is "contentTypeKey" or "udi") continue;

            if (property.Value.Type == JTokenType.String && property.Value.ToString().StartsWith("{\"layout\":{\"Umbraco.BlockList\":")) {
                properties[property.Name] = ParseBlockList(JObject.Parse(property.Value.ToString()));
            } else {
                properties[property.Name] = property.Value.Type == JTokenType.Null ? null : property.Value.ToObject<object>();
            }
        }

        return new UnusedMediaBlockListContentData(contentTypeKey, udi, properties);

    }

}
