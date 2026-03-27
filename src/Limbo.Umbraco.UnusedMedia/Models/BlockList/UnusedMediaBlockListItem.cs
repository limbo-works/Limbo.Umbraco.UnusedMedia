namespace Limbo.Umbraco.UnusedMedia.Models.BlockList;

public class UnusedMediaBlockListItem {

    public string ContentUdi {
        get => Item.ContentUdi;
        //set {
        //    Item.ContentUdi = value;
        //    Content.Udi = value;
        //}
    }

    public string? SettingsUdi {
        get => Item.SettingsUdi;
        //set {
        //    Item.SettingsUdi = value;
        //    if (Settings is not null) Settings.Udi = value;
        //}
    }

    public UnusedMediaBlockListLayoutItem Item { get; }

    public UnusedMediaBlockListContentData Content { get; }

    public UnusedMediaBlockListContentData? Settings { get; }

    public UnusedMediaBlockListItem(UnusedMediaBlockListLayoutItem item, UnusedMediaBlockListContentData content, UnusedMediaBlockListContentData? settings) {
        Item = item;
        Content = content;
        Settings = settings;
    }

}