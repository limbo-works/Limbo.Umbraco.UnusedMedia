using System.Net;
using System.Net.Http;
using System.Web.Http;
using Limbo.Umbraco.UnusedMedia.Helpers;
using Limbo.Umbraco.UnusedMedia.Models;
using Limbo.Umbraco.UnusedMedia.Services;
using Skybrud.WebApi.Json;
using Umbraco.Core.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

namespace Limbo.Umbraco.UnusedMedia.Controllers {
    
    [JsonOnlyConfiguration]
    [PluginController("Limbo")]
    public class UnusedMediaController : UmbracoAuthorizedApiController {
        
        private readonly UnusedMediaService _unusedMediaService;
        private readonly UnusedMediaBackOfficeHelper _backOfficeHelper;

        #region Contructors

        public UnusedMediaController(UnusedMediaService unusedMediaService, UnusedMediaBackOfficeHelper backOfficeHelper) {
            _unusedMediaService = unusedMediaService;
            _backOfficeHelper = backOfficeHelper;
        }

        #endregion

        #region Public API methods
        
        [HttpGet]
        public object GetFilters() {
            return _backOfficeHelper.GetFilters(UmbracoContext.HttpContext, Security.CurrentUser);
        }
        
        [HttpGet]
        public object GetItems() {

            // Get the options via the helper (method can be overriden)
            UnusedMediaOptions options = _backOfficeHelper.GetOptions(UmbracoContext.HttpContext, Security.CurrentUser);
            
            // Get the result from the unused media service
            UnusedMediaResult result = _unusedMediaService.GetUnusedMedia(options);

            return result;

        }

        [HttpGet]
        public object TrashMedia(int mediaId) {

            // Get the media by it's ID
            IMedia media = Services.MediaService.GetById(mediaId);
            if (media == null) return Request.CreateResponse(HttpStatusCode.NotFound);

            // Move the media to the recycle bind (on behalf of the current user)
            Services.MediaService.MoveToRecycleBin(media, Security.CurrentUser.Id);

            // Send an OK response to the Angular dashboard
            return Request.CreateResponse(HttpStatusCode.NotFound);

        }

        #endregion

    }

}