using Jellyfin.Plugin.ReleaseWatcher.Models;
using Jellyfin.Plugin.ReleaseWatcher.Services;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.ReleaseWatcher.Controllers;

[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("Plugins/ReleaseWatcher")]
public sealed class ReleaseWatcherController : ControllerBase
{
    private readonly ReleaseCheckService _releaseCheckService;

    public ReleaseWatcherController(ReleaseCheckService releaseCheckService)
    {
        _releaseCheckService = releaseCheckService;
    }

    [HttpGet("status")]
    public ActionResult<ReleaseStatus> GetStatus() => Ok(_releaseCheckService.GetStatus());

    [HttpPost("check")]
    public async Task<ActionResult<ReleaseStatus>> Check(CancellationToken cancellationToken)
        => Ok(await _releaseCheckService.CheckAsync(cancellationToken).ConfigureAwait(false));
}
