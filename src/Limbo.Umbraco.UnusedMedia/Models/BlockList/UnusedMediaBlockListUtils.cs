using Limbo.Umbraco.UnusedMedia.BlockList;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Umbraco.Cms.Core.DependencyInjection;

namespace Limbo.Umbraco.UnusedMedia.Models.BlockList;

[Obsolete("Use UnusedMediaBlockListParser directly instead.")]
public class UnusedMediaBlockListUtils {

    public static UnusedMediaBlockListModel ParseBlockList(JObject json) {
        return StaticServiceProvider.Instance.GetRequiredService<UnusedMediaBlockListParser>().ParseBlockList(json);
    }

    public static UnusedMediaBlockListLayout ParseBlockListLayout(JObject json) {
        return StaticServiceProvider.Instance.GetRequiredService<UnusedMediaBlockListParser>().ParseBlockListLayout(json);
    }

    public static UnusedMediaBlockListLayoutItem ParseBlockListLayoutItem(JObject json) {
        return StaticServiceProvider.Instance.GetRequiredService<UnusedMediaBlockListParser>().ParseBlockListLayoutItem(json);
    }

    public static UnusedMediaBlockListContentData ParseBlockListContentData(JObject json) {
        return StaticServiceProvider.Instance.GetRequiredService<UnusedMediaBlockListParser>().ParseBlockListContentData(json);
    }

}