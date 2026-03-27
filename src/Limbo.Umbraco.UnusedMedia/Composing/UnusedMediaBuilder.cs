using Limbo.Umbraco.UnusedMedia.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;

namespace Limbo.Umbraco.UnusedMedia.Composing;

/// <summary>
/// Provides a builder for configuring unused media detection in Umbraco.
/// </summary>
public class UnusedMediaBuilder {

    #region Properties

    /// <summary>
    /// Gets the underlying <see cref="IUmbracoBuilder"/> instance.
    /// </summary>
    public IUmbracoBuilder UmbracoBuilder { get; }

    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> for registering services.
    /// </summary>
    public IServiceCollection Services => UmbracoBuilder.Services;

    /// <summary>
    /// Gets the <see cref="IConfiguration"/> for accessing configuration settings.
    /// </summary>
    public IConfiguration Config => UmbracoBuilder.Config;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="UnusedMediaBuilder"/> class.
    /// </summary>
    /// <param name="umbracoBuilder">The Umbraco builder to use for configuration.</param>
    public UnusedMediaBuilder(IUmbracoBuilder umbracoBuilder) {
        UmbracoBuilder = umbracoBuilder;
    }

    #endregion

    #region Member methods

    /// <summary>
    /// Adds a <see cref="UsedMediaProvider"/> implementation to the collection of providers.
    /// </summary>
    /// <typeparam name="TProvider">The type of provider to add.</typeparam>
    /// <returns>The current <see cref="UnusedMediaBuilder"/> instance.</returns>
    public UnusedMediaBuilder AddProvider<TProvider>() where TProvider : UsedMediaProvider {
        UmbracoBuilder
            .WithCollectionBuilder<UsedMediaProviderCollectionBuilder>()
            .Add<TProvider>();
        return this;
    }

    /// <summary>
    /// Replaces an existing <see cref="UsedMediaProvider"/> implementation with a new one.
    /// </summary>
    /// <typeparam name="TReplace">The type of provider to remove.</typeparam>
    /// <typeparam name="TProvider">The type of provider to add.</typeparam>
    /// <returns>The current <see cref="UnusedMediaBuilder"/> instance.</returns>
    public UnusedMediaBuilder ReplaceProvider<TReplace, TProvider>() where TReplace : UsedMediaProvider where TProvider : UsedMediaProvider {
        UmbracoBuilder
            .WithCollectionBuilder<UsedMediaProviderCollectionBuilder>()
            .Exclude<TReplace>()
            .Add<TProvider>();
        return this;
    }

    #endregion

}