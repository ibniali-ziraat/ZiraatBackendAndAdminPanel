  using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.Comments.Data;

namespace ECommerce.Module.Comments
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ICommentRepository, CommentRepository>();

            GlobalConfiguration.RegisterAngularModule("simplAdmin.comments");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
              
        }
    }
}
