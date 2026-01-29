using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Providers;

public class DeepScanProvider {

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DeepScanProvider> _logger;
    private readonly Lazy<HashSet<string>> _usedMediaUdis;
    private static readonly Regex MediaUdiRegex = new Regex(@"(umb:\/\/media\/([0-9a-fA-F]{32}))", RegexOptions.Compiled);

    public DeepScanProvider(IServiceScopeFactory serviceScopeFactory, ILogger<DeepScanProvider> logger) {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _usedMediaUdis = new Lazy<HashSet<string>>(ScanForUsedMediaUdis);
    }

    /// <summary>
    /// Scans the entire site for media UDIs used within content properties.
    /// This method is intended to be called once to populate the internal cache.
    /// </summary>
    /// <returns>A HashSet of media UDIs found in content properties.</returns>
    private HashSet<string> ScanForUsedMediaUdis() {
        _logger.LogInformation("DeepScanProvider: Starting scan for used media UDIs in content");
        var usedMediaUdis = new HashSet<string>();
        int contentCount = 0;
        int propertyCount = 0;

        using (var scope = _serviceScopeFactory.CreateScope()) {
            var publishedContentQuery = scope.ServiceProvider.GetRequiredService<IPublishedContentQuery>();
            var rootContent = publishedContentQuery.ContentAtRoot();

            foreach (var content in rootContent) {
                foreach (var descendant in content.DescendantsOrSelf()) {
                    contentCount++;
                    foreach (var property in descendant.Properties) {
                        propertyCount++;
                        var value = property.GetValue()?.ToString();
                        if (string.IsNullOrWhiteSpace(value)) {
                            continue;
                        }

                        var matches = MediaUdiRegex.Matches(value);
                        foreach (Match match in matches) {
                            usedMediaUdis.Add(match.Groups[1].Value);
                        }
                    }
                }
            }
        }

        _logger.LogInformation("DeepScanProvider: Scanned {ContentCount} content items with {PropertyCount} properties, found {MediaCount} unique media UDIs",
            contentCount, propertyCount, usedMediaUdis.Count);

        return usedMediaUdis;
    }

    /// <summary>
    /// Checks if the given media UDI is used anywhere in the site's content properties.
    /// </summary>
    public bool IsMediaUsed(string mediaUdi) {
        return _usedMediaUdis.Value.Contains(mediaUdi);
    }

    /// <summary>
    /// Returns all identified used media UDIs.
    /// </summary>
    public HashSet<string> GetUsedMediaUdis() {
        return _usedMediaUdis.Value;
    }

}