using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.PaymentBraintree.Services;

namespace ECommerce.Module.PaymentBraintree
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IBraintreeConfiguration, BraintreeConfiguration>();

            GlobalConfiguration.RegisterAngularModule("simplAdmin.paymentBraintree");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
