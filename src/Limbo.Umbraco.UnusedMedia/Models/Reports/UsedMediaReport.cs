// [CHANGE: Umbraco 17 upgrade - System.Text.Json + DateTimeOffset] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Limbo.Umbraco.UnusedMedia.Providers;
using Umbraco.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Models.Reports;

public class UsedMediaReport : IUsedMediaReport {

    [JsonPropertyName("alias")]
    public required string Alias { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("createDate")]
    public required DateTimeOffset CreateDate { get; init; }

    [JsonPropertyName("keys")]
    public required ISet<Guid> Keys { get; init; }

    public UsedMediaReport() { }

    [SetsRequiredMembers]
    public UsedMediaReport(string alias, string name, DateTimeOffset createDate, ISet<Guid> keys) {
        Alias = alias;
        Name = name;
        CreateDate = createDate;
        Keys = keys;
    }

    [SetsRequiredMembers]
    public UsedMediaReport(UsedMediaProvider provider, DateTimeOffset createDate, HashSet<Guid> keys) {
        Alias = provider.GetType().GetFullNameWithAssembly();
        Name = provider.GetType().Name;
        CreateDate = createDate;
        Keys = keys;
    }

}
