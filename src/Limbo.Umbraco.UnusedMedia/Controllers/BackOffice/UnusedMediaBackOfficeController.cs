using Limbo.Umbraco.UnusedMedia.Models;
using Limbo.Umbraco.UnusedMedia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Skybrud.Essentials.Json.Newtonsoft.Extensions;
using Umbraco.Cms.Web.BackOffice.Controllers;

namespace Limbo.Umbraco.UnusedMedia.Controllers.BackOffice;

[ApiController]
[Route("umbraco/backoffice/api/UnusedMediaBackOffice/[action]")]
public class UnusedMediaBackOfficeController : UmbracoAuthorizedApiController {

    private readonly ILogger<UnusedMediaBackOfficeController> _logger;
    private readonly UnusedMediaService _unusedMediaService;

    public UnusedMediaBackOfficeController(ILogger<UnusedMediaBackOfficeController> logger, UnusedMediaService unusedMediaService) {
        _logger = logger;
        _unusedMediaService = unusedMediaService;
    }

    [HttpGet]
    public object GetUnusedMedia() {
        return _unusedMediaService.GetUnusedMediaReport();
    }

    [HttpPost]
    public object ClearScan() {
        _unusedMediaService.ClearScan();
        return Ok();
    }

    [HttpPost]
    public object StartScan() {
        Guid taskId = _unusedMediaService.StartScan();
        return new { taskId };
    }

    [HttpGet]
    public IActionResult GetScanStatus(Guid taskId) {
        UnusedMediaScanStatus? status = _unusedMediaService.GetScanStatus(taskId);
        if (status == null) {
            return NotFound();
        }
        return Ok(status);
    }

    [HttpPost]
    public object DeleteMedia([FromBody] JObject body) {
        int mediaId = body.GetInt32("mediaId");
        _unusedMediaService.DeleteMedia(mediaId);
        return Ok();
    }

}
