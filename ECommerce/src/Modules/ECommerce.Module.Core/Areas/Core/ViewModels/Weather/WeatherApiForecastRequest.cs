namespace ECommerce.Module.Core.Areas.Core.ViewModels.Weather
{
    public class WeatherApiForecastRequest
    {
        public string Q { get; set; }

        public int Days { get; set; } = 7;

        public string Aqi { get; set; } = "no";

        public string Alerts { get; set; } = "yes";
    }
}
