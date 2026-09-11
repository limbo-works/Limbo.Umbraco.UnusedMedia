// [CHANGE: Umbraco 17 upgrade - System.Text.Json + explicit UmbracoContext] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Limbo.Umbraco.UnusedMedia.BlockList;
using Limbo.Umbraco.UnusedMedia.Json;
using Limbo.Umbraco.UnusedMedia.Models.BlockList;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Skybrud.Essentials.Collections.Enumerables.Extensions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Providers;

public class ContentCacheUsedMediaProvider : UsedMediaProvider {

    private static readonly Regex _mediaUdiRegex = new(@"(umb:\/\/media\/([0-9a-fA-F]{32}))", RegexOptions.Compiled);
    private static readonly Regex _mediaKeyRegex = new("([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})", RegexOptions.Compiled);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly ILogger<ContentCacheUsedMediaProvider> _logger;
    private readonly UnusedMediaBlockListParser _blockListParser;

    private HashSet<Guid>? _usedMediaKeys;
    private HashSet<string>? _usedMediaUdis;

    /// <summary>
    /// Initializes a new provider.
    ///
    /// Unlike the Umbraco 13 backoffice, Management API requests don't come with an ambient
    /// <see cref="IUmbracoContext"/>, so one has to be ensured explicitly before querying the published caches.
    /// </summary>
    public ContentCacheUsedMediaProvider(IServiceScopeFactory serviceScopeFactory, IUmbracoContextFactory umbracoContextFactory, ILogger<ContentCacheUsedMediaProvider> logger) {
        _serviceScopeFactory = serviceScopeFactory;
        _umbracoContextFactory = umbracoContextFactory;
        _logger = logger;
        _blockListParser = StaticServiceProvider.Instance.GetRequiredService<UnusedMediaBlockListParser>();
    }

    /// <summary>
    /// Initializes a new provider, resolving <see cref="IUmbracoContextFactory"/> from the static service provider.
    /// </summary>
    public ContentCacheUsedMediaProvider(IServiceScopeFactory serviceScopeFactory, ILogger<ContentCacheUsedMediaProvider> logger)
        : this(serviceScopeFactory, StaticServiceProvider.Instance.GetRequiredService<IUmbracoContextFactory>(), logger) { }

    /// <summary>
    /// Scans the entire site for media UDIs and GUID keys used within content properties.
    /// This method is intended to be called once to populate the internal cache.
    /// </summary>
    /// <returns>A HashSet of media keys found in content properties.</returns>
    public override HashSet<Guid> ScanForUsedMediaKeys() {

        _logger.LogInformation("ContentCacheUsedMediaProvider: Starting scan for used media UDIs in content");

        HashSet<Guid> usedMediaKeys = [];
        int contentCount = 0;
        int propertyCount = 0;

        using (UmbracoContextReference contextReference = _umbracoContextFactory.EnsureUmbracoContext())
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

        _logger.LogInformation("ContentCacheUsedMediaProvider: Scanned {ContentCount} content items with {PropertyCount} properties, found {MediaCount} unique media keys",
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

        if (property.PropertyType.EditorAlias is "Umbraco.BlockList" or "Limbo.Umbraco.BlockList") {
            if (JsonNodeExtensions.TryParseJsonObject(value, out JsonObject? json)) {
                try {
                    UnusedMediaBlockListModel blockList = _blockListParser.ParseBlockList(json);
                    AppendMediaKeys(blockList, property, owner, keys);
                } catch (Exception ex) {
                    // TODO: add as warning to the report instead of logging an error
                    _logger.LogError(ex, "Failed parsing block list model for property {PropertyAlias} on page with ID {PageId}.", property.Alias, owner.Id);
                }
            }
        }

        foreach (Match match in _mediaUdiRegex.Matches(value)) {
            if (Guid.TryParse(match.Groups[2].Value, out Guid mediaKey)) keys.Add(mediaKey);
        }

        foreach (Match match in _mediaKeyRegex.Matches(value)) {
            if (Guid.TryParse(match.Groups[1].Value, out Guid mediaKey)) keys.Add(mediaKey);
        }

    }

    public virtual void AppendMediaKeys(UnusedMediaBlockListModel blockList, IPublishedProperty property, IPublishedContent owner, HashSet<Guid> keys) {
        foreach (var item in blockList.Blocks) {
            AppendMediaKeys(item, property, owner, keys);
        }
    }

    public virtual void AppendMediaKeys(UnusedMediaBlockListItem item, IPublishedProperty property, IPublishedContent owner, HashSet<Guid> keys) {
        // we don't do anything by default
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
