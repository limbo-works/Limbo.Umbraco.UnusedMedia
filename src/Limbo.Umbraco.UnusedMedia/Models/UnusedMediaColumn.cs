// [CHANGE: Umbraco 17 upgrade - System.Text.Json + client side localization] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Limbo.Umbraco.UnusedMedia.Json;
using Skybrud.Essentials.Collections;

namespace Limbo.Umbraco.UnusedMedia.Models;

public class UnusedMediaColumn {

    [JsonPropertyName("alias")]
    public required string Alias { get; set; }

    /// <summary>
    /// Gets or sets the fallback name of the column. Used by the dashboard when <see cref="NameKey"/> is
    /// <see langword="null"/> or can't be resolved by the backoffice.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the localization key the dashboard should use for the name of this column.
    ///
    /// Server side localization via <c>ILocalizedTextService</c> was dropped as part of the Umbraco 17 upgrade - the
    /// new backoffice localizes in the client, so the API returns a key and an English fallback instead.
    /// </summary>
    [JsonPropertyName("nameKey")]
    public string? NameKey { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(CamelCaseEnumConverter<UnusedMediaColumnType>))]
    public required UnusedMediaColumnType Type { get; set; }

    [JsonPropertyName("allowSort")]
    public bool AllowSort { get; set; }

    [JsonPropertyName("defaultOrder")]
    [JsonConverter(typeof(CamelCaseEnumConverter<SortOrder>))]
    public SortOrder DefaultOrder { get; set; }

    public UnusedMediaColumn() { }

    [SetsRequiredMembers]
    public UnusedMediaColumn(string alias, string name, UnusedMediaColumnType type) {
        Alias = alias;
        Name = name;
        Type = type;
    }

    [SetsRequiredMembers]
    public UnusedMediaColumn(string alias, string name, UnusedMediaColumnType type, bool allowSort = false, SortOrder defaultOrder = SortOrder.Ascending) {
        Alias = alias;
        Name = name;
        Type = type;
        AllowSort = allowSort;
        DefaultOrder = defaultOrder;
    }

    /// <summary>
    /// Initializes a new column with a localization key for its name.
    /// </summary>
    [SetsRequiredMembers]
    public UnusedMediaColumn(string alias, string name, string? nameKey, UnusedMediaColumnType type, bool allowSort = false, SortOrder defaultOrder = SortOrder.Ascending) {
        Alias = alias;
        Name = name;
        NameKey = nameKey;
        Type = type;
        AllowSort = allowSort;
        DefaultOrder = defaultOrder;
    }

    public static UnusedMediaColumn CreateBytes(string alias, string name, bool allowSort = false, SortOrder defaultOrder = SortOrder.Ascending) {
        return new UnusedMediaColumn(alias, name, UnusedMediaColumnType.Bytes, allowSort, defaultOrder);
    }

    public static UnusedMediaColumn CreateDateTime(string alias, string name, bool allowSort = false, SortOrder defaultOrder = SortOrder.Ascending) {
        return new UnusedMediaColumn(alias, name, UnusedMediaColumnType.DateTime, allowSort, defaultOrder);
    }

    public static UnusedMediaColumn CreateText(string alias, string name, bool allowSort = false, SortOrder defaultOrder = SortOrder.Ascending) {
        return new UnusedMediaColumn(alias, name, UnusedMediaColumnType.Text, allowSort, defaultOrder);
    }

    public static UnusedMediaColumn CreateUser(string alias, string name, bool allowSort = false, SortOrder defaultOrder = SortOrder.Ascending) {
        return new UnusedMediaColumn(alias, name, UnusedMediaColumnType.User, allowSort, defaultOrder);
    }

}
