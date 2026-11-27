namespace ECommerce.Module.Core.Areas.Core.ViewModels.Weather
{
    public class WeatherCurrentSummary
    {
        public string LocationName { get; set; }

        public int? TemperatureC { get; set; }

        public int? FeelsLikeC { get; set; }

        public int? Humidity { get; set; }

        public int? WindKph { get; set; }

        public int? ConditionCode { get; set; }

        public int? HighTempC { get; set; }

        public int? LowTempC { get; set; }

        public string SentAt { get; set; }
    }
}
