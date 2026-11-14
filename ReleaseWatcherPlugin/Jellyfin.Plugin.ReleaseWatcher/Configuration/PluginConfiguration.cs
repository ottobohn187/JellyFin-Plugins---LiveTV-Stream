using System;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ReleaseWatcher.Configuration;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public bool IncludePrereleases { get; set; }

    public string Repository { get; set; } = "jellyfin/jellyfin";

    public string LatestVersion { get; set; } = string.Empty;

    public string LatestReleaseUrl { get; set; } = string.Empty;

    public string LatestReleaseName { get; set; } = string.Empty;

    public string LastNotifiedVersion { get; set; } = string.Empty;

    public DateTime? LastCheckedUtc { get; set; }

    public string LastError { get; set; } = string.Empty;
}
