using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection; // Added for IServiceScopeFactory
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Providers;

public class DeepScanProvider {

    private readonly IServiceScopeFactory _serviceScopeFactory; // Changed dependency
    private readonly Lazy<HashSet<string>> _usedMediaUdis; // Use Lazy for deferred execution
    private static readonly Regex MediaUdiRegex = new Regex(@"(umb:\/\/media\/([0-9a-fA-F]{32}))", RegexOptions.Compiled);

    public DeepScanProvider(IServiceScopeFactory serviceScopeFactory) { // Changed constructor
        _serviceScopeFactory = serviceScopeFactory;
        _usedMediaUdis = new Lazy<HashSet<string>>(ScanForUsedMediaUdis); // Initialize Lazy
    }

    /// <summary>
    /// Scans the entire site for media UDIs used within content properties.
    /// This method is intended to be called once to populate the internal cache.
    /// </summary>
    /// <returns>A HashSet of media UDIs found in content properties.</returns>
    private HashSet<string> ScanForUsedMediaUdis() {
        var usedMediaUdis = new HashSet<string>();
        
        using (var scope = _serviceScopeFactory.CreateScope()) { // Create a new scope
            var publishedContentQuery = scope.ServiceProvider.GetRequiredService<IPublishedContentQuery>();
            var rootContent = publishedContentQuery.ContentAtRoot();

            foreach (var content in rootContent) {
                foreach (var descendant in content.DescendantsOrSelf()) {
                    foreach (var property in descendant.Properties) {
                        var value = property.GetValue()?.ToString();
                        if (string.IsNullOrWhiteSpace(value)) {
                            continue;
                        }

                        var matches = MediaUdiRegex.Matches(value);
                        foreach (Match match in matches) {
                            usedMediaUdis.Add(match.Groups[1].Value); // Add the full UDI
                        }
                    }
                }
            }
        }

        return usedMediaUdis;
    }

    /// <summary>
    /// Checks if the given media UDI is used anywhere in the site's content properties.
    /// </summary>
    /// <param name="mediaUdi">The UDI of the media item to check (e.g., "umb://media/xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx").</param>
    /// <returns><c>true</c> if the media is used; otherwise, <c>false</c>.</returns>
    public bool IsMediaUsed(string mediaUdi) {
        return _usedMediaUdis.Value.Contains(mediaUdi); // Access .Value to trigger lazy evaluation
    }

}
