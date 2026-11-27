using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.Orders.Events;
using ECommerce.Module.PaymentRazor.Events;
using ECommerce.Module.PaymentRazor.Services;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Module.PaymentRazor
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddHttpClient();
            serviceCollection.AddTransient<IRazorpayVendorPayoutService, RazorpayVendorPayoutService>();
            serviceCollection.AddTransient<INotificationHandler<OrderChanged>, RazorpayOrderCompletedHandler>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.paymentRazor");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
