// [CHANGE: Umbraco 17 upgrade - replaces the Newtonsoft-decorated Limbo.Forms field models] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Text.Json.Serialization;

namespace Limbo.Umbraco.UnusedMedia.Models.Filters;

/// <summary>
/// Class representing a drop down filter.
/// </summary>
public class UnusedMediaDropDownFilter : UnusedMediaFilter {

    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "dropdown";

    /// <summary>
    /// Gets the items of the drop down.
    /// </summary>
    [JsonPropertyName("items")]
    public List<UnusedMediaFilterItem> Items { get; set; } = [];

    /// <summary>
    /// Initializes a new drop down filter with the specified <paramref name="alias"/>.
    /// </summary>
    /// <param name="alias">The alias of the filter.</param>
    public UnusedMediaDropDownFilter(string alias) : base(alias) { }

}

/// <summary>
/// Class representing a single item of a <see cref="UnusedMediaDropDownFilter"/>.
/// </summary>
public class UnusedMediaFilterItem {

    /// <summary>
    /// Gets the value of the item, submitted as the query string value when selected.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; }

    /// <summary>
    /// Gets the label of the item.
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; }

    /// <summary>
    /// Gets or sets the localization key the dashboard should use for the label of this item.
    /// </summary>
    [JsonPropertyName("labelKey")]
    public string? LabelKey { get; set; }

    /// <summary>
    /// Initializes a new item.
    /// </summary>
    /// <param name="value">The value of the item.</param>
    /// <param name="label">The label of the item.</param>
    /// <param name="labelKey">An optional localization key for the label.</param>
    public UnusedMediaFilterItem(object? value, string label, string? labelKey = null) {
        Value = value?.ToString() ?? string.Empty;
        Label = label;
        LabelKey = labelKey;
    }

}
