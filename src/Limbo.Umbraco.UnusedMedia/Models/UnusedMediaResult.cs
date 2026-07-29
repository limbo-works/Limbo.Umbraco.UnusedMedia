// [CHANGE: Umbraco 17 upgrade - System.Text.Json] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Text.Json.Serialization;
using Limbo.Umbraco.UnusedMedia.Json;
using Limbo.Umbraco.UnusedMedia.Models.Reports;
using Skybrud.Essentials.Collections;

namespace Limbo.Umbraco.UnusedMedia.Models;

public class UnusedMediaResult {

    [JsonPropertyName("total")]
    public int Total { get; }

    [JsonPropertyName("unused")]
    public int Unused { get; }

    [JsonPropertyName("unusedPercent")]
    public double UnusedPercent { get; }

    [JsonPropertyName("used")]
    public int Used { get; }

    [JsonPropertyName("usedPercent")]
    public double UsedPercent { get; }

    [JsonPropertyName("limit")]
    public int Limit { get; }

    [JsonPropertyName("offset")]
    public int Offset { get; }

    [JsonPropertyName("page")]
    public int Page { get; }

    [JsonPropertyName("pages")]
    public int Pages { get; }

    [JsonPropertyName("sortField")]
    public string? SortField { get; }

    [JsonPropertyName("sortOrder")]
    [JsonConverter(typeof(CamelCaseEnumConverter<SortOrder>))]
    public SortOrder SortOrder { get; }

    [JsonPropertyName("reports")]
    public IReadOnlyList<UsedMediaReportSummary> Reports { get; }

    [JsonPropertyName("columns")]
    public IReadOnlyList<UnusedMediaColumn> Columns { get; }

    [JsonPropertyName("items")]
    public IEnumerable<UnusedMediaItem> Items { get; }

    public UnusedMediaResult(int total, int unused, int limit, int offset, int page, int pages, string? sortField, SortOrder sortOrder, IReadOnlyList<UsedMediaReportSummary> reports, IReadOnlyList<UnusedMediaColumn> columns, IEnumerable<UnusedMediaItem> items) {

        Total = total;
        Unused = unused;

        Limit = limit;
        Offset = offset;
        Page = page;
        Pages = pages;

        SortField = sortField;
        SortOrder = sortOrder;

        Reports = reports;
        Columns = columns;
        Items = items;

        Used = total - unused;
        UnusedPercent = total == 0 ? -1 : unused / (double) total * 100;
        UsedPercent = total == 0 ? -1 : Used / (double) total * 100;

    }

}
