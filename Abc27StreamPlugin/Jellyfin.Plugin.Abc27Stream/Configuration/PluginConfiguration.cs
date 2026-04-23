using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Abc27Stream.Configuration;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public string ChannelName { get; set; } = "ABC27 Harrisburg";

    public string ChannelNumber { get; set; } = "27";

    public string VideoApiBaseUrl { get; set; } = "https://tkx.mp.lura.live/rest/v2/mcp/video";

    public string VideoId { get; set; } = "adstJjQDmxQjd3nl";

    public string Anvack { get; set; } = "qN1zdy1wGBd0MhWNKpuggfVd7q2AWkJo";

    public string StreamUrlOverride { get; set; } = string.Empty;

    public string TvGuideId { get; set; } = "ABC27";

    public bool RedirectDirectlyToStream { get; set; } = true;
}
