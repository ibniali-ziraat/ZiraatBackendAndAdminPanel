using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.SignalR.Hubs;
using ECommerce.Module.SignalR.RealTime;

namespace ECommerce.Module.SignalR
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSignalR();
            serviceCollection.AddSingleton<IOnlineClientManager, OnlineClientManager>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<CommonHub>("/signalr");
            });
        }
    }
}
