using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Skybrud.Essentials.Collections;
using Skybrud.Essentials.Json.Newtonsoft.Converters.Enums;

namespace Limbo.Umbraco.UnusedMedia.Models;

public class UnusedMediaColumn {

    [JsonProperty("alias")]
    public required string Alias { get; set; }

    [JsonProperty("name")]
    public required string Name { get; set; }

    [JsonProperty("type")]
    [JsonConverter(typeof(EnumCamelCaseConverter))]
    public required UnusedMediaColumnType Type { get; set; }

    [JsonProperty("allowSort")]
    public bool AllowSort { get; set; }

    [JsonProperty("defaultOrder")]
    [JsonConverter(typeof(EnumCamelCaseConverter))]
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