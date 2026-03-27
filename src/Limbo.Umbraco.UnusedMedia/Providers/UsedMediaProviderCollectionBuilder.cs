using Umbraco.Cms.Core.Composing;

namespace Limbo.Umbraco.UnusedMedia.Providers;

public sealed class UsedMediaProviderCollectionBuilder : LazyCollectionBuilderBase<UsedMediaProviderCollectionBuilder, UsedMediaProviderCollection, UsedMediaProvider> {

    protected override UsedMediaProviderCollectionBuilder This => this;

}