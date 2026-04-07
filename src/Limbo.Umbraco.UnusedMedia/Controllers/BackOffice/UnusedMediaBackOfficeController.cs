using System.Diagnostics.CodeAnalysis;
using System.Text;
using Limbo.Umbraco.UnusedMedia.Helpers;
using Limbo.Umbraco.UnusedMedia.Models.Reports;
using Limbo.Umbraco.UnusedMedia.Providers;
using Limbo.Umbraco.UnusedMedia.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Skybrud.Essentials.AspNetCore.Json.Newtonsoft;
using Skybrud.Essentials.Json.Newtonsoft.Extensions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.BackOffice.Controllers;

namespace Limbo.Umbraco.UnusedMedia.Controllers.BackOffice;

[ApiController]
[Route("umbraco/backoffice/api/UnusedMediaBackOffice/[action]")]
public class UnusedMediaBackOfficeController : UmbracoAuthorizedApiController {

    private readonly UnusedMediaService _unusedMediaService;
    private readonly UnusedMediaBackOfficeHelper _unusedMediaBackOfficeHelper;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    public UnusedMediaBackOfficeController(UnusedMediaService unusedMediaService, UnusedMediaBackOfficeHelper unusedMediaBackOfficeHelper, IBackOfficeSecurityAccessor backOfficeSecurityAccessor) {
        _unusedMediaService = unusedMediaService;
        _unusedMediaBackOfficeHelper = unusedMediaBackOfficeHelper;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    [AllowAnonymous]
    [HttpGet("/umbraco/backoffice/api/UnusedMediaBackOffice/importmap.js")]
    public object GetImportMap() {

        string cacheBuster = _unusedMediaBackOfficeHelper.GetCacheBuster();

        Dictionary<string, string> imports = new() {
            ["@limbo/unused-media/lit"] = $"/App_Plugins/{UnusedMediaPackage.Alias}/Scripts/External/lit-all.min.js?v={cacheBuster}",
            ["@limbo/unused-media/events"] = $"/App_Plugins/{UnusedMediaPackage.Alias}/Scripts/Events/Index.js?v={cacheBuster}",
            ["@limbo/unused-media/service"] = $"/App_Plugins/{UnusedMediaPackage.Alias}/Scripts/UnusedMediaService.js?v={cacheBuster}",
            ["@limbo/unused-media/dashboard"] = $"/App_Plugins/{UnusedMediaPackage.Alias}/Scripts/DashboardElement.js?v={cacheBuster}"
        };

        JObject json = new() { { "imports", JObject.FromObject(imports) } };

        StringBuilder sb = new();
        sb.AppendLine("const map = document.createElement(\"script\");");
        sb.AppendLine("map.type = \"importmap\";");
        sb.AppendLine($"map.textContent = JSON.stringify({json});");
        sb.AppendLine("document.head.appendChild(map);");

        sb.AppendLine($"import(\"/App_Plugins/{UnusedMediaPackage.Alias}/Scripts/DashboardElement.js?v={cacheBuster}\");");

        return new ContentResult { ContentType = "text/javascript", Content = sb.ToString() };

    }

    [HttpGet]
    public object GetFilters() {
        // Ensure that we have a current user - if not, return unauthorized
        if (_backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser is not { } user) return Unauthorized();
        return _unusedMediaBackOfficeHelper.CreateFilters(Request, user);
    }

    [HttpGet]
    public object GetSites() {
        return _unusedMediaBackOfficeHelper.GetSites();
    }

    [HttpGet]
    public object GetUnusedMedia() {

        // Ensure that we have a current user - if not, return unauthorized
        if (_backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser is not { } user) return Unauthorized();

        // Create the options based on the request query and the current user
        UnusedMediaOptions options = _unusedMediaBackOfficeHelper.CreateOptions(Request, user);

        // Build a new report based on the options
        return _unusedMediaBackOfficeHelper.GetUnusedMedia(options);

    }

    [HttpPost]
    public object StartScan(string? provider = null) {

        if (string.IsNullOrWhiteSpace(provider)) return BadRequest("No provider specified.");

        UsedMediaProvider? p = _unusedMediaService.GetUsedMediaProvider(provider);
        if (p is null) return NotFound("Provider not found.");

        var report = _unusedMediaService.BuildUsedMediaReport(p);

        return new UsedMediaReportSummary(report);

    }

    [HttpPost]
    public object TrashMedia([FromBody] JObject body) {

        // Ensure that we have a current user - if not, return unauthorized
        if (_backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser is not { } user) return Unauthorized();

        if (body.TryGetGuid("mediaKey", out Guid mediaKey)) {

            // Get a reference to the media item - if it doesn't exist, return not found
            IMedia? media = _unusedMediaService.GetMedia(mediaKey);
            if (media == null) return NotFound();

            // And wo trash the media
            try {
                _unusedMediaService.TrashMedia(media, user);
                return Ok();
            } catch {
                // The service already logs the exception, so we just return a generic error message here
                return NewtonsoftJsonResult.InternalError($"Failed trashing media with ID '{media.Key}'.");
            }

        }

        if (body.TryGetGuidArray("mediaKeys", out Guid[]? mediaKeys)) {

            foreach (Guid key in mediaKeys) {

                // Get a reference to the media item - if it doesn't exist, skip it
                IMedia? media = _unusedMediaService.GetMedia(key);
                if (media == null) continue;

                try {
                    _unusedMediaService.TrashMedia(media, user);
                } catch {
                    // The service already logs the exception, so we just return a generic error message here
                    return NewtonsoftJsonResult.InternalError($"Failed trashing media with ID '{media.Key}'.");
                }

            }

            return Ok();

        }

        return BadRequest("No media keys specified.");

    }

}

static class HelloExtensions {

    public static bool TryGetGuidArray(this JObject json, string propertyName, [NotNullWhen(true)] out Guid[]? guids) {

        guids = null;

       if (json.TryGetValue(propertyName, out JToken? token) && token is JArray array) {
           guids = array.Select(x => x.ToObject<Guid>()).ToArray();
           return true;
       }

       return false;

    }

}