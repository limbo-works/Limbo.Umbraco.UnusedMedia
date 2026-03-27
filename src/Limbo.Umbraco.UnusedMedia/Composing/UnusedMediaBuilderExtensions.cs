using Umbraco.Cms.Core.DependencyInjection;

namespace Limbo.Umbraco.UnusedMedia.Composing;

/// <summary>
/// Provides extension methods for <see cref="IUmbracoBuilder"/> to support unused media functionality.
/// </summary>
public static class UnusedMediaBuilderExtensions {

    /// <summary>
    /// Returns an instance of <see cref="UnusedMediaBuilder"/> that can be used for configuration of unused media providers and related services.
    /// </summary>
    /// <param name="umbracoBuilder">The <see cref="IUmbracoBuilder"/> instance to extend.</param>
    /// <returns>An instance of <see cref="UnusedMediaBuilder"/>.</returns>
    public static UnusedMediaBuilder UnusedMedia(this IUmbracoBuilder umbracoBuilder) {
        return (UnusedMediaBuilder) umbracoBuilder.AppCaches.RuntimeCache.Get("UnusedMediaBuilder", () => new UnusedMediaBuilder(umbracoBuilder))!;
    }

}