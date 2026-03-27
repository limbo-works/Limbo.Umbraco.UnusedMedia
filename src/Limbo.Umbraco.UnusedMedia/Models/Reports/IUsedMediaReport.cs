using Skybrud.Essentials.Time;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Limbo.Umbraco.UnusedMedia.Models.Reports;

/// <summary>
/// Interface representing a report over media that is in use.
/// </summary>
public interface IUsedMediaReport {

    /// <summary>
    /// Gets the alias of the report, which is typically the type name of the provider that generated it.
    /// </summary>
    string Alias { get; }

    /// <summary>
    /// Gets the name of the report, which is typically the name of the provider that generated it.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a timestamp for when the report was generated.
    /// </summary>
    EssentialsTime CreateDate { get; }

    /// <summary>
    /// Returns a set of the keys of the media that are in use, according to the report.
    /// </summary>
    ISet<Guid> Keys { get; }

    /// <summary>
    /// Returns whether the specified <paramref name="media"/> is currently in use, according to the report.
    /// </summary>
    /// <param name="media">The media.</param>
    /// <returns><c>true</c> if <paramref name="media"/> is in use; otherwise <c>false</c>.</returns>
    public bool IsInUse(IPublishedContent media) {
        return Keys.Contains(media.Key);
    }

}