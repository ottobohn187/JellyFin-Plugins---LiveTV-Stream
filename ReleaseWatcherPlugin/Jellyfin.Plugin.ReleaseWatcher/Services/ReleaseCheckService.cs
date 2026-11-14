using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ReleaseWatcher.Models;
using MediaBrowser.Controller;
using MediaBrowser.Model.Activity;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ReleaseWatcher.Services;

public sealed class ReleaseCheckService
{
    private readonly HttpClient _httpClient;
    private readonly IServerApplicationHost _applicationHost;
    private readonly IActivityManager _activityManager;
    private readonly ILogger<ReleaseCheckService> _logger;
    private readonly SemaphoreSlim _checkLock = new(1, 1);

    public ReleaseCheckService(
        HttpClient httpClient,
        IServerApplicationHost applicationHost,
        IActivityManager activityManager,
        ILogger<ReleaseCheckService> logger)
    {
        _httpClient = httpClient;
        _applicationHost = applicationHost;
        _activityManager = activityManager;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Jellyfin-Release-Watcher/1.0");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public ReleaseStatus GetStatus()
    {
        var config = Plugin.Instance?.Configuration;
        var installed = _applicationHost.ApplicationVersionString;
        var latest = config?.LatestVersion ?? string.Empty;
        return new ReleaseStatus(
            installed,
            latest,
            config?.LatestReleaseName ?? string.Empty,
            config?.LatestReleaseUrl ?? string.Empty,
            IsNewer(latest, installed),
            config?.LastCheckedUtc,
            config?.LastError ?? string.Empty);
    }

    public async Task<ReleaseStatus> CheckAsync(CancellationToken cancellationToken)
    {
        await _checkLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var plugin = Plugin.Instance ?? throw new InvalidOperationException("Release Watcher is not initialized.");
            var config = plugin.Configuration;
            var repository = string.IsNullOrWhiteSpace(config.Repository) ? "jellyfin/jellyfin" : config.Repository.Trim();
            var uri = $"https://api.github.com/repos/{repository}/releases?per_page=20";

            try
            {
                var releases = await _httpClient.GetFromJsonAsync<List<GitHubRelease>>(uri, cancellationToken).ConfigureAwait(false)
                    ?? new List<GitHubRelease>();
                var release = releases.FirstOrDefault(item =>
                    !item.Draft &&
                    (config.IncludePrereleases || !item.Prerelease) &&
                    TryParseVersion(item.TagName, out _));

                if (release is null)
                {
                    throw new InvalidOperationException("GitHub returned no matching Jellyfin releases.");
                }

                config.LatestVersion = NormalizeVersion(release.TagName);
                config.LatestReleaseName = release.Name ?? release.TagName;
                config.LatestReleaseUrl = release.HtmlUrl;
                config.LastCheckedUtc = DateTime.UtcNow;
                config.LastError = string.Empty;

                if (IsNewer(config.LatestVersion, _applicationHost.ApplicationVersionString) &&
                    !string.Equals(config.LastNotifiedVersion, config.LatestVersion, StringComparison.OrdinalIgnoreCase))
                {
                    await CreateActivityAsync(config.LatestVersion, config.LatestReleaseUrl).ConfigureAwait(false);
                    config.LastNotifiedVersion = config.LatestVersion;
                }

                plugin.SaveConfiguration();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                config.LastCheckedUtc = DateTime.UtcNow;
                config.LastError = ex.Message;
                plugin.SaveConfiguration();
                _logger.LogWarning(ex, "Unable to check for a Jellyfin release");
            }

            return GetStatus();
        }
        finally
        {
            _checkLock.Release();
        }
    }

    private Task CreateActivityAsync(string version, string releaseUrl)
    {
        return _activityManager.CreateAsync(new ActivityLog(
            string.Format(CultureInfo.InvariantCulture, "Jellyfin {0} is available", version),
            "JellyfinReleaseAvailable",
            Guid.Empty)
        {
            ShortOverview = "A newer Jellyfin server release is available.",
            Overview = releaseUrl
        });
    }

    private static bool IsNewer(string candidate, string installed)
    {
        return TryParseVersion(candidate, out var candidateVersion) &&
            TryParseVersion(installed, out var installedVersion) &&
            candidateVersion > installedVersion;
    }

    private static bool TryParseVersion(string value, out Version version)
    {
        var normalized = NormalizeVersion(value);
        var prereleaseIndex = normalized.IndexOf('-', StringComparison.Ordinal);
        if (prereleaseIndex >= 0)
        {
            normalized = normalized[..prereleaseIndex];
        }

        return Version.TryParse(normalized, out version!);
    }

    private static string NormalizeVersion(string value)
    {
        return value.Trim().TrimStart('v', 'V');
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; init; } = string.Empty;

        [JsonPropertyName("draft")]
        public bool Draft { get; init; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; init; }
    }
}
