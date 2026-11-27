using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Module.Orders.Events;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.Orders.Services;
using ECommerce.Infrastructure;

namespace ECommerce.Module.Orders
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IOrderService, OrderService>();
            services.AddTransient<IOrderEmailService, OrderEmailService>();
            services.AddHostedService<OrderCancellationBackgroundService>();
            services.AddTransient<INotificationHandler<OrderChanged>, OrderChangedCreateOrderHistoryHandler>();
            services.AddTransient<INotificationHandler<OrderCreated>, OrderCreatedCreateOrderHistoryHandler>();
            services.AddTransient<INotificationHandler<OrderCreated>, OrderCreatedClearCartHandler>();
            services.AddTransient<INotificationHandler<AfterOrderCreated>, AfterOrderCreatedSendEmailHanlder>();
            services.AddScoped<ECommerce.Module.Core.Services.IOrderService, OrderService>();

            GlobalConfiguration.RegisterAngularModule("simplAdmin.orders");
        }

        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
        }
    }
}
