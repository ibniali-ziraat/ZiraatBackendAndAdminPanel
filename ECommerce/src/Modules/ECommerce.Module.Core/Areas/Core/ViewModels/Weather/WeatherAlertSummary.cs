namespace ECommerce.Module.Core.Areas.Core.ViewModels.Weather
{
    public class WeatherAlertSummary
    {
        public string AlertKey { get; set; }

        public string Event { get; set; }

        public string Severity { get; set; }

        public string Headline { get; set; }

        public string Description { get; set; }

        public string Instruction { get; set; }

        public string StartsAt { get; set; }

        public string EndsAt { get; set; }
    }
}
