using Jellyfin.Plugin.ReleaseWatcher.Services;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.ReleaseWatcher.Tasks;

public sealed class CheckForJellyfinUpdatesTask : IScheduledTask
{
    private readonly ReleaseCheckService _releaseCheckService;

    public CheckForJellyfinUpdatesTask(ReleaseCheckService releaseCheckService)
    {
        _releaseCheckService = releaseCheckService;
    }

    public string Name => "Check for Jellyfin updates";

    public string Key => "ReleaseWatcherCheckForUpdates";

    public string Description => "Checks the official Jellyfin GitHub releases and alerts administrators when a newer version is available.";

    public string Category => "Application";

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo { Type = TaskTriggerInfoType.StartupTrigger };
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromHours(24).Ticks
        };
    }

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        progress.Report(0);
        await _releaseCheckService.CheckAsync(cancellationToken).ConfigureAwait(false);
        progress.Report(100);
    }
}
