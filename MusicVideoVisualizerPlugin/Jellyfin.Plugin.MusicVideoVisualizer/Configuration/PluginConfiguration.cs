using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MusicVideoVisualizer.Configuration;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public bool Enabled { get; set; } = true;

    public string[] LibraryIds { get; set; } = [];

    public int BarCount { get; set; } = 64;

    public int HeightPercent { get; set; } = 22;

    public double Opacity { get; set; } = 0.9;
}
