using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using MediatR;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.Orders.Events;
using ECommerce.Module.Shipments.Events;
using ECommerce.Module.Shipments.Services;
using ECommerce.Infrastructure;

namespace ECommerce.Module.Shipments
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<INotificationHandler<OrderDetailGot>, OrderDetailGotHandler>();
            services.AddTransient<IShipmentService, ShipmentService>();

            GlobalConfiguration.RegisterAngularModule("simplAdmin.shipment");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
