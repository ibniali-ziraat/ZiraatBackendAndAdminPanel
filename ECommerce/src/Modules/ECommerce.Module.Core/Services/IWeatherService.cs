using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels.Weather;

namespace ECommerce.Module.Core.Services
{
    public interface IWeatherService
    {
        Task<WeatherProxyResponse> GetMeteoblueForecastAsync(WeatherForecastRequest request, IDictionary<string, string> extraQuery = null);

        Task<WeatherProxyResponse> GetMeteoblueImageAsync(WeatherImageRequest request, IDictionary<string, string> extraQuery = null);

        Task<WeatherProxyResponse> GetWeatherApiForecastAsync(WeatherApiForecastRequest request, IDictionary<string, string> extraQuery = null);

        Task<WeatherCurrentSummary> GetCurrentSummaryAsync(decimal lat, decimal lon, string locationName = null, string packageName = null);

        Task<WeatherProxyResponse> GetMeteoblueWarningsAsync(decimal lat, decimal lon, IDictionary<string, string> extraQuery = null);

        Task<WeatherProxyResponse> GetWeatherApiAlertsAsync(decimal lat, decimal lon, IDictionary<string, string> extraQuery = null);

        Task<IList<WeatherAlertSummary>> GetWeatherAlertsAsync(decimal lat, decimal lon, IDictionary<string, string> extraQuery = null);
    }
}

