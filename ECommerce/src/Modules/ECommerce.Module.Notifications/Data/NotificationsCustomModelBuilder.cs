using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Notifications.Models;

namespace ECommerce.Module.Notifications.Data
{
    public class NotificationsCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserNotification>().ToTable("Notifications_UserNotification");
            modelBuilder.Entity<NotificationDevice>(b =>
            {
                b.ToTable("Notifications_Device");
                b.Property(x => x.Latitude).HasColumnType("decimal(9,6)");
                b.Property(x => x.Longitude).HasColumnType("decimal(9,6)");
                b.HasIndex(x => x.TokenHash).IsUnique();
                b.HasIndex(x => x.UserId);
                b.HasIndex(x => new { x.Latitude, x.Longitude });
            });

            modelBuilder.Entity<WeatherAlertNotificationLog>(b =>
            {
                b.ToTable("Notifications_WeatherAlertLog");
                b.HasIndex(x => new { x.AlertKey, x.TokenHash }).IsUnique();
                b.HasIndex(x => x.SentOn);
            });
        }
    }
}

