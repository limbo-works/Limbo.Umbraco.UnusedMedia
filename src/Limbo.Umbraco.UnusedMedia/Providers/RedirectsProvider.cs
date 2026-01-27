using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Scans the redirects table for media UDIs used within destination URLs.
    /// This method is intended to be called once to populate the internal cache.
    /// </summary>
    /// <returns>A HashSet of media UDIs found in redirect URLs.</returns>
    private HashSet<string> ScanForUsedMediaUdis() {
        _logger.LogInformation("RedirectsProvider: Starting scan for used media UDIs in redirects");
        var usedMediaUdis = new HashSet<string>();

        using var scope = _scopeProvider.CreateScope(autoComplete: true);
        if (scope.Database.SqlContext.SqlSyntax.DoesTableExist(scope.Database, "SkybrudRedirects") == false) {
            _logger.LogInformation("RedirectsProvider: SkybrudRedirects table does not exist, skipping redirect scan");
            return usedMediaUdis;
        }

        var sql = scope.SqlContext.Sql()
            .Select("DestinationUrl")
            .From("SkybrudRedirects");

        var redirectUrls = scope.Database.Fetch<string>(sql);
        _logger.LogInformation("RedirectsProvider: Found {Count} redirects to scan", redirectUrls.Count);

        foreach (var url in redirectUrls) {
            if (string.IsNullOrWhiteSpace(url)) {
                continue;
            }

            var matches = _mediaUdiRegex.Matches(url);
            foreach (Match match in matches) {
                usedMediaUdis.Add(match.Groups[1].Value);
            }
        }

        _logger.LogInformation("RedirectsProvider: Found {MediaCount} unique media UDIs in redirects", usedMediaUdis.Count);
        return usedMediaUdis;
    }

    /// <summary>
    /// Checks if the given media UDI is used in any redirect destination URLs.
    /// </summary>
    /// <param name="mediaUdi">The UDI of the media item to check (e.g., "umb://media/xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx").</param>
    /// <returns><c>true</c> if the media is used; otherwise, <c>false</c>.</returns>
    public bool IsMediaUsed(string mediaUdi) {
        return _usedMediaUdis.Value.Contains(mediaUdi);
    }

}
