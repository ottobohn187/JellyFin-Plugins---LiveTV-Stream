using System.Reflection;
using System.Text;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Jellyfin.Plugin.DefaultDateAdded.Services;
public sealed class WebClientInstaller(IApplicationPaths paths, ILogger<WebClientInstaller> logger) : IHostedService
{
    const string Tag = "<script defer=\"defer\" src=\"default-date-added.js?v=1.0.1\" data-default-date-added></script>";
    public async Task StartAsync(CancellationToken token)
    {
        try {
            // Let other web-client plugins finish their index update first.
            await Task.Delay(TimeSpan.FromSeconds(2), token);
            var indexPath = Path.Combine(paths.WebPath, "index.html");
            await using var source = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(Plugin).Namespace + ".Web.default-date-added.js") ?? throw new InvalidOperationException("Embedded script missing.");
            await using (var output = File.Create(Path.Combine(paths.WebPath, "default-date-added.js"))) await source.CopyToAsync(output, token);
            var html = await File.ReadAllTextAsync(indexPath, token);
            html = System.Text.RegularExpressions.Regex.Replace(html, "<script[^>]+data-default-date-added[^>]*></script>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            await File.WriteAllTextAsync(indexPath, html.Replace("</body>", Tag + "</body>"), new UTF8Encoding(false), token);
            logger.LogInformation("Default Date Added web client installed");
        } catch (Exception ex) { logger.LogWarning(ex, "Default Date Added web client installation failed"); }
    }
    public Task StopAsync(CancellationToken token) => Task.CompletedTask;
}
