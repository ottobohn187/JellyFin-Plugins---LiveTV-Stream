using System.Reflection;
using System.Text;
using System.Text.Json;
using Jellyfin.Plugin.Abc27Stream.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Abc27Stream.Controllers;

[ApiController]
[Authorize]
[Route("Plugins/Abc27Stream")]
public class Abc27Controller : ControllerBase
{
    private const string LogoResourcePath = "Jellyfin.Plugin.Abc27Stream.Assets.abc27-logo.jpg";
    private static readonly HttpClient HttpClient = new();
    private static PluginConfiguration Config => Plugin.Instance?.Configuration ?? new PluginConfiguration();

    private string IconUrl => $"{Request.Scheme}://{Request.Host}/Plugins/Abc27Stream/icon.jpg";

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        string? resolvedStreamUrl = null;

        try
        {
            resolvedStreamUrl = await ResolveStreamUrlAsync(cancellationToken);
        }
        catch
        {
            resolvedStreamUrl = null;
        }

        return Ok(new
        {
            plugin = "ABC27 Stream",
            channelName = Config.ChannelName,
            channelNumber = Config.ChannelNumber,
            tvGuideId = Config.TvGuideId,
            videoApiBaseUrl = Config.VideoApiBaseUrl,
            videoId = Config.VideoId,
            metadataUrl = BuildMetadataUrl(),
            streamUrl = resolvedStreamUrl,
            iconUrl = IconUrl,
            playlistUrl = $"{Request.Scheme}://{Request.Host}/Plugins/Abc27Stream/playlist.m3u",
            redirectUrl = $"{Request.Scheme}://{Request.Host}/Plugins/Abc27Stream/stream"
        });
    }

    [HttpGet("icon.jpg")]
    [AllowAnonymous]
    public IActionResult GetIcon()
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LogoResourcePath);
        if (stream is null)
        {
            return NotFound();
        }

        return File(stream, "image/jpeg");
    }

    [HttpGet("playlist.m3u")]
    [AllowAnonymous]
    public ContentResult GetPlaylist()
    {
        var playlist = new StringBuilder();
        playlist.AppendLine("#EXTM3U");
        playlist.AppendLine($"#EXTINF:-1 tvg-id=\"{Escape(Config.TvGuideId)}\" tvg-name=\"{Escape(Config.ChannelName)}\" tvg-chno=\"{Escape(Config.ChannelNumber)}\" tvg-logo=\"{Escape(IconUrl)}\",{Config.ChannelName}");
        playlist.AppendLine($"{Request.Scheme}://{Request.Host}/Plugins/Abc27Stream/stream");

        return Content(playlist.ToString(), "audio/x-mpegurl", Encoding.UTF8);
    }

    [HttpGet("stream")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStream(CancellationToken cancellationToken)
    {
        string streamUrl;

        try
        {
            streamUrl = await ResolveStreamUrlAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return StatusCode(502, $"Unable to resolve the ABC27 live stream URL. {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(streamUrl))
        {
            return StatusCode(502, "ABC27 stream resolution returned an empty URL.");
        }

        if (Config.RedirectDirectlyToStream)
        {
            return Redirect(streamUrl);
        }

        return Content(streamUrl, "text/plain", Encoding.UTF8);
    }

    private static string BuildMetadataUrl()
    {
        return $"{Config.VideoApiBaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(Config.VideoId)}?anvack={Uri.EscapeDataString(Config.Anvack)}";
    }

    private static async Task<string> ResolveStreamUrlAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(Config.StreamUrlOverride))
        {
            return Config.StreamUrlOverride;
        }

        if (string.IsNullOrWhiteSpace(Config.VideoApiBaseUrl) ||
            string.IsNullOrWhiteSpace(Config.VideoId) ||
            string.IsNullOrWhiteSpace(Config.Anvack))
        {
            throw new InvalidOperationException("ABC27 resolver settings are incomplete.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildMetadataUrl());
        request.Headers.Referrer = new Uri("https://www.abc27.com/watch-live/");
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0");

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var json = ExtractJson(payload);
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("published_urls", out var publishedUrls))
        {
            throw new InvalidOperationException("ABC27 metadata did not include published URLs.");
        }

        string? fallbackUrl = null;

        foreach (var publishedUrl in publishedUrls.EnumerateArray())
        {
            if (publishedUrl.TryGetProperty("embed_url", out var embedUrlElement))
            {
                var embedUrl = embedUrlElement.GetString();
                if (!string.IsNullOrWhiteSpace(embedUrl))
                {
                    fallbackUrl ??= embedUrl;

                    if (publishedUrl.TryGetProperty("format", out var formatElement) &&
                        string.Equals(formatElement.GetString(), "m3u8-variant", StringComparison.OrdinalIgnoreCase))
                    {
                        return embedUrl;
                    }
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackUrl))
        {
            return fallbackUrl;
        }

        throw new InvalidOperationException("ABC27 metadata did not include an embed URL.");
    }

    private static string ExtractJson(string payload)
    {
        var start = payload.IndexOf('(');
        var end = payload.LastIndexOf(')');
        if (start >= 0 && end > start)
        {
            return payload[(start + 1)..end];
        }

        return payload;
    }

    private static string Escape(string value) => value.Replace("\"", string.Empty);
}
