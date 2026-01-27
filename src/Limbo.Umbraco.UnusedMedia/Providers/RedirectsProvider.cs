using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Umbraco.Cms.Infrastructure.Scoping;

namespace Limbo.Umbraco.UnusedMedia.Providers;

public class RedirectsProvider {

    private readonly IScopeProvider _scopeProvider;
    private readonly Lazy<HashSet<string>> _usedMediaUdis; // Use Lazy for deferred execution
    private static readonly Regex _mediaUdiRegex = new Regex(@"(umb:\/\/media\/([0-9a-fA-F]{32}))", RegexOptions.Compiled);

    public RedirectsProvider(IScopeProvider scopeProvider) {
        _scopeProvider = scopeProvider;
        _usedMediaUdis = new Lazy<HashSet<string>>(ScanForUsedMediaUdis); // Initialize Lazy
    }

    /// <summary>
    /// Scans the redirects table for media UDIs used within destination URLs.
    /// This method is intended to be called once to populate the internal cache.
    /// </summary>
    /// <returns>A HashSet of media UDIs found in redirect URLs.</returns>
    private HashSet<string> ScanForUsedMediaUdis() {
        var usedMediaUdis = new HashSet<string>();

        using var scope = _scopeProvider.CreateScope(autoComplete: true);
        if (scope.Database.SqlContext.SqlSyntax.DoesTableExist(scope.Database, "SkybrudRedirects") == false) {
            return usedMediaUdis;
        }

        var sql = scope.SqlContext.Sql()
            .Select("DestinationUrl")
            .From("SkybrudRedirects");

        var redirectUrls = scope.Database.Fetch<string>(sql);

        foreach (var url in redirectUrls) {
            if (string.IsNullOrWhiteSpace(url)) {
                continue;
            }

            var matches = _mediaUdiRegex.Matches(url);
            foreach (Match match in matches) {
                usedMediaUdis.Add(match.Groups[1].Value); // Add the full UDI
            }
        }

        return usedMediaUdis;
    }

    /// <summary>
    /// Checks if the given media UDI is used in any redirect destination URLs.
    /// </summary>
    /// <param name="mediaUdi">The UDI of the media item to check (e.g., "umb://media/xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx").</param>
    /// <returns><c>true</c> if the media is used; otherwise, <c>false</c>.</returns>
    public bool IsMediaUsed(string mediaUdi) {
        return _usedMediaUdis.Value.Contains(mediaUdi); // Access .Value to trigger lazy evaluation
    }

}
