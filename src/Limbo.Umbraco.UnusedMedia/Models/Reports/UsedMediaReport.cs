using System.Diagnostics.CodeAnalysis;
using Limbo.Umbraco.UnusedMedia.Providers;
using Newtonsoft.Json;
using Skybrud.Essentials.Time;
using Umbraco.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Models.Reports;

public class UsedMediaReport : IUsedMediaReport {

    [JsonProperty("alias")]
    public required string Alias { get; init; }

    [JsonProperty("name")]
    public required string Name { get; init; }

    [JsonProperty("createDate")]
    public required EssentialsTime CreateDate { get; init; }

    [JsonProperty("keys")]
    public required ISet<Guid> Keys { get; init; }

    public UsedMediaReport() { }

    [SetsRequiredMembers]
    public UsedMediaReport(string alias, string name, EssentialsTime createDate, ISet<Guid> keys) {
        Alias = alias;
        Name = name;
        CreateDate = createDate;
        Keys = keys;
    }

    [SetsRequiredMembers]
    public UsedMediaReport(UsedMediaProvider provider, EssentialsTime createDate, HashSet<Guid> keys) {
        Alias = provider.GetType().GetFullNameWithAssembly();
        Name = provider.GetType().Name;
        CreateDate = createDate;
        Keys = keys;
    }

}