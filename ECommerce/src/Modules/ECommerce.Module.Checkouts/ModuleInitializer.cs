using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.Checkouts.Services;

namespace ECommerce.Module.Checkouts
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
        }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<ICheckoutService, CheckoutService>();
        }
    }
}
