using Limbo.Umbraco.UnusedMedia.Helpers;
using Limbo.Umbraco.UnusedMedia.Services;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Limbo.Umbraco.UnusedMedia.Composers {

    public class UnusedMediaComposer : IUserComposer {
        
        public void Compose(Composition composition) {
            composition.Register<UnusedMediaService>();
            composition.Register<UnusedMediaBackOfficeHelper>();
        }

    }

}