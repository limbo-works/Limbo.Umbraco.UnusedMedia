// [CHANGE: Umbraco 17 upgrade - replaces the Newtonsoft-decorated Limbo.Forms field models] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Text.Json.Serialization;

namespace Limbo.Umbraco.UnusedMedia.Models.Filters;

/// <summary>
/// Base class describing a filter shown above the unused media table in the backoffice dashboard.
///
/// Prior to the Umbraco 17 upgrade, the filters were described using the field models of the <c>Limbo.Forms</c>
/// package. Those models are decorated with Newtonsoft attributes, which the System.Text.Json based Management API
/// ignores - so the package now ships its own minimal filter models.
/// </summary>
[JsonDerivedType(typeof(UnusedMediaTextFilter))]
[JsonDerivedType(typeof(UnusedMediaDropDownFilter))]
public abstract class UnusedMediaFilter {

    /// <summary>
    /// Gets the type of the filter - eg. <c>text</c> or <c>dropdown</c>. The dashboard element uses this to
    /// determine which form control to render.
    /// </summary>
    [JsonPropertyName("type")]
    public abstract string Type { get; }

    /// <summary>
    /// Gets the alias of the filter. The alias is used as the query string parameter when requesting the list of
    /// unused media.
    /// </summary>
    [JsonPropertyName("alias")]
    public string Alias { get; }

    /// <summary>
    /// Gets or sets the localization key the dashboard should use for the label of this filter. May be
    /// <see langword="null"/>, in which case <see cref="Label"/> is shown as-is.
    /// </summary>
    [JsonPropertyName("labelKey")]
    public string? LabelKey { get; set; }

    /// <summary>
    /// Gets or sets the fallback label of this filter, used when <see cref="LabelKey"/> is <see langword="null"/> or
    /// can't be resolved by the backoffice.
    /// </summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>
    /// Initializes a new filter with the specified <paramref name="alias"/>.
    /// </summary>
    /// <param name="alias">The alias of the filter.</param>
    protected UnusedMediaFilter(string alias) {
        Alias = alias;
    }

}
