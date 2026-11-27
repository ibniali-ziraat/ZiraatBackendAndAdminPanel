using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.ShippingPrices.Services;
using ECommerce.Module.ShippingTableRate.Services;
using ECommerce.Infrastructure;

namespace ECommerce.Module.ShippingTableRate
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IShippingPriceServiceProvider, TableRateShippingServiceProvider>();

            GlobalConfiguration.RegisterAngularModule("simplAdmin.shipping-tablerate");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
