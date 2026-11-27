using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using MediatR;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.Core.Events;
using ECommerce.Module.Localization.Events;
using ECommerce.Module.Localization.Services;
using ECommerce.Module.Core.Services;
using ECommerce.Infrastructure;

namespace ECommerce.Module.Localization
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<INotificationHandler<UserSignedIn>, UserSignedInHandler>();
            services.AddTransient<IContentLocalizationService, ContentLocalizationService>();

            GlobalConfiguration.RegisterAngularModule("simplAdmin.localization");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
