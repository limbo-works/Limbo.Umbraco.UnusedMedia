using Limbo.Umbraco.UnusedMedia.Helpers;
using Limbo.Umbraco.UnusedMedia.Models;
using Limbo.Umbraco.UnusedMedia.Models.References;
using Limbo.Umbraco.UnusedMedia.Models.Used;
using Limbo.Umbraco.UnusedMedia.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;

namespace Limbo.Umbraco.UnusedMedia.Controllers.BackOffice {

    [PluginController("Limbo")]
    public class UnusedMediaController : UmbracoAuthorizedApiController {

        private readonly UnusedMediaService _unusedMediaService;
        private readonly UnusedMediaBackOfficeHelper _backOfficeHelper;
        private readonly IMediaService _mediaService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

        #region Contructors

        public UnusedMediaController(UnusedMediaService unusedMediaService, UnusedMediaBackOfficeHelper backOfficeHelper, IMediaService mediaService, IHttpContextAccessor httpContextAccessor, IBackOfficeSecurityAccessor backOfficeSecurityAccessor) {
            _unusedMediaService = unusedMediaService;
            _backOfficeHelper = backOfficeHelper;
            _mediaService = mediaService;
            _httpContextAccessor = httpContextAccessor;
            _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        }

        #endregion

        #region Public API methods

        [HttpGet]
        public object GetFilters() {
            return _backOfficeHelper.GetFilters(_httpContextAccessor.HttpContext, _backOfficeSecurityAccessor.BackOfficeSecurity.CurrentUser);
        }

        [HttpGet]
        public object GetItems() {

            // Get the options via the helper (method can be overriden)
            UnusedMediaOptions options = _backOfficeHelper.GetOptions(_httpContextAccessor.HttpContext, _backOfficeSecurityAccessor.BackOfficeSecurity.CurrentUser);

            // Get the result from the unused media service
            UnusedMediaResult result = _unusedMediaService.GetUnusedMedia(options);

            return result;

        }

        [HttpGet]
        public object TrashMedia(int mediaId) {

            // Get the media by it's ID
            IMedia media = _mediaService.GetById(mediaId);
            if (media == null) {
                return NotFound();
            }

            // Move the media to the recycle bind (on behalf of the current user)
            _mediaService.MoveToRecycleBin(media, _backOfficeSecurityAccessor.BackOfficeSecurity.CurrentUser.Id);

            // Send an OK response to the Angular dashboard
            return Ok();

        }

        [HttpGet]
        [AllowAnonymous]
        public object Rebuild() {

            // Get the options via the helper (method can be overriden)
            UnusedMediaOptions options = _backOfficeHelper.GetOptions(_httpContextAccessor.HttpContext, _backOfficeSecurityAccessor.BackOfficeSecurity.CurrentUser);

            _unusedMediaService.BuildReportFromContentCache();

            if (options.IncludeMembers) {
                _unusedMediaService.BuildReportFromMemberCache();
            }

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
            IUser user = _backOfficeSecurityAccessor.BackOfficeSecurity.CurrentUser;

            switch (type) {

                case "media":

                    IMedia media = _mediaService.GetById(id);
                    if (media == null) {
                        return NotFound("Media not found.");
                    }

                    ReferenceResult result = _unusedMediaService.GetReferencesByChild(media, user);

                    if (result.AllowDelete == false && string.IsNullOrWhiteSpace(result.NotAllowedMessage)) {
                        result.NotAllowedMessage = "Du har ikke rettigheder til at slette det valgte medie.";
                    }

                    return result;

                default:
                    return BadRequest($"Unsupported item type: {id}");

            }


        }

        #endregion

    }

}