namespace ECommerce.Module.Notifications.Services
{
    public class PushNotificationSendResponse
    {
        public bool Success { get; set; }

        public string MessageId { get; set; }

        public string Error { get; set; }
    }
}
