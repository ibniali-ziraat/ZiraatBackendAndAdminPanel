using ECommerce.Infrastructure.Modules;
using ECommerce.Module.Discussion.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Module.Discussion
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IDiscussionForumService, DiscussionForumService>();
            services.AddScoped<ECommerce.Module.Core.Services.IFarmerAdvisoryDataSource, FarmerForumDashboardDataSource>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
