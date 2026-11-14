using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.MusicVideoVisualizer.Controllers;

[ApiController]
[Authorize]
[Route("Plugins/MusicVideoVisualizer")]
public sealed class VisualizerController : ControllerBase
{
    [HttpGet("settings")]
    public ActionResult<object> GetSettings()
    {
        var config = Plugin.Instance?.Configuration;
        return Ok(new
        {
            Enabled = config?.Enabled ?? false,
            LibraryIds = config?.LibraryIds ?? [],
            BarCount = Math.Clamp(config?.BarCount ?? 64, 24, 128),
            HeightPercent = Math.Clamp(config?.HeightPercent ?? 22, 10, 40),
            Opacity = Math.Clamp(config?.Opacity ?? 0.9, 0.25, 1),
            Palette = config?.Palette ?? "Rainbow"
        });
    }
}
