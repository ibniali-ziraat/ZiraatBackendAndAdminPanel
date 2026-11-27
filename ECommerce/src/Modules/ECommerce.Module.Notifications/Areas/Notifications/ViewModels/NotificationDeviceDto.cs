using System;

namespace ECommerce.Module.Notifications.Areas.Notifications.ViewModels
{
    public class NotificationDeviceDto
    {
        public long Id { get; set; }

        public long? UserId { get; set; }

        public string Token { get; set; }

        public string Platform { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public string LocationName { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset UpdatedOn { get; set; }
    }
}