using Jellyfin.Plugin.DefaultDateAdded.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
namespace Jellyfin.Plugin.DefaultDateAdded;
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection services, IServerApplicationHost host) => services.AddHostedService<WebClientInstaller>();
}
