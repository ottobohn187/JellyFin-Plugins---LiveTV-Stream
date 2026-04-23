using System.Text;
using Jellyfin.Plugin.Fox43Stream.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Fox43Stream.Controllers;

[ApiController]
[Authorize]
[Route("Plugins/Fox43Stream")]
public class Fox43Controller : ControllerBase
{
    private static PluginConfiguration Config => Plugin.Instance?.Configuration ?? new PluginConfiguration();

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            plugin = "FOX43 Stream",
            channelName = Config.ChannelName,
            channelNumber = Config.ChannelNumber,
            tvGuideId = Config.TvGuideId,
            streamUrl = Config.StreamUrl,
            playlistUrl = $"{Request.Scheme}://{Request.Host}/Plugins/Fox43Stream/playlist.m3u",
            redirectUrl = $"{Request.Scheme}://{Request.Host}/Plugins/Fox43Stream/stream"
        });
    }

    [HttpGet("playlist.m3u")]
    [AllowAnonymous]
    public ContentResult GetPlaylist()
    {
        var playlist = new StringBuilder();
        playlist.AppendLine("#EXTM3U");
        playlist.AppendLine($"#EXTINF:-1 tvg-id=\"{Escape(Config.TvGuideId)}\" tvg-name=\"{Escape(Config.ChannelName)}\" tvg-chno=\"{Escape(Config.ChannelNumber)}\",{Config.ChannelName}");
        playlist.AppendLine($"{Request.Scheme}://{Request.Host}/Plugins/Fox43Stream/stream");

        return Content(playlist.ToString(), "audio/x-mpegurl", Encoding.UTF8);
    }

    [HttpGet("stream")]
    [AllowAnonymous]
    public IActionResult GetStream()
    {
        if (string.IsNullOrWhiteSpace(Config.StreamUrl))
        {
            return BadRequest("Stream URL is empty.");
        }

        if (Config.RedirectDirectlyToStream)
        {
            return Redirect(Config.StreamUrl);
        }

        return Content(Config.StreamUrl, "text/plain", Encoding.UTF8);
    }

    private static string Escape(string value) => value.Replace("\"", string.Empty);
}
