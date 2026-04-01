#pragma warning disable CS1591
using MediaBrowser.Controller;

#pragma warning disable CS1591
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.SortAdditions
{
    public class SortAdditionsServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddTransient<Extensions.TheWorstSolution>();
        }
    }
}
