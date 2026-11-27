using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.Core;
using ECommerce.Module.Core.Events;
using ECommerce.Module.Notifications.Data;
using ECommerce.Module.Notifications.Events;
using ECommerce.Module.Notifications.Jobs;
using ECommerce.Module.Notifications.Notifiers;
using ECommerce.Module.Notifications.Services;
using ECommerce.Module.Orders.Events;
using ECommerce.Module.SignalR.RealTime;

namespace ECommerce.Module.Notifications
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<FarmAdvisoryDelivery>();
            services.AddHostedService<FarmAdvisoryDeliveryWorker>();
            services.AddTransient<INotificationRepository, NotificationRepository>();

            services.AddSingleton<INotificationDefinitionManager, NotificationDefinitionManager>();
            services.AddSingleton<IOnlineClientManager, OnlineClientManager>();
            services.AddSingleton<IRealTimeNotifier, SignalRRealTimeNotifier>();
            services.AddSingleton<IFirebaseCloudMessagingService, FirebaseCloudMessagingService>();
            services.AddTransient<INotificationDeviceService, NotificationDeviceService>();
            services.AddTransient<IRealTimeNotifier, FirebaseRealTimeNotifier>();

            services.AddTransient<INotificationDistributer, NotificationDistributer>();
            services.AddTransient<Services.INotificationPublisher, NotificationPublisher>();
            services.AddTransient<INotificationSubscriptionManager, NotificationSubscriptionManager>();
            services.AddTransient<IUserNotificationManager, UserNotificationManager>();
            services.AddScoped<ECommerce.Module.Core.Services.IFarmerAlertDataSource, FarmerAlertDashboardDataSource>();

            services.AddTransient<ITestNotifier, TestNotifier>();
            services.AddTransient<INotificationHandler<UserSignedIn>, UserSignedInHandler>();
            services.AddTransient<INotificationHandler<OrderCreated>, OrderCreatedVendorNotificationHandler>();
            services.AddTransient<INotificationHandler<OrderChanged>, OrderChangedCustomerNotificationHandler>();
            services.AddTransient<NotificationDistributionJob>();
            services.AddTransient<WeatherAlertBackgroundJob>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.AddSettingDefinitionItems(SettingDefinitions.DefaultItems());
            app.AddNotificationDefinitionItems(NotificationDefinitions.DefaultItems());
        }
    }
}
