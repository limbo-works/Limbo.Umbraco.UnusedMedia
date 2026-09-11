using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;

namespace Limbo.Umbraco.UnusedMedia;

public static class Workarounds {

    /// <see href="https://chatgpt.com/c/6aa40989-7e68-83eb-9a82-f2ba6fa7b8e1" />
    public static IReadOnlyList<int> GetPath(IPublishedContent content) {

        IMediaNavigationQueryService documentNavigationQueryService = StaticServiceProvider.Instance.GetRequiredService<IMediaNavigationQueryService>();
        IIdKeyMap idKeyMap = StaticServiceProvider.Instance.GetRequiredService<IIdKeyMap>();

        List<int> path = [];

        if (documentNavigationQueryService.TryGetAncestorsOrSelfKeys(content.Key, out var ancestorsOrSelfKeys)) {
            path.Add(-1);
            foreach (Guid ancestorsOrSelfKey in ancestorsOrSelfKeys.Reverse()) {
                Attempt<int> idAttempt = idKeyMap.GetIdForKey(ancestorsOrSelfKey, UmbracoObjectTypes.Media);
                if (idAttempt.Success) {
                    path.Add(idAttempt.Result);
                }
            }
        }

        return path;

    }

}