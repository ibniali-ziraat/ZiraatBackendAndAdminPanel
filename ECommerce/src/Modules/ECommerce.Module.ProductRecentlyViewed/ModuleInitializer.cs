using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using MediatR;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.Core.Events;
using ECommerce.Module.ProductRecentlyViewed.Data;
using ECommerce.Module.ProductRecentlyViewed.Events;
using ECommerce.Infrastructure;

namespace ECommerce.Module.ProductRecentlyViewed
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IRecentlyViewedProductRepository, RecentlyViewedProductRepository>();
            services.AddTransient<INotificationHandler<EntityViewed>, EntityViewedHandler>();

            GlobalConfiguration.RegisterAngularModule("simplAdmin.recentlyViewed");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
