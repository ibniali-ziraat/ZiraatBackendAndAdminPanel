using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.Reviews.Data;
using ECommerce.Infrastructure;

namespace ECommerce.Module.Reviews
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IReplyRepository, ReplyRepository>();
            services.AddTransient<IReviewRepository, ReviewRepository>();

            GlobalConfiguration.RegisterAngularModule("simplAdmin.reviews");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
