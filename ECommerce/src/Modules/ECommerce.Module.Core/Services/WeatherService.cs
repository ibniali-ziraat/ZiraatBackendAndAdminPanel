using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels.Weather;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Module.Core.Services
{
    public class WeatherService : IWeatherService
    {
        private const string DefaultMeteoblueForecastBaseUrl = "https://my.meteoblue.com/packages/";
        private const string DefaultMeteoblueImageBaseUrl = "https://my.meteoblue.com/images/";
        private const string DefaultWeatherApiForecastUrl = "https://api.weatherapi.com/v1/forecast.json";
        private const string DefaultWeatherApiAlertsUrl = "https://api.weatherapi.com/v1/alerts.json";
        private const string DefaultMeteoblueWarningsUrl = "https://my.meteoblue.com/warnings/list";
        private const string DefaultAlertsProvider = "WeatherApi";
        private const string DefaultForecastPackage = "basic-1h_basic-day_current";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public WeatherService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<WeatherProxyResponse> GetMeteoblueForecastAsync(WeatherForecastRequest request, IDictionary<string, string> extraQuery = null)
        {
            ValidateCoordinates(request?.Lat, request?.Lon);

            var apiKey = GetRequiredSetting("Weather:Meteoblue:ApiKey", "Meteoblue API key is not configured.");
            var packageName = string.IsNullOrWhiteSpace(request.Package)
                ? GetSetting("Weather:Meteoblue:DefaultPackage", DefaultForecastPackage)
                : request.Package.Trim();

            var query = BuildBaseMeteoblueQuery(apiKey, extraQuery);
            query["lat"] = FormatDecimal(request.Lat);
            query["lon"] = FormatDecimal(request.Lon);

            if (request.Asl.HasValue)
            {
                query["asl"] = request.Asl.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(request.City))
            {
                query["city"] = request.City.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.TimeZone))
            {
                query["tz"] = request.TimeZone.Trim();
            }

            var url = BuildUrl(GetSetting("Weather:Meteoblue:ForecastBaseUrl", DefaultMeteoblueForecastBaseUrl), packageName, query);
            return await GetTextAsync(url);
        }

        public async Task<WeatherProxyResponse> GetMeteoblueImageAsync(WeatherImageRequest request, IDictionary<string, string> extraQuery = null)
        {
            ValidateCoordinates(request?.Lat, request?.Lon);
            if (string.IsNullOrWhiteSpace(request.Path))
            {
                throw new ArgumentException("path required", nameof(request.Path));
            }

            var apiKey = GetRequiredSetting("Weather:Meteoblue:ApiKey", "Meteoblue API key is not configured.");
            var query = BuildBaseMeteoblueQuery(apiKey, extraQuery);
            query["lat"] = FormatDecimal(request.Lat);
            query["lon"] = FormatDecimal(request.Lon);
            query.TryAdd("format", "png");
            query.TryAdd("dpi", "72");
            query.TryAdd("temperature_units", "C");

            var url = BuildUrl(GetSetting("Weather:Meteoblue:ImageBaseUrl", DefaultMeteoblueImageBaseUrl), request.Path, query);
            var client = _httpClientFactory.CreateClient();
            using var httpRequest = CreateGetRequest(url, "image/png");
            using var response = await client.SendAsync(httpRequest);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(BuildProviderErrorMessage(response.StatusCode, errorContent), null, response.StatusCode);
            }

            return new WeatherProxyResponse
            {
                Bytes = await response.Content.ReadAsByteArrayAsync(),
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "image/png"
            };
        }

        public async Task<WeatherProxyResponse> GetWeatherApiForecastAsync(WeatherApiForecastRequest request, IDictionary<string, string> extraQuery = null)
        {
            if (string.IsNullOrWhiteSpace(request?.Q))
            {
                throw new ArgumentException("q required", nameof(request.Q));
            }

            var apiKey = GetRequiredSetting("Weather:WeatherApi:ApiKey", "WeatherAPI key is not configured.");
            var query = SanitizeExtraQuery(extraQuery);
            query["key"] = apiKey;
            query["q"] = request.Q.Trim();
            query["days"] = Math.Clamp(request.Days, 1, 10).ToString(CultureInfo.InvariantCulture);
            query["aqi"] = string.IsNullOrWhiteSpace(request.Aqi) ? "no" : request.Aqi.Trim();
            query["alerts"] = string.IsNullOrWhiteSpace(request.Alerts) ? "yes" : request.Alerts.Trim();

            var url = QueryHelpers.AddQueryString(GetSetting("Weather:WeatherApi:ForecastUrl", DefaultWeatherApiForecastUrl), query);
            return await GetTextAsync(url);
        }

        public async Task<WeatherCurrentSummary> GetCurrentSummaryAsync(decimal lat, decimal lon, string locationName = null, string packageName = null)
        {
            var response = await GetMeteoblueForecastAsync(new WeatherForecastRequest
            {
                Lat = lat,
                Lon = lon,
                Package = packageName
            }, new Dictionary<string, string>
            {
                ["temperatureUnit"] = "C",
                ["windSpeedUnit"] = "km/h"
            });

            using var document = JsonDocument.Parse(response.Content);
            var root = document.RootElement;
            var current = TryGetObject(root, "data_current");
            var hourly = TryGetObject(root, "data_1h");
            var day = TryGetObject(root, "data_day");
            var hourIndex = ResolveCurrentHourlyIndex(hourly);

            var temp = ReadNumber(current, "temperature") ?? ReadArrayNumber(hourly, "temperature", hourIndex);
            var feels = ReadArrayNumber(hourly, "felttemperature", hourIndex);
            var humidity = ReadNumber(current, "relativehumidity") ?? ReadArrayNumber(hourly, "relativehumidity", hourIndex);
            var wind = ReadNumber(current, "windspeed") ?? ReadArrayNumber(hourly, "windspeed", hourIndex);
            var condition = ReadNumber(current, "pictocode") ?? ReadArrayNumber(hourly, "pictocode", hourIndex);

            return new WeatherCurrentSummary
            {
                LocationName = locationName ?? ReadString(root, "location_name"),
                TemperatureC = RoundNullable(temp),
                FeelsLikeC = RoundNullable(feels),
                Humidity = RoundNullable(humidity),
                WindKph = RoundNullable(wind),
                ConditionCode = RoundNullable(condition),
                HighTempC = RoundNullable(ReadArrayNumber(day, "temperature_max", 0)),
                LowTempC = RoundNullable(ReadArrayNumber(day, "temperature_min", 0)),
                SentAt = DateTimeOffset.UtcNow.ToString("O")
            };
        }
        public async Task<WeatherProxyResponse> GetMeteoblueWarningsAsync(decimal lat, decimal lon, IDictionary<string, string> extraQuery = null)
        {
            ValidateCoordinates(lat, lon);
            var apiKey = GetRequiredSetting("Weather:Meteoblue:ApiKey", "Meteoblue API key is not configured.");
            var query = BuildBaseMeteoblueQuery(apiKey, extraQuery);
            query["lat"] = FormatDecimal(lat);
            query["lon"] = FormatDecimal(lon);

            var url = QueryHelpers.AddQueryString(GetSetting("Weather:Meteoblue:WarningsUrl", DefaultMeteoblueWarningsUrl), query);
            return await GetTextAsync(url);
        }

        public async Task<WeatherProxyResponse> GetWeatherApiAlertsAsync(decimal lat, decimal lon, IDictionary<string, string> extraQuery = null)
        {
            ValidateCoordinates(lat, lon);
            var apiKey = GetRequiredSetting("Weather:WeatherApi:ApiKey", "WeatherAPI key is not configured.");
            var query = SanitizeExtraQuery(extraQuery);
            query["key"] = apiKey;
            query["q"] = $"{FormatDecimal(lat)},{FormatDecimal(lon)}";

            var url = QueryHelpers.AddQueryString(GetSetting("Weather:WeatherApi:AlertsUrl", DefaultWeatherApiAlertsUrl), query);
            return await GetTextAsync(url);
        }

        public async Task<IList<WeatherAlertSummary>> GetWeatherAlertsAsync(decimal lat, decimal lon, IDictionary<string, string> extraQuery = null)
        {
            var provider = GetSetting("Weather:Alerts:Provider", DefaultAlertsProvider);
            WeatherProxyResponse response;
            if (string.Equals(provider, "Meteoblue", StringComparison.OrdinalIgnoreCase))
            {
                response = await GetMeteoblueWarningsAsync(lat, lon, extraQuery);
            }
            else if (string.Equals(provider, "WeatherApi", StringComparison.OrdinalIgnoreCase))
            {
                response = await GetWeatherApiAlertsAsync(lat, lon, extraQuery);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported weather alert provider '{provider}'. Use 'WeatherApi' or 'Meteoblue'.");
            }

            using var document = JsonDocument.Parse(response.Content);
            return ExtractWeatherAlerts(document.RootElement, lat, lon);
        }

        private static IList<WeatherAlertSummary> ExtractWeatherAlerts(JsonElement root, decimal lat, decimal lon)
        {
            var results = TryGetArray(root, "results") ?? TryGetWeatherApiAlertsArray(root) ?? TryGetArray(root, "alerts") ?? TryGetArray(root, "warnings");
            if (!results.HasValue)
            {
                return Array.Empty<WeatherAlertSummary>();
            }

            var alerts = new List<WeatherAlertSummary>();
            foreach (var item in results.Value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var eventName = FirstString(item, "event", "eventName", "type", "phenomenon", "name") ?? "Weather";
                var severity = FirstString(item, "severity", "severityText", "level", "awareness_level") ?? string.Empty;
                var headline = FirstString(item, "headline", "title", "event") ?? eventName;
                var startsAt = FirstString(item, "onset", "startsAt", "start", "effective", "begin") ?? FirstNestedString(item, "periods", "begin");
                var endsAt = FirstString(item, "expires", "endsAt", "end", "until") ?? FirstNestedString(item, "periods", "end");
                var description = FirstString(item, "description", "desc", "text", "message") ?? FirstNestedString(item, "info", "description");
                var instruction = FirstString(item, "instruction", "recommendation") ?? FirstNestedString(item, "info", "instruction");
                var providerKey = FirstString(item, "id", "alertId", "identifier", "code");

                alerts.Add(new WeatherAlertSummary
                {
                    AlertKey = BuildAlertKey(providerKey, eventName, severity, startsAt, endsAt, lat, lon),
                    Event = eventName,
                    Severity = severity,
                    Headline = headline,
                    Description = description,
                    Instruction = instruction,
                    StartsAt = startsAt,
                    EndsAt = endsAt
                });
            }

            return alerts;
        }

        private static JsonElement? TryGetArray(JsonElement element, string propertyName)
        {
            return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array
                ? value
                : null;
        }

        private static JsonElement? TryGetWeatherApiAlertsArray(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("alerts", out var alerts))
            {
                return null;
            }

            if (alerts.ValueKind == JsonValueKind.Array)
            {
                return alerts;
            }

            return alerts.ValueKind == JsonValueKind.Object && alerts.TryGetProperty("alert", out var alertItems) && alertItems.ValueKind == JsonValueKind.Array
                ? alertItems
                : null;
        }

        private static string FirstString(JsonElement element, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                var value = ReadString(element, propertyName);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }

                var number = ReadNumber(element, propertyName);
                if (number.HasValue)
                {
                    return number.Value.ToString(CultureInfo.InvariantCulture);
                }
            }

            return null;
        }

        private static string FirstNestedString(JsonElement element, string propertyName, string nestedPropertyName)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                {
                    var result = FirstString(item, nestedPropertyName);
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        return result;
                    }
                }
            }

            return value.ValueKind == JsonValueKind.Object ? FirstString(value, nestedPropertyName) : null;
        }

        private static string BuildAlertKey(string providerKey, string eventName, string severity, string startsAt, string endsAt, decimal lat, decimal lon)
        {
            if (!string.IsNullOrWhiteSpace(providerKey))
            {
                return providerKey.Trim();
            }

            return string.Join(":", new[]
            {
                FormatDecimal(Math.Round(lat, 3)),
                FormatDecimal(Math.Round(lon, 3)),
                eventName,
                severity,
                startsAt,
                endsAt
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        private async Task<WeatherProxyResponse> GetTextAsync(string url)
        {
            var client = _httpClientFactory.CreateClient();
            using var httpRequest = CreateGetRequest(url, "application/json");
            using var response = await client.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(BuildProviderErrorMessage(response.StatusCode, content), null, response.StatusCode);
            }

            return new WeatherProxyResponse
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
            };
        }

        private HttpRequestMessage CreateGetRequest(string url, string accept)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.TryParseAdd(GetSetting("Weather:UserAgent", "ECommerceApi/1.0"));
            request.Headers.Accept.TryParseAdd(accept);
            return request;
        }

        private static string BuildProviderErrorMessage(HttpStatusCode statusCode, string content)
        {
            var message = $"Weather provider returned {(int)statusCode} {statusCode}.";
            if (string.IsNullOrWhiteSpace(content))
            {
                return message;
            }

            var trimmed = content.Trim();
            if (trimmed.Length > 1000)
            {
                trimmed = trimmed.Substring(0, 1000);
            }

            return message + " Response: " + trimmed;
        }
        private static IDictionary<string, string> BuildBaseMeteoblueQuery(string apiKey, IDictionary<string, string> extraQuery)
        {
            var query = SanitizeExtraQuery(extraQuery);
            query["apikey"] = apiKey;
            query["format"] = "json";
            return query;
        }

        private static Dictionary<string, string> SanitizeExtraQuery(IDictionary<string, string> extraQuery)
        {
            var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (extraQuery != null)
            {
                foreach (var item in extraQuery.Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value)))
                {
                    query[item.Key] = item.Value;
                }
            }

            query.Remove("apikey");
            query.Remove("key");
            return query;
        }

        private static string BuildUrl(string baseUrl, string path, IDictionary<string, string> query)
        {
            var normalizedBase = baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : baseUrl + "/";
            var normalizedPath = path.Trim().TrimStart('/');
            return QueryHelpers.AddQueryString(normalizedBase + normalizedPath, query);
        }

        private static string FormatDecimal(decimal value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private string GetSetting(string key, string fallback)
        {
            return string.IsNullOrWhiteSpace(_configuration[key]) ? fallback : _configuration[key];
        }

        private string GetRequiredSetting(string key, string message)
        {
            var value = _configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(message);
            }

            return value;
        }

        private static void ValidateCoordinates(decimal? lat, decimal? lon)
        {
            if (!lat.HasValue || !lon.HasValue || lat < -90 || lat > 90 || lon < -180 || lon > 180)
            {
                throw new ArgumentException("valid lat/lon required");
            }
        }

        private static JsonElement? TryGetObject(JsonElement element, string propertyName)
        {
            return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Object
                ? value
                : null;
        }

        private static string ReadString(JsonElement element, string propertyName)
        {
            return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        private static double? ReadNumber(JsonElement? element, string propertyName)
        {
            if (!element.HasValue || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            return value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var result) ? result : null;
        }

        private static double? ReadArrayNumber(JsonElement? element, string propertyName, int index)
        {
            if (!element.HasValue || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            if (index < 0 || index >= value.GetArrayLength())
            {
                return null;
            }

            var item = value[index];
            return item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out var result) ? result : null;
        }

        private static int ResolveCurrentHourlyIndex(JsonElement? hourly)
        {
            if (!hourly.HasValue || hourly.Value.ValueKind != JsonValueKind.Object || !hourly.Value.TryGetProperty("time", out var times) || times.ValueKind != JsonValueKind.Array)
            {
                return 0;
            }

            var now = DateTimeOffset.UtcNow;
            var fallback = Math.Max(times.GetArrayLength() - 1, 0);
            for (var i = 0; i < times.GetArrayLength(); i++)
            {
                var value = times[i].ValueKind == JsonValueKind.String ? times[i].GetString() : null;
                if (DateTimeOffset.TryParse(value?.Replace(" ", "T"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed) && parsed >= now)
                {
                    return i;
                }
            }

            return fallback;
        }

        private static int? RoundNullable(double? value)
        {
            return value.HasValue ? Convert.ToInt32(Math.Round(value.Value)) : null;
        }
    }
}




