using Umbraco.Cms.Core.Composing;

namespace Limbo.Umbraco.UnusedMedia.Providers;

public sealed class UsedMediaProviderCollection : BuilderCollectionBase<UsedMediaProvider> {

    public UsedMediaProviderCollection(Func<IEnumerable<UsedMediaProvider>> items) : base(items) { }

}