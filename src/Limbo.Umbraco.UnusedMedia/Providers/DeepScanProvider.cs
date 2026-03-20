using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Skybrud.Essentials.Collections.Extensions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Providers;

public class DeepScanProvider {

    private static readonly Regex _mediaUdiRegex = new(@"(umb:\/\/media\/([0-9a-fA-F]{32}))", RegexOptions.Compiled);
    private static readonly Regex _mediaKeyRegex = new("(^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$)", RegexOptions.Compiled);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DeepScanProvider> _logger;

    private HashSet<Guid>? _usedMediaKeys;
    private HashSet<string>? _usedMediaUdis;

    public DeepScanProvider(IServiceScopeFactory serviceScopeFactory, ILogger<DeepScanProvider> logger) {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Scans the entire site for media UDIs and GUID keys used within content properties.
    /// This method is intended to be called once to populate the internal cache.
    /// </summary>
    /// <returns>A HashSet of media keys found in content properties.</returns>
    private HashSet<Guid> ScanForUsedMediaKeys() {

        _logger.LogInformation("DeepScanProvider: Starting scan for used media UDIs in content");

        HashSet<Guid> usedMediaKeys = [];
        int contentCount = 0;
        int propertyCount = 0;

        using (IServiceScope scope = _serviceScopeFactory.CreateScope()) {

            IPublishedContentQuery publishedContentQuery = scope.ServiceProvider.GetRequiredService<IPublishedContentQuery>();
            IEnumerable<IPublishedContent> rootContent = publishedContentQuery.ContentAtRoot();

            foreach (IPublishedContent content in rootContent) {
                foreach (IPublishedContent descendant in content.DescendantsOrSelf()) {
                    contentCount++;
                    foreach (IPublishedProperty property in descendant.Properties) {
                        propertyCount++;
                        AppendMediaKeys(property, descendant, usedMediaKeys);
                    }
                }
            }
        }

        _logger.LogInformation("DeepScanProvider: Scanned {ContentCount} content items with {PropertyCount} properties, found {MediaCount} unique media keys",
            contentCount, propertyCount, usedMediaKeys.Count);

        return usedMediaKeys;

    }

    /// <summary>
    /// Scans a single <see cref="IPublishedProperty"/> for media UDIs and media keys, and appends any found media keys to the provided HashSet.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="owner">The owner content.</param>
    /// <param name="keys">The HashSet to append found media keys to.</param>
    public virtual void AppendMediaKeys(IPublishedProperty property, IPublishedContent owner, HashSet<Guid> keys) {

        // Get the source value of the property. We want the source value because it may contain
        // the raw UDI or GUID, whereas the processed value might have been converted to something
        // else (e.g. a URL).
        string? value = property.GetSourceValue()?.ToString();
        if (string.IsNullOrWhiteSpace(value)) return;

        // TODO: do something else for block lists

        foreach (Match match in _mediaUdiRegex.Matches(value)) {
            if (Guid.TryParse(match.Groups[2].Value, out Guid mediaKey)) keys.Add(mediaKey);
        }

        foreach (Match match in _mediaKeyRegex.Matches(value)) {
            if (Guid.TryParse(match.Groups[1].Value, out Guid mediaKey)) keys.Add(mediaKey);
        }

    }

    public bool IsMediaUsed(Guid key) {
        return GetUsedMediaKeys().Contains(key);
    }

    public bool IsMediaUsed(Udi udi) {
        return udi is GuidUdi guidUdi && IsMediaUsed(guidUdi.Guid);
    }

    /// <summary>
    /// Returns all identified used media keys.
    /// </summary>
    public HashSet<Guid> GetUsedMediaKeys() {
        return _usedMediaKeys ??= ScanForUsedMediaKeys();
    }

    public HashSet<string> GetUsedMediaUdis() {
        return _usedMediaUdis ??= GetUsedMediaKeys().ToHashSet(x => $"umb://media/{x:N}");
    }

}