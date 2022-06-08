using Limbo.Umbraco.UnusedMedia.Helpers;
using Limbo.Umbraco.UnusedMedia.Services;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Limbo.Umbraco.UnusedMedia.Composers {

    public class UnusedMediaComposer : IComposer {

        public void Compose(IUmbracoBuilder builder) {
            builder.Services.AddTransient<UnusedMediaService>();
            builder.Services.AddTransient<UnusedMediaBackOfficeHelper>();
        }
    }

}