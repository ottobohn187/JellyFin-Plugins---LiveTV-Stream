using Jellyfin.Plugin.ReleaseWatcher.Services;
using Jellyfin.Plugin.ReleaseWatcher.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ReleaseWatcher;

public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHttpClient<ReleaseCheckService>();
        serviceCollection.AddSingleton<IScheduledTask, CheckForJellyfinUpdatesTask>();
    }
}
