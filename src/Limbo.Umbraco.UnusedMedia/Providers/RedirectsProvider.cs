using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Scoping;

namespace Limbo.Umbraco.UnusedMedia.Providers;

public class RedirectsProvider {

    private readonly IScopeProvider _scopeProvider;
    private readonly ILogger<RedirectsProvider> _logger;
    private readonly Lazy<HashSet<string>> _usedMediaUdis;
    private static readonly Regex _mediaUdiRegex = new Regex(@"(umb:\/\/media\/([0-9a-fA-F]{32}))", RegexOptions.Compiled);

    public RedirectsProvider(IScopeProvider scopeProvider, ILogger<RedirectsProvider> logger) {
        _scopeProvider = scopeProvider;
        _logger = logger;
        _usedMediaUdis = new Lazy<HashSet<string>>(ScanForUsedMediaUdis);
    }

    /// <summary>
    /// Scans the redirects table for media UDIs used within destination URLs and destination keys.
    /// This method is intended to be called once to populate the internal cache.
    /// </summary>
    /// <returns>A HashSet of media UDIs found in redirect URLs and keys.</returns>
    private HashSet<string> ScanForUsedMediaUdis() {
        _logger.LogInformation("RedirectsProvider: Starting scan for used media UDIs in redirects");
        var usedMediaUdis = new HashSet<string>();

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
                    usedMediaUdis.Add(match.Groups[1].Value);
                }
            }

            // Check DestinationKey for media GUIDs and convert to UDI
            if (redirect.DestinationKey.HasValue && redirect.DestinationKey.Value != Guid.Empty) {
                // Convert GUID to UDI format: umb://media/{guid without hyphens}
                string guidWithoutHyphens = redirect.DestinationKey.Value.ToString("N");
                string mediaUdi = $"umb://media/{guidWithoutHyphens}";
                usedMediaUdis.Add(mediaUdi);
                _logger.LogDebug("RedirectsProvider: Found media key {Key}, converted to UDI {Udi}",
                    redirect.DestinationKey.Value, mediaUdi);
            }
        }

        _logger.LogInformation("RedirectsProvider: Found {MediaCount} unique media UDIs in redirects", usedMediaUdis.Count);
        return usedMediaUdis;
    }

    /// <summary>
    /// Checks if the given media UDI is used in any redirect destination URLs or keys.
    /// </summary>
    /// <param name="mediaUdi">The UDI of the media item to check (e.g., "umb://media/xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx").</param>
    /// <returns><c>true</c> if the media is used; otherwise, <c>false</c>.</returns>
    public bool IsMediaUsed(string mediaUdi) {
        return _usedMediaUdis.Value.Contains(mediaUdi);
    }

    // Helper class to map database results
    private class RedirectData {
        public string? DestinationUrl { get; set; }
        public Guid? DestinationKey { get; set; }
    }

}
