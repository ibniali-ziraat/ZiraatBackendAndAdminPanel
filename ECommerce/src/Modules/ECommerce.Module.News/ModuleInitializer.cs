using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.News.Services;
using ECommerce.Infrastructure;

namespace ECommerce.Module.News
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<INewsItemService, NewsItemService>();
            services.AddTransient<INewsCategoryService, NewsCategoryService>();

            GlobalConfiguration.RegisterAngularModule("simplAdmin.news");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
