using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Scoping;

namespace Limbo.Umbraco.UnusedMedia.Providers;

public class RedirectsUsedMediaProvider : UsedMediaProvider {

    private readonly IScopeProvider _scopeProvider;
    private readonly ILogger<RedirectsUsedMediaProvider> _logger;
    private readonly Lazy<HashSet<Guid>> _usedMediaUdis;
    private static readonly Regex _mediaUdiRegex = new Regex(@"(umb:\/\/media\/([0-9a-fA-F]{32}))", RegexOptions.Compiled);

    public RedirectsUsedMediaProvider(IScopeProvider scopeProvider, ILogger<RedirectsUsedMediaProvider> logger) {
        _scopeProvider = scopeProvider;
        _logger = logger;
        _usedMediaUdis = new Lazy<HashSet<Guid>>(ScanForUsedMediaKeys);
    }

    /// <summary>
    /// Scans the redirects table for media UDIs used within destination URLs and destination keys.
    /// This method is intended to be called once to populate the internal cache.
    /// </summary>
    public override HashSet<Guid> ScanForUsedMediaKeys() {

        _logger.LogInformation("RedirectsProvider: Starting scan for used media UDIs in redirects");

        HashSet<Guid> usedMediaUdis = [];

        using var scope = _scopeProvider.CreateScope(autoComplete: true);
        if (scope.Database.SqlContext.SqlSyntax.DoesTableExist(scope.Database, "SkybrudRedirects") == false) {
            _logger.LogInformation("RedirectsProvider: SkybrudRedirects table does not exist, skipping redirect scan");
            return usedMediaUdis;
        }

        // Fetch both DestinationUrl and DestinationKey
        var sql = scope.SqlContext.Sql()
            .Select("DestinationUrl", "DestinationKey")
            .From("SkybrudRedirects");

        var redirects = scope.Database.Fetch<RedirectData>(sql);
        _logger.LogInformation("RedirectsProvider: Found {Count} redirects to scan", redirects.Count);

        foreach (var redirect in redirects) {

            // Check DestinationUrl for media UDIs
            if (!string.IsNullOrWhiteSpace(redirect.DestinationUrl)) {
                var matches = _mediaUdiRegex.Matches(redirect.DestinationUrl);
                foreach (Match match in matches) {
                    usedMediaUdis.Add(Guid.Parse(match.Groups[2].Value));
                }
            }

            // Check DestinationKey for media GUIDs and convert to UDI
            if (redirect.DestinationKey.HasValue && redirect.DestinationKey.Value != Guid.Empty) {
                usedMediaUdis.Add(redirect.DestinationKey.Value);
            }

        }

        _logger.LogInformation("RedirectsProvider: Found {MediaCount} unique media UDIs in redirects", usedMediaUdis.Count);
        return usedMediaUdis;
    }

    /// <summary>
    /// Checks if the given media UDI is used in any redirect destination URLs or keys.
    /// </summary>
    public bool IsMediaUsed(Guid key) {
        return _usedMediaUdis.Value.Contains(key);
    }

    /// <summary>
    /// Returns all identified used media UDIs.
    /// </summary>
    public HashSet<Guid> GetUsedMediaUdis() {
        return _usedMediaUdis.Value;
    }

    // Helper class to map database results
    private class RedirectData {
        public string? DestinationUrl { get; set; }
        public Guid? DestinationKey { get; set; }
    }

}