#pragma warning disable CS1591
using Jellyfin.Plugin.SortAdditions.ScheduledTasks;
using MediaBrowser.Controller;

#pragma warning disable CS1591
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.SortAdditions
{
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddHttpClient();
            serviceCollection.AddSingleton<Logger>();
            serviceCollection.AddTransient<TheWorstSolution>();
            serviceCollection.AddTransient<RomajiNamingTask>();
        }
    }
}
