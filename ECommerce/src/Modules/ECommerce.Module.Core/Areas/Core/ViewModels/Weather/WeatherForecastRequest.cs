namespace ECommerce.Module.Core.Areas.Core.ViewModels.Weather
{
    public class WeatherForecastRequest
    {
        public decimal Lat { get; set; }

        public decimal Lon { get; set; }

        public string Package { get; set; }

        public int? Asl { get; set; }

        public string City { get; set; }

        public string TimeZone { get; set; }
    }
}
