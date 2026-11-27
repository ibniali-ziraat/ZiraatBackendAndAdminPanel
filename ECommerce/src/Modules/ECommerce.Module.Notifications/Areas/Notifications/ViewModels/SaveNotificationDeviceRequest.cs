namespace ECommerce.Module.Notifications.Areas.Notifications.ViewModels
{
    public class SaveNotificationDeviceRequest
    {
        public string Token { get; set; }

        public string Platform { get; set; }

        public long? UserId { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public string LocationName { get; set; }
    }
}