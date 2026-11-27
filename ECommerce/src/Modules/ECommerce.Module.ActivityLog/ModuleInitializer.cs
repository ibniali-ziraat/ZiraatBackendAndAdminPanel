using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.ActivityLog.Data;
using ECommerce.Module.ActivityLog.Events;
using ECommerce.Module.Core.Events;

namespace ECommerce.Module.ActivityLog
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IActivityTypeRepository, ActivityRepository>();
            services.AddTransient<INotificationHandler<EntityViewed>, EntityViewedHandler>();

            GlobalConfiguration.RegisterAngularModule("simplAdmin.activityLog");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
