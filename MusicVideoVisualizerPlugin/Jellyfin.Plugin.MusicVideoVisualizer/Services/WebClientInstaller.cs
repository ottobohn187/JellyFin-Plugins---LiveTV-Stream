using System.Reflection;
using System.Text;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MusicVideoVisualizer.Services;

public sealed class WebClientInstaller : IHostedService
{
    private const string ScriptFileName = "music-video-visualizer.js";
    private const string ScriptTag = "<script defer=\"defer\" src=\"music-video-visualizer.js?v=0.1.0\" data-music-video-visualizer></script>";
    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<WebClientInstaller> _logger;

    public WebClientInstaller(IApplicationPaths applicationPaths, ILogger<WebClientInstaller> logger)
    {
        _applicationPaths = applicationPaths;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var webPath = _applicationPaths.WebPath;
            var indexPath = Path.Combine(webPath, "index.html");
            if (string.IsNullOrWhiteSpace(webPath) || !File.Exists(indexPath))
            {
                _logger.LogWarning("Music Video Visualizer could not find Jellyfin Web at {WebPath}", webPath);
                return;
            }

            var resourceName = typeof(Plugin).Namespace + ".Web.music-video-visualizer.js";
            await using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (resource is null)
            {
                throw new InvalidOperationException("Embedded visualizer script was not found.");
            }

            await using (var output = File.Create(Path.Combine(webPath, ScriptFileName)))
            {
                await resource.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            }

            var index = await File.ReadAllTextAsync(indexPath, cancellationToken).ConfigureAwait(false);
            if (!index.Contains("data-music-video-visualizer", StringComparison.Ordinal))
            {
                index = index.Replace("</body>", ScriptTag + "</body>", StringComparison.Ordinal);
            }
            else
            {
                index = System.Text.RegularExpressions.Regex.Replace(
                    index,
                    "<script[^>]+data-music-video-visualizer[^>]*></script>",
                    ScriptTag,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            await File.WriteAllTextAsync(indexPath, index, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Music Video Visualizer web client installed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Music Video Visualizer could not install its web client");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
