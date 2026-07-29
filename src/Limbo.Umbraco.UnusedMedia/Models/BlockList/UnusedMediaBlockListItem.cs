// [CHANGE: Umbraco 17 upgrade - block editors switched from UDIs to GUID keys in Umbraco 14] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

namespace Limbo.Umbraco.UnusedMedia.Models.BlockList;

public class UnusedMediaBlockListItem {

    /// <summary>
    /// Gets the key of the content of the block.
    /// </summary>
    public Guid ContentKey => Item.ContentKey;

    /// <summary>
    /// Gets the key of the settings of the block, if any.
    /// </summary>
    public Guid? SettingsKey => Item.SettingsKey;

    public UnusedMediaBlockListLayoutItem Item { get; }

    public UnusedMediaBlockListContentData Content { get; }

    public UnusedMediaBlockListContentData? Settings { get; }

    public UnusedMediaBlockListItem(UnusedMediaBlockListLayoutItem item, UnusedMediaBlockListContentData content, UnusedMediaBlockListContentData? settings) {
        Item = item;
        Content = content;
        Settings = settings;
    }

}
