using System.Collections.Generic;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class FarmerWeatherDashboardVm
    {
        public IList<FarmerOrchardWeatherVm> Orchards { get; set; } = new List<FarmerOrchardWeatherVm>();
        public IList<FarmerMetricVm> HistoricalMetrics { get; set; } = new List<FarmerMetricVm>();
    }

    public class FarmerOrchardWeatherVm
    {
        public long OrchardProfileId { get; set; }
        public string OrchardName { get; set; }
        public FarmerWeatherSummaryVm Current { get; set; }
        public Newtonsoft.Json.Linq.JObject Forecast { get; set; }
        public string ForecastStatus { get; set; } = "missing";
        public IList<Weather.WeatherAlertSummary> Alerts { get; set; } = new List<Weather.WeatherAlertSummary>();
        public string AlertsStatus { get; set; } = "missing";
    }
}
