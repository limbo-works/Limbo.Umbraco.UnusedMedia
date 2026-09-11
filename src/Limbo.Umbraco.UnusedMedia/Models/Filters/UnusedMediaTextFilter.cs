// [CHANGE: Umbraco 17 upgrade - replaces the Newtonsoft-decorated Limbo.Forms field models] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Text.Json.Serialization;

namespace Limbo.Umbraco.UnusedMedia.Models.Filters;

/// <summary>
/// Class representing a free text filter.
/// </summary>
public class UnusedMediaTextFilter : UnusedMediaFilter {

    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "text";

    /// <summary>
    /// Gets or sets the localization key for the placeholder of the text field.
    /// </summary>
    [JsonPropertyName("placeholderKey")]
    public string? PlaceholderKey { get; set; }

    /// <summary>
    /// Gets or sets the fallback placeholder of the text field.
    /// </summary>
    [JsonPropertyName("placeholder")]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Initializes a new text filter with the specified <paramref name="alias"/>.
    /// </summary>
    /// <param name="alias">The alias of the filter.</param>
    public UnusedMediaTextFilter(string alias) : base(alias) { }

}
