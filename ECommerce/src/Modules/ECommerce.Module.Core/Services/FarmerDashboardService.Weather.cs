using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Areas.Core.ViewModels.Weather;
using ECommerce.Module.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace ECommerce.Module.Core.Services
{
    public partial class FarmerDashboardService
    {
        public async Task<FarmerWeatherDashboardVm> GetWeather(long userId, long? orchardProfileId = null)
        {
            var query = _orchards.Query().AsNoTracking().Where(x => x.UserId == userId);
            if (orchardProfileId.HasValue) query = query.Where(x => x.Id == orchardProfileId.Value);
            var orchards = await query.OrderBy(x => x.Id).ToListAsync();
            if (orchardProfileId.HasValue && orchards.Count == 0) throw new KeyNotFoundException("Orchard not found.");
            var result = new FarmerWeatherDashboardVm
            {
                HistoricalMetrics = new List<FarmerMetricVm>
                {
                    Metric("rainfall-last-seven-days", "Observed Rainfall: Last Seven Days", null, "mm", "Requires historical observations; forecast rainfall must not be presented as observed rainfall."),
                    Metric("degree-days", "Growing Degree Days", null, "degree-days", "Requires daily observed temperatures and a crop-specific base temperature."),
                    Metric("hail-frequency", "Hail Frequency", null, "count", "Requires dated hail observations."),
                    Metric("hail-impact", "Hail Impact", null, "%", "Requires measured crop damage assessments.")
                }
            };
            foreach (var orchard in orchards)
            {
                var row = new FarmerOrchardWeatherVm
                {
                    OrchardProfileId = orchard.Id, OrchardName = orchard.OrchardName, Current = await CurrentWeather(orchard)
                };
                if (ValidCoordinates(orchard))
                {
                    try
                    {
                        var forecast = await _weather.GetMeteoblueForecastAsync(new WeatherForecastRequest
                        {
                            Lat = orchard.Latitude.Value, Lon = orchard.Longitude.Value, City = orchard.District
                        }, new Dictionary<string, string> { ["temperatureUnit"] = "C", ["windSpeedUnit"] = "km/h", ["precipitationUnit"] = "metric" });
                        row.Forecast = JObject.Parse(forecast.Content);
                        row.ForecastStatus = "available";
                    }
                    catch (Exception ex) when (IsWeatherFailure(ex))
                    {
                        row.ForecastStatus = "unavailable";
                        LogWeatherFailure(orchard.Id, "forecast", ex);
                    }
                    try
                    {
                        row.Alerts = await _weather.GetWeatherAlertsAsync(orchard.Latitude.Value, orchard.Longitude.Value);
                        row.AlertsStatus = "available";
                    }
                    catch (Exception ex) when (IsWeatherFailure(ex))
                    {
                        row.AlertsStatus = "unavailable";
                        LogWeatherFailure(orchard.Id, "alerts", ex);
                    }
                }
                result.Orchards.Add(row);
            }
            return result;
        }

        private async Task<FarmerWeatherSummaryVm> CurrentWeather(OrchardProfile orchard)
        {
            if (_currentWeather.TryGetValue(orchard.Id, out var cached)) return cached;
            var result = new FarmerWeatherSummaryVm { OrchardProfileId = orchard.Id, Location = orchard.District };
            if (!ValidCoordinates(orchard))
            {
                result.UnavailableReason = "Valid orchard latitude and longitude are required.";
            }
            else
            {
                try
                {
                    var current = await _weather.GetCurrentSummaryAsync(orchard.Latitude.Value, orchard.Longitude.Value, orchard.District);
                    result.TemperatureCelsius = current.TemperatureC;
                    result.FeelsLikeCelsius = current.FeelsLikeC;
                    result.HumidityPercent = current.Humidity;
                    result.WindSpeedKmph = current.WindKph;
                    result.DataStatus = current.TemperatureC.HasValue && current.Humidity.HasValue && current.WindKph.HasValue ? "available" : "partial";
                    // Storm/frost/drought classifications come from provider alerts, not arbitrary temperature thresholds.
                }
                catch (Exception ex) when (IsWeatherFailure(ex))
                {
                    result.DataStatus = "unavailable";
                    result.UnavailableReason = "The configured weather provider could not return current conditions.";
                    LogWeatherFailure(orchard.Id, "current", ex);
                }
            }
            _currentWeather[orchard.Id] = result;
            return result;
        }

        private static bool ValidCoordinates(OrchardProfile orchard) =>
            orchard.Latitude is >= -90 and <= 90 && orchard.Longitude is >= -180 and <= 180;

        private static bool IsWeatherFailure(Exception ex) =>
            ex is HttpRequestException or InvalidOperationException or ArgumentException or JsonException or Newtonsoft.Json.JsonException or TaskCanceledException;

        private void LogWeatherFailure(long orchardId, string component, Exception ex)
        {
            // Provider exceptions may include authenticated URLs; log the type, not the exception body.
            _logger.LogWarning("Dashboard weather {Component} unavailable for orchard {OrchardId}: {FailureType}", component, orchardId, ex.GetType().Name);
        }
    }
}
