using Umbraco.Cms.Core.Composing;

namespace Limbo.Umbraco.UnusedMedia.Providers;

internal sealed class UsedMediaProviderCollectionBuilder : LazyCollectionBuilderBase<UsedMediaProviderCollectionBuilder, UsedMediaProviderCollection, UsedMediaProvider> {

    protected override UsedMediaProviderCollectionBuilder This => this;

}