using System.Collections.Generic;

namespace ECommerce.Module.Notifications.Areas.Notifications.ViewModels
{
    public class SendTopicPushNotificationRequest
    {
        public string Topic { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        public IDictionary<string, string> Data { get; set; }
    }
}
