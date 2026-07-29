// [CHANGE: Umbraco 17 upgrade - UmbracoAuthorizedApiController replaced by ManagementApiControllerBase] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Text.Json.Serialization;
using Limbo.Umbraco.UnusedMedia.Helpers;
using Limbo.Umbraco.UnusedMedia.Models;
using Limbo.Umbraco.UnusedMedia.Models.Filters;
using Limbo.Umbraco.UnusedMedia.Models.Reports;
using Limbo.Umbraco.UnusedMedia.Models.Sites;
using Limbo.Umbraco.UnusedMedia.Providers;
using Limbo.Umbraco.UnusedMedia.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Authorization;

namespace Limbo.Umbraco.UnusedMedia.Controllers.BackOffice;

/// <summary>
/// Management API controller backing the unused media dashboard.
///
/// Umbraco 14 removed <c>UmbracoAuthorizedApiController</c> and the <c>/umbraco/backoffice/api/</c> routes. The
/// endpoints of this controller are now served from <c>/umbraco/management/api/v1/unused-media/</c> and are
/// authorized through the standard backoffice policies.
/// </summary>
[ApiExplorerSettings(GroupName = "Unused Media")]
[VersionedApiBackOfficeRoute("unused-media")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
public class UnusedMediaBackOfficeController : ManagementApiControllerBase {

    private readonly UnusedMediaService _unusedMediaService;
    private readonly UnusedMediaBackOfficeHelper _unusedMediaBackOfficeHelper;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    public UnusedMediaBackOfficeController(UnusedMediaService unusedMediaService, UnusedMediaBackOfficeHelper unusedMediaBackOfficeHelper, IBackOfficeSecurityAccessor backOfficeSecurityAccessor) {
        _unusedMediaService = unusedMediaService;
        _unusedMediaBackOfficeHelper = unusedMediaBackOfficeHelper;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <summary>
    /// Returns whether the current user is allowed to use the dashboard, along with a few settings the dashboard
    /// needs up front.
    ///
    /// The <c>Dashboard:AllowedGroups</c> setting used to be enforced through the <c>IDashboard.AccessRules</c> of
    /// the removed server side dashboard. It is now enforced on every endpoint of this controller, and the dashboard
    /// element calls this endpoint to decide whether to render itself.
    /// </summary>
    [HttpGet("config")]
    [ProducesResponseType<UnusedMediaConfigResponse>(StatusCodes.Status200OK)]
    public IActionResult GetConfig() {
        IUser? user = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        return Ok(new UnusedMediaConfigResponse {
            Allowed = user is not null && _unusedMediaBackOfficeHelper.IsAllowed(user),
            Version = UnusedMediaPackage.InformationalVersion,
            PerPage = _unusedMediaBackOfficeHelper.Settings.Dashboard.PerPage
        });
    }

    [HttpGet("filters")]
    [ProducesResponseType<IEnumerable<UnusedMediaFilter>>(StatusCodes.Status200OK)]
    public IActionResult GetFilters() {
        if (!TryGetAllowedUser(out IUser? user, out IActionResult? error)) return error;
        return Ok(_unusedMediaBackOfficeHelper.CreateFilters(Request, user));
    }

    [HttpGet("sites")]
    [ProducesResponseType<IEnumerable<UnusedSiteItem>>(StatusCodes.Status200OK)]
    public IActionResult GetSites() {
        if (!TryGetAllowedUser(out _, out IActionResult? error)) return error;
        return Ok(_unusedMediaBackOfficeHelper.GetSites());
    }

    [HttpGet]
    [ProducesResponseType<UnusedMediaResult>(StatusCodes.Status200OK)]
    public IActionResult GetUnusedMedia() {

        if (!TryGetAllowedUser(out IUser? user, out IActionResult? error)) return error;

        // Create the options based on the request query and the current user
        UnusedMediaOptions options = _unusedMediaBackOfficeHelper.CreateOptions(Request, user);

        // Build a new report based on the options
        return Ok(_unusedMediaBackOfficeHelper.GetUnusedMedia(options));

    }

    [HttpPost("scan")]
    [ProducesResponseType<UsedMediaReportSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult StartScan(string? provider = null) {

        if (!TryGetAllowedUser(out _, out IActionResult? error)) return error;

        if (string.IsNullOrWhiteSpace(provider)) return BadRequest("No provider specified.");

        UsedMediaProvider? p = _unusedMediaService.GetUsedMediaProvider(provider);
        if (p is null) return NotFound("Provider not found.");

        IUsedMediaReport report = _unusedMediaService.BuildUsedMediaReport(p);

        return Ok(new UsedMediaReportSummary(report));

    }

    [HttpPost("trash")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult TrashMedia([FromBody] TrashMediaRequest body) {

        if (!TryGetAllowedUser(out IUser? user, out IActionResult? error)) return error;

        if (body.MediaKey is { } mediaKey) {

            // Get a reference to the media item - if it doesn't exist, return not found
            IMedia? media = _unusedMediaService.GetMedia(mediaKey);
            if (media == null) return NotFound();

            // And now trash the media
            try {
                _unusedMediaService.TrashMedia(media, user);
                return Ok();
            } catch {
                // The service already logs the exception, so we just return a generic error message here
                return Problem($"Failed trashing media with ID '{media.Key}'.", statusCode: StatusCodes.Status500InternalServerError);
            }

        }

        if (body.MediaKeys is { Length: > 0 } mediaKeys) {

            foreach (Guid key in mediaKeys) {

                // Get a reference to the media item - if it doesn't exist, skip it
                IMedia? media = _unusedMediaService.GetMedia(key);
                if (media == null) continue;

                try {
                    _unusedMediaService.TrashMedia(media, user);
                } catch {
                    // The service already logs the exception, so we just return a generic error message here
                    return Problem($"Failed trashing media with ID '{media.Key}'.", statusCode: StatusCodes.Status500InternalServerError);
                }

            }

            return Ok();

        }

        return BadRequest("No media keys specified.");

    }

    /// <summary>
    /// Resolves the current backoffice user and verifies that they're allowed to use the dashboard.
    /// </summary>
    private bool TryGetAllowedUser(out IUser user, out IActionResult error) {

        user = null!;
        error = null!;

        if (_backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser is not { } currentUser) {
            error = Unauthorized();
            return false;
        }

        if (!_unusedMediaBackOfficeHelper.IsAllowed(currentUser)) {
            // [CHANGE: code review fix - "Forbid()" delegates to the default forbid authentication scheme, which
            // isn't guaranteed to be configured in an Umbraco site (multiple schemes are registered), in which case
            // it throws instead of returning a 403. Return the status code explicitly.] Related: wwwroot/Scripts/dashboard.js
            error = StatusCode(StatusCodes.Status403Forbidden);
            return false;
        }

        user = currentUser;
        return true;

    }

}

/// <summary>
/// Request model for the <c>trash</c> endpoint. Either <see cref="MediaKey"/> or <see cref="MediaKeys"/> should be
/// specified.
/// </summary>
public class TrashMediaRequest {

    [JsonPropertyName("mediaKey")]
    public Guid? MediaKey { get; set; }

    [JsonPropertyName("mediaKeys")]
    public Guid[]? MediaKeys { get; set; }

}

/// <summary>
/// Response model for the <c>config</c> endpoint.
/// </summary>
public class UnusedMediaConfigResponse {

    [JsonPropertyName("allowed")]
    public required bool Allowed { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("perPage")]
    public required int PerPage { get; init; }

}
