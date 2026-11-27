using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ECommerce.Module.Notifications.Models;
using ECommerce.Module.SignalR.Hubs;
using ECommerce.Module.SignalR.RealTime;

namespace ECommerce.Module.Notifications.Services
{
    /// <summary>
    /// Sends real-time notifications through SignalR.
    /// </summary>
    public class SignalRRealTimeNotifier : IRealTimeNotifier
    {
        public ILogger<SignalRRealTimeNotifier> Logger { get; set; }

        private readonly IOnlineClientManager _onlineClientManager;
        private readonly IHubContext<CommonHub> _hubContext;

        public SignalRRealTimeNotifier(
            IOnlineClientManager onlineClientManager,
            IHubContext<CommonHub> hubContext,
            ILogger<SignalRRealTimeNotifier> logger)
        {
            _onlineClientManager = onlineClientManager;
            _hubContext = hubContext;
            Logger = logger;
        }

        public async Task SendNotificationsAsync(UserNotificationDto[] userNotifications)
        {
            foreach (var userNotification in userNotifications)
            {
                try
                {
                    var onlineClients = _onlineClientManager.GetAllByUserId(userNotification.UserId);
                    if (onlineClients.Count == 0)
                    {
                        Logger.LogInformation(
                            "No online SignalR clients found for notification user {UserId}. NotificationId: {NotificationId}",
                            userNotification.UserId,
                            userNotification.Id);
                        continue;
                    }

                    foreach (var onlineClient in onlineClients)
                    {
                        var signalRClient = _hubContext.Clients.Client(onlineClient.ConnectionId);
                        if (signalRClient == null)
                        {
                            Logger.LogDebug(
                                "Cannot get SignalR client for user {UserId} with connectionId {ConnectionId}.",
                                userNotification.UserId,
                                onlineClient.ConnectionId);
                            continue;
                        }

                        await signalRClient.SendAsync("getNotification", userNotification);
                        Logger.LogInformation(
                            "Sent SignalR notification {NotificationId} to user {UserId} on connection {ConnectionId}.",
                            userNotification.Id,
                            userNotification.UserId,
                            onlineClient.ConnectionId);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Could not send SignalR notification to user {UserId}.", userNotification.UserId);
                }
            }
        }
    }
}
