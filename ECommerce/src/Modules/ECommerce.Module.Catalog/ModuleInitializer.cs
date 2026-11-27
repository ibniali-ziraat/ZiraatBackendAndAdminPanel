using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.Catalog.Data;
using ECommerce.Module.Catalog.Events;
using ECommerce.Module.Catalog.Services;
using ECommerce.Module.Core.Events;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Catalog
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IProductTemplateProductAttributeRepository, ProductTemplateProductAttributeRepository>();
            services.AddTransient<INotificationHandler<ReviewSummaryChanged>, ReviewSummaryChangedHandler>();
            services.AddTransient<IBrandService, BrandService>();
            services.AddTransient<ICategoryService, CategoryService>();
            services.AddTransient<IProductPricingService, ProductPricingService>();
            services.AddTransient<IProductService, ProductService>();
            services.AddScoped<IProductPriceProvider, ProductPriceProvider>();


            GlobalConfiguration.RegisterAngularModule("simplAdmin.catalog");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
