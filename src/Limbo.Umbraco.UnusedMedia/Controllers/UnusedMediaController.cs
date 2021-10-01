using Limbo.Umbraco.UnusedMedia.Helpers;
using Limbo.Umbraco.UnusedMedia.Models;
using Limbo.Umbraco.UnusedMedia.Services;
using Skybrud.WebApi.Json;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

namespace Limbo.Umbraco.UnusedMedia.Controllers {
    
    [JsonOnlyConfiguration]
    [PluginController("Limbo")]
    public class UnusedMediaController : UmbracoAuthorizedApiController {
        
        private readonly UnusedMediaService _unusedMediaService;
        private readonly UnusedMediaBackOfficeHelper _backOfficeHelper;

        public UnusedMediaController(UnusedMediaService unusedMediaService, UnusedMediaBackOfficeHelper backOfficeHelper) {
            _unusedMediaService = unusedMediaService;
            _backOfficeHelper = backOfficeHelper;
        }

        public object GetFilters() {
            return _backOfficeHelper.GetFilters(UmbracoContext.HttpContext, Security.CurrentUser);
        }

        public object GetItems() {

            UnusedMediaOptions options = _backOfficeHelper.GetOptions(UmbracoContext.HttpContext, Security.CurrentUser);
            
            UnusedMediaResult result = _unusedMediaService.GetUnusedMedia(options);

            return result;

        }

    }

}