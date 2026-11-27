using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Module.Notifications.Services
{
    public interface IFirebaseCloudMessagingService
    {
        bool IsConfigured { get; }

        Task<string> SendSingleMessageAsync(string token, string title, string body, IDictionary<string, string> data = null);

        Task<string> SendTopicMessageAsync(string topic, string title, string body, IDictionary<string, string> data = null);

        Task<PushNotificationResult> SendMulticastAsync(IEnumerable<string> tokens, string title, string body, IDictionary<string, string> data = null);
    }
}
