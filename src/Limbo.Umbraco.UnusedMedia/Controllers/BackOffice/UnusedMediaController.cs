using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Limbo.Umbraco.UnusedMedia.Helpers;
using Limbo.Umbraco.UnusedMedia.Models;
using Limbo.Umbraco.UnusedMedia.Models.References;
using Limbo.Umbraco.UnusedMedia.Models.Used;
using Limbo.Umbraco.UnusedMedia.Services;
using Skybrud.WebApi.Json;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Membership;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

namespace Limbo.Umbraco.UnusedMedia.Controllers.BackOffice {
    
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
            return Request.CreateResponse(HttpStatusCode.OK);

        }

        [HttpGet]
        [AllowAnonymous]
        public object Rebuild() {

            // Get the options via the helper (method can be overriden)
            UnusedMediaOptions options = _backOfficeHelper.GetOptions(UmbracoContext.HttpContext, Security.CurrentUser);

            _unusedMediaService.BuildReportFromContentCache();
            
            if (options.IncludeMembers) _unusedMediaService.BuildReportFromMemberCache();
            
            return new { success = true };

        }

        [HttpGet]
        [AllowAnonymous]
        public object RebuildMembers() {
            MemberCacheUsedMediaReport report = _unusedMediaService.BuildReportFromMemberCache();
            return new {
                success = true,
                start = report.Start,
                completed = report.Completed,
                duration = report.Duration.TotalMilliseconds
            };
        }
        
        [HttpGet]
        public object GetReferencesById(int id, string type) {

            // Get a reference to the current backoffice user
            IUser user = Security.CurrentUser;
            
            switch (type) {

                case "media":

                    IMedia media = Services.MediaService.GetById(id);
                    if (media == null) return Request.CreateResponse(HttpStatusCode.NotFound, "Media not found.");

                    ReferenceResult result = _unusedMediaService.GetReferencesByChild(media, user);

                    if (result.AllowDelete == false && string.IsNullOrWhiteSpace(result.NotAllowedMessage)) {
                        result.NotAllowedMessage = "Du har ikke rettigheder til at slette det valgte medie.";
                    }

                    return result;

                default:
                    return Request.CreateResponse(HttpStatusCode.BadRequest, $"Unsupported item type: {id}");
                
            }


        }

        #endregion

    }

}