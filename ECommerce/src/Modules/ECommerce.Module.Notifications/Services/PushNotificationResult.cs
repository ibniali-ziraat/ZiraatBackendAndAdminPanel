using System.Collections.Generic;

namespace ECommerce.Module.Notifications.Services
{
    public class PushNotificationResult
    {
        public bool Success { get; set; }

        public string MessageId { get; set; }

        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public IList<PushNotificationSendResponse> Responses { get; set; } = new List<PushNotificationSendResponse>();
    }
}
