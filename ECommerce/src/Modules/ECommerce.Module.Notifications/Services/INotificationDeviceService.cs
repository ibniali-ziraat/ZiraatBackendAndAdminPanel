using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Notifications.Areas.Notifications.ViewModels;
using ECommerce.Module.Notifications.Models;

namespace ECommerce.Module.Notifications.Services
{
    public interface INotificationDeviceService
    {
        Task<NotificationDevice> SaveTokenAsync(string token, string platform, long? userId, decimal? latitude = null, decimal? longitude = null, string locationName = null);

        Task<IList<NotificationDeviceDto>> GetDevicesAsync(long? userId = null);

        Task<IList<string>> GetTokensForUsersAsync(IEnumerable<long> userIds);
    }
}
