using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Module.Notifications.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Module.Notifications.Services
{
    public class FirebaseRealTimeNotifier : IRealTimeNotifier
    {
        private readonly INotificationDeviceService _deviceService;
        private readonly IFirebaseCloudMessagingService _fcmService;
        private readonly IConfiguration _configuration;

        public ILogger<FirebaseRealTimeNotifier> Logger { get; set; }

        public FirebaseRealTimeNotifier(
            INotificationDeviceService deviceService,
            IFirebaseCloudMessagingService fcmService,
            IConfiguration configuration,
            ILogger<FirebaseRealTimeNotifier> logger)
        {
            _deviceService = deviceService;
            _fcmService = fcmService;
            _configuration = configuration;
            Logger = logger;
        }

        public async Task SendNotificationsAsync(UserNotificationDto[] userNotifications)
        {
            if (!_fcmService.IsConfigured)
            {
                Logger.LogInformation("Firebase push notification skipped because Firebase is not configured.");
                return;
            }

            if (!IsEnabled())
            {
                Logger.LogInformation("Firebase push notification skipped because Firebase:Enabled is false.");
                return;
            }

            foreach (var userNotification in userNotifications ?? Array.Empty<UserNotificationDto>())
            {
                try
                {
                    var tokens = await _deviceService.GetTokensForUsersAsync(new[] { userNotification.UserId });
                    if (tokens.Count == 0)
                    {
                        Logger.LogInformation(
                            "Firebase push notification skipped because user {UserId} has no saved device tokens. NotificationId: {NotificationId}, NotificationName: {NotificationName}",
                            userNotification.UserId,
                            userNotification.Id,
                            userNotification.Detail?.NotificationName);
                        continue;
                    }

                    var title = ResolveTitle(userNotification);
                    var body = ResolveBody(userNotification);
                    if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
                    {
                        Logger.LogInformation(
                            "Firebase push notification skipped because title/body could not be resolved. UserId: {UserId}, NotificationId: {NotificationId}, NotificationName: {NotificationName}",
                            userNotification.UserId,
                            userNotification.Id,
                            userNotification.Detail?.NotificationName);
                        continue;
                    }

                    await _fcmService.SendMulticastAsync(tokens, title, body, BuildData(userNotification));
                    Logger.LogInformation(
                        "Firebase push notification sent to {TokenCount} token(s) for user {UserId}. NotificationId: {NotificationId}, NotificationName: {NotificationName}",
                        tokens.Count,
                        userNotification.UserId,
                        userNotification.Id,
                        userNotification.Detail?.NotificationName);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Could not send push notification to user: {UserId}.", userNotification.UserId);
                }
            }
        }

        private bool IsEnabled()
        {
            return !string.Equals(_configuration["Firebase:Enabled"], "false", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveTitle(UserNotificationDto userNotification)
        {
            if (userNotification.Detail?.Data is AlertNotificationData alert && !string.IsNullOrWhiteSpace(alert.Title))
            {
                return alert.Title;
            }

            return userNotification.Detail?.NotificationName ?? "Notification";
        }

        private static string ResolveBody(UserNotificationDto userNotification)
        {
            if (userNotification.Detail?.Data is AlertNotificationData alert && !string.IsNullOrWhiteSpace(alert.Message))
            {
                return alert.Message;
            }

            if (userNotification.Detail?.Data is MessageNotificationData message && !string.IsNullOrWhiteSpace(message.Message))
            {
                return message.Message;
            }

            return userNotification.Detail?.NotificationName;
        }

        private static IDictionary<string, string> BuildData(UserNotificationDto userNotification)
        {
            var data = new Dictionary<string, string>
            {
                ["user_notification_id"] = userNotification.Id.ToString(),
                ["notification_id"] = userNotification.Detail?.Id.ToString(CultureInfo.InvariantCulture),
                ["notification_name"] = userNotification.Detail?.NotificationName,
                ["severity"] = userNotification.Detail?.Severity.ToString(),
                ["created_on"] = userNotification.Detail?.CreatedOn.ToString("O")
            };

            if (userNotification.Detail?.Data?.Properties != null)
            {
                foreach (var item in userNotification.Detail.Data.Properties.Where(x => x.Value != null))
                {
                    data[item.Key] = Convert.ToString(item.Value, CultureInfo.InvariantCulture);
                }
            }

            return data.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
