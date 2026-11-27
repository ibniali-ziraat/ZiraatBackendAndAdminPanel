using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels.Weather;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Notifications.Areas.Notifications.ViewModels;
using ECommerce.Module.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Module.Notifications.Areas.Notifications.Controllers
{
    [Area("Notifications")]
    [ApiController]
    [Authorize]
    [Route("api/notifications")]
    public class PushNotificationsController : ControllerBase
    {
        private readonly INotificationDeviceService _deviceService;
        private readonly IFirebaseCloudMessagingService _fcmService;
        private readonly IWeatherService _weatherService;
        private readonly IWorkContext _workContext;

        public PushNotificationsController(
            INotificationDeviceService deviceService,
            IFirebaseCloudMessagingService fcmService,
            IWeatherService weatherService,
            IWorkContext workContext)
        {
            _deviceService = deviceService;
            _fcmService = fcmService;
            _weatherService = weatherService;
            _workContext = workContext;
        }

        [HttpGet("push/status")]
        [AllowAnonymous]
        public IActionResult Status()
        {
            return Ok(new
            {
                status = "FCM API running",
                configured = _fcmService.IsConfigured
            });
        }

        [HttpPost("save-token")]
        public async Task<IActionResult> SaveToken([FromBody] SaveNotificationDeviceRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Token))
            {
                return BadRequest(new { error = "token required" });
            }

            var userId = model.UserId;
            if (!User.IsInRole("admin"))
            {
                var currentUser = await _workContext.GetCurrentUser();
                userId = currentUser.Id;
            }

            try
            {
                await _deviceService.SaveTokenAsync(model.Token, model.Platform, userId, model.Latitude, model.Longitude, model.LocationName);
                return Ok(new { success = true, message = "Token saved" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("devices")]
        public async Task<IActionResult> GetDevices([FromQuery] long? userId)
        {
            if (!User.IsInRole("admin"))
            {
                var currentUser = await _workContext.GetCurrentUser();
                userId = currentUser.Id;
            }

            var devices = await _deviceService.GetDevicesAsync(userId);
            return Ok(new
            {
                success = true,
                count = devices.Count,
                tokens = devices
            });
        }

        [HttpPost("send-notification")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SendNotification([FromBody] SendPushNotificationRequest model)
        {
            var title = model?.Notification?.Title ?? model?.Title;
            var body = model?.Notification?.Body ?? model?.Body;

            if (model == null || string.IsNullOrWhiteSpace(model.Token) || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                return BadRequest(new { error = "token/title/body required" });
            }

            try
            {
                var messageId = await _fcmService.SendSingleMessageAsync(model.Token, title, body, model.Data);
                return Ok(new { success = true, messageId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("send-topic")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SendTopic([FromBody] SendTopicPushNotificationRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Topic) || string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Body))
            {
                return BadRequest(new { error = "topic/title/body required" });
            }

            try
            {
                var messageId = await _fcmService.SendTopicMessageAsync(model.Topic, model.Title, model.Body, model.Data);
                return Ok(new { success = true, messageId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("send-multiple")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SendMultiple([FromBody] SendMultiplePushNotificationsRequest model)
        {
            if (model?.Tokens == null || model.Tokens.Length == 0)
            {
                return BadRequest(new { error = "tokens required" });
            }

            if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Body))
            {
                return BadRequest(new { error = "title/body required" });
            }

            try
            {
                var result = await _fcmService.SendMulticastAsync(model.Tokens.Where(x => !string.IsNullOrWhiteSpace(x)), model.Title, model.Body, model.Data);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("weather/send")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SendWeather([FromBody] SendWeatherPushNotificationRequest model)
        {
            if (model == null || !HasWeatherTarget(model))
            {
                return BadRequest(new { error = "topic, tokens, or userIds required" });
            }

            WeatherCurrentSummary summary = null;
            if (model.IncludeCurrentWeather && model.Lat.HasValue && model.Lon.HasValue)
            {
                try
                {
                    summary = await _weatherService.GetCurrentSummaryAsync(model.Lat.Value, model.Lon.Value, model.LocationName, model.Package);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new { error = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    return StatusCode(503, new { error = ex.Message });
                }
                catch (HttpRequestException ex)
                {
                    return StatusCode(502, new { error = "Weather provider request failed.", detail = ex.Message });
                }
            }

            var title = string.IsNullOrWhiteSpace(model.Title) ? "Weather update" : model.Title.Trim();
            var body = string.IsNullOrWhiteSpace(model.Body) ? BuildWeatherBody(summary, model.LocationName) : model.Body.Trim();
            if (string.IsNullOrWhiteSpace(body))
            {
                return BadRequest(new { error = "body or lat/lon weather summary required" });
            }

            var data = BuildWeatherData(model.Data, summary, model.LocationName);

            try
            {
                if (!string.IsNullOrWhiteSpace(model.Topic))
                {
                    var messageId = await _fcmService.SendTopicMessageAsync(model.Topic.Trim(), title, body, data);
                    return Ok(new { success = true, target = "topic", messageId, weather = summary });
                }

                var tokens = await ResolveWeatherTokens(model);
                if (tokens.Count == 0)
                {
                    return BadRequest(new { error = "No device tokens found for weather notification." });
                }

                var result = await _fcmService.SendMulticastAsync(tokens, title, body, data);
                return Ok(new
                {
                    result.Success,
                    result.SuccessCount,
                    result.FailureCount,
                    result.Responses,
                    weather = summary
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        private static bool HasWeatherTarget(SendWeatherPushNotificationRequest model)
        {
            return !string.IsNullOrWhiteSpace(model.Topic)
                || model.Tokens?.Any(x => !string.IsNullOrWhiteSpace(x)) == true
                || model.UserIds?.Length > 0;
        }

        private async Task<IList<string>> ResolveWeatherTokens(SendWeatherPushNotificationRequest model)
        {
            var tokens = new HashSet<string>(StringComparer.Ordinal);
            foreach (var token in model.Tokens?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()) ?? Enumerable.Empty<string>())
            {
                tokens.Add(token);
            }

            if (model.UserIds?.Length > 0)
            {
                foreach (var token in await _deviceService.GetTokensForUsersAsync(model.UserIds))
                {
                    tokens.Add(token);
                }
            }

            return tokens.ToList();
        }

        private static string BuildWeatherBody(WeatherCurrentSummary summary, string fallbackLocation)
        {
            if (summary == null)
            {
                return null;
            }

            var location = summary.LocationName ?? fallbackLocation ?? "your area";
            var temp = summary.TemperatureC.HasValue ? $"{summary.TemperatureC.Value}C" : "updated conditions";
            var highLow = summary.HighTempC.HasValue && summary.LowTempC.HasValue
                ? $" High {summary.HighTempC.Value}C, low {summary.LowTempC.Value}C."
                : string.Empty;
            var humidity = summary.Humidity.HasValue ? $" Humidity {summary.Humidity.Value}%." : string.Empty;
            var wind = summary.WindKph.HasValue ? $" Wind {summary.WindKph.Value} km/h." : string.Empty;

            return $"Weather in {location}: {temp}.{highLow}{humidity}{wind}";
        }

        private static IDictionary<string, string> BuildWeatherData(IDictionary<string, string> source, WeatherCurrentSummary summary, string fallbackLocation)
        {
            var data = source == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(source);

            data["alert_type"] = "weather";
            data["target_app"] = "farmer";

            if (summary == null)
            {
                if (!string.IsNullOrWhiteSpace(fallbackLocation))
                {
                    data["location_name"] = fallbackLocation;
                }

                return data;
            }

            AddIfPresent(data, "location_name", summary.LocationName ?? fallbackLocation);
            AddIfPresent(data, "temperature_c", summary.TemperatureC);
            AddIfPresent(data, "feels_like_c", summary.FeelsLikeC);
            AddIfPresent(data, "humidity", summary.Humidity);
            AddIfPresent(data, "wind_kph", summary.WindKph);
            AddIfPresent(data, "condition_code", summary.ConditionCode);
            AddIfPresent(data, "high_temp_c", summary.HighTempC);
            AddIfPresent(data, "low_temp_c", summary.LowTempC);
            AddIfPresent(data, "weather_sent_at", summary.SentAt);
            return data;
        }

        private static void AddIfPresent(IDictionary<string, string> data, string key, object value)
        {
            if (value != null)
            {
                data[key] = Convert.ToString(value);
            }
        }
    }
}
