using System.Reflection;
using System.Text;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ReleaseWatcher.Services;

public sealed class DashboardWidgetInstaller : IHostedService
{
    private const string ScriptFileName = "release-watcher-dashboard.js";
    private const string ScriptTag = "<script defer=\"defer\" src=\"release-watcher-dashboard.js?v=1.2.0\" data-release-watcher-dashboard></script>";
    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<DashboardWidgetInstaller> _logger;

    public DashboardWidgetInstaller(
        IApplicationPaths applicationPaths,
        ILogger<DashboardWidgetInstaller> logger)
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
                _logger.LogWarning("Release Watcher could not find Jellyfin Web at {WebPath}", webPath);
                return;
            }

            var resourceName = typeof(Plugin).Namespace + ".Web.release-watcher-dashboard.js";
            await using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (resource is null)
            {
                _logger.LogWarning("Release Watcher dashboard resource {ResourceName} was not found", resourceName);
                return;
            }

            await using (var output = File.Create(Path.Combine(webPath, ScriptFileName)))
            {
                await resource.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            }

            var index = await File.ReadAllTextAsync(indexPath, cancellationToken).ConfigureAwait(false);
            if (!index.Contains("data-release-watcher-dashboard", StringComparison.Ordinal))
            {
                var backupPath = indexPath + ".release-watcher-backup";
                if (!File.Exists(backupPath))
                {
                    File.Copy(indexPath, backupPath);
                }

                index = index.Replace("</body>", ScriptTag + "</body>", StringComparison.Ordinal);
                await File.WriteAllTextAsync(indexPath, index, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Release Watcher dashboard widget installed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Release Watcher could not install its dashboard widget");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
