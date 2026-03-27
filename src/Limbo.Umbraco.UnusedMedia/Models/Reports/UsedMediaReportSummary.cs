using Newtonsoft.Json;
using Skybrud.Essentials.Time;

namespace Limbo.Umbraco.UnusedMedia.Models.Reports;

public class UsedMediaReportSummary {

    [JsonProperty("alias")]
    public string Alias { get; }

    [JsonProperty("name")]
    public string Name { get; }

    [JsonProperty("createDate")]
    public EssentialsTime CreateDate { get; }

    [JsonProperty("keys")]
    public int Keys { get; }

    public UsedMediaReportSummary(IUsedMediaReport report) {
        Alias = report.Alias;
        Name = report.Name;
        CreateDate = report.CreateDate;
        Keys = report.Keys.Count;
    }

}