using System;

namespace Jellyfin.Plugin.ReleaseWatcher.Models;

public sealed record ReleaseStatus(
    string InstalledVersion,
    string LatestVersion,
    string LatestReleaseName,
    string LatestReleaseUrl,
    bool UpdateAvailable,
    DateTime? LastCheckedUtc,
    string LastError);
