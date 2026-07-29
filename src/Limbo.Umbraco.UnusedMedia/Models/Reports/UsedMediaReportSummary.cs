// [CHANGE: Umbraco 17 upgrade - System.Text.Json + DateTimeOffset] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Text.Json.Serialization;

namespace Limbo.Umbraco.UnusedMedia.Models.Reports;

public class UsedMediaReportSummary {

    [JsonPropertyName("alias")]
    public string Alias { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("createDate")]
    public DateTimeOffset CreateDate { get; }

    [JsonPropertyName("keys")]
    public int Keys { get; }

    public UsedMediaReportSummary(IUsedMediaReport report) {
        Alias = report.Alias;
        Name = report.Name;
        CreateDate = report.CreateDate;
        Keys = report.Keys.Count;
    }

}
