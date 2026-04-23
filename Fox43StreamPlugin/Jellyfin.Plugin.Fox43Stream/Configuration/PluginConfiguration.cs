using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Fox43Stream.Configuration;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public string ChannelName { get; set; } = "FOX43 Harrisburg";

    public string ChannelNumber { get; set; } = "43";

    public string StreamUrl { get; set; } = "https://video.tegnaone.com/wpmt/live/v1/master/f9c1bf9ffd6ac86b6173a7c169ff6e3f4efbd693/WPMT-Production/live/index.m3u8";

    public string TvGuideId { get; set; } = "FOX43";

    public bool RedirectDirectlyToStream { get; set; } = true;
}
