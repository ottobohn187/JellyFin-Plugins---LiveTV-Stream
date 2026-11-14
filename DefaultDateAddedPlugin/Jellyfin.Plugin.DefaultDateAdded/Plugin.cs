using MediaBrowser.Common.Plugins;
namespace Jellyfin.Plugin.DefaultDateAdded;
public sealed class Plugin : BasePlugin
{
    public override string Name => "Default Date Added Sort";
    public override string Description => "Defaults every user's library views to Date Added descending.";
    public override Guid Id => Guid.Parse("36f9e2f1-c26c-4c58-a334-e5bb508c2855");
}
