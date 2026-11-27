using System.Collections.Generic;

namespace ECommerce.Module.Notifications.Areas.Notifications.ViewModels
{
    public class SendWeatherPushNotificationRequest
    {
        public long[] UserIds { get; set; }

        public string[] Tokens { get; set; }

        public string Topic { get; set; }

        public decimal? Lat { get; set; }

        public decimal? Lon { get; set; }

        public string LocationName { get; set; }

        public string Package { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        public IDictionary<string, string> Data { get; set; }

        public bool IncludeCurrentWeather { get; set; } = true;
    }
}
