// [CHANGE: Umbraco 17 upgrade - block editors switched from UDIs to GUID keys in Umbraco 14] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

namespace Limbo.Umbraco.UnusedMedia.Models.BlockList;

/// <summary>
/// Class representing an item in the <c>Umbraco.BlockList</c> layout array.
///
/// As of Umbraco 14, blocks reference their content and settings by GUID key rather than by UDI - so the
/// <c>ContentUdi</c> and <c>SettingsUdi</c> properties of previous versions are now <see cref="ContentKey"/> and
/// <see cref="SettingsKey"/>.
/// </summary>
public class UnusedMediaBlockListLayoutItem {

    /// <summary>
    /// Gets or sets the key of the content of the block.
    /// </summary>
    public Guid ContentKey { get; set; }

    /// <summary>
    /// Gets or sets the key of the settings of the block, if any.
    /// </summary>
    public Guid? SettingsKey { get; set; }

    /// <summary>
    /// Initializes a new layout item.
    /// </summary>
    /// <param name="contentKey">The key of the content of the block.</param>
    /// <param name="settingsKey">The key of the settings of the block, if any.</param>
    public UnusedMediaBlockListLayoutItem(Guid contentKey, Guid? settingsKey) {
        ContentKey = contentKey;
        SettingsKey = settingsKey;
    }

}
