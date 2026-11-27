using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.Inventory.Services;
using ECommerce.Infrastructure;
using MediatR;
using ECommerce.Module.Catalog.Events;
using ECommerce.Module.Core.Events;
using ECommerce.Module.Inventory.Event;

namespace ECommerce.Module.Inventory
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IStockService, StockService>();
            serviceCollection.AddTransient<IStockSubscriptionService, StockSubscriptionService>();
            serviceCollection.AddTransient<INotificationHandler<ProductBackInStock>, ProductBackInStockSendEmailHandler>();

            GlobalConfiguration.RegisterAngularModule("simplAdmin.inventory");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
