using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels.Weather;
using ECommerce.Module.Core.Services;
using ECommerce.Module.HangfireJobs.Models;
using ECommerce.Module.Notifications.Models;
using ECommerce.Module.Notifications.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Module.Notifications.Jobs
{
    public class WeatherAlertBackgroundJob : ScheduledJob
    {
        private const string DefaultSchedule = "*/10 * * * *";

        private readonly IConfiguration _configuration;
        private readonly IFirebaseCloudMessagingService _fcmService;
        private readonly IRepository<NotificationDevice> _deviceRepository;
        private readonly IRepository<WeatherAlertNotificationLog> _alertLogRepository;
        private readonly IWeatherService _weatherService;

        public WeatherAlertBackgroundJob(
            IConfiguration configuration,
            IFirebaseCloudMessagingService fcmService,
            IRepository<NotificationDevice> deviceRepository,
            IRepository<WeatherAlertNotificationLog> alertLogRepository,
            IWeatherService weatherService,
            ILogger<WeatherAlertBackgroundJob> logger)
        {
            _configuration = configuration;
            _fcmService = fcmService;
            _deviceRepository = deviceRepository;
            _alertLogRepository = alertLogRepository;
            _weatherService = weatherService;
            Logger = logger;
        }

        public override string Schedule => IsEnabled ? GetSetting("Weather:Alerts:Schedule", DefaultSchedule) : null;

        private bool IsEnabled => string.Equals(_configuration["Weather:Alerts:Enabled"], "true", StringComparison.OrdinalIgnoreCase);

        protected override async Task ExecuteAsync(IJobCancellationToken cancellationToken)
        {
            if (!_fcmService.IsConfigured)
            {
                Logger.LogWarning("Weather alert job skipped because Firebase is not configured.");
                return;
            }

            var devices = await _deviceRepository.Query()
                .Where(x => !x.IsDeleted && !string.IsNullOrWhiteSpace(x.Token) && x.Latitude.HasValue && x.Longitude.HasValue)
                .OrderByDescending(x => x.UpdatedOn)
                .ToListAsync();

            var allowedSeverities = GetConfiguredSet("Weather:Alerts:MinimumSeverities", new[] { "Moderate", "Severe", "Extreme" });
            var sentCount = 0;
            var inspectedCount = 0;

            foreach (var device in devices)
            {
                cancellationToken?.ThrowIfCancellationRequested();
                inspectedCount++;

                IList<WeatherAlertSummary> alerts;
                try
                {
                    alerts = await _weatherService.GetWeatherAlertsAsync(device.Latitude.Value, device.Longitude.Value);
                }
                catch (InvalidOperationException ex)
                {
                    Logger.LogWarning(ex, "Weather alert job skipped because weather provider configuration is missing.");
                    return;
                }
                catch (HttpRequestException ex)
                {
                    Logger.LogWarning(ex, "Weather alert request failed for device {DeviceId}.", device.Id);
                    continue;
                }

                foreach (var alert in alerts.Where(x => ShouldSend(x, allowedSeverities)))
                {
                    cancellationToken?.ThrowIfCancellationRequested();
                    if (await AlreadySentAsync(device, alert))
                    {
                        continue;
                    }

                    var data = BuildData(device, alert);
                    await _fcmService.SendSingleMessageAsync(device.Token, "Weather Alert", BuildBody(alert), data);

                    _alertLogRepository.Add(new WeatherAlertNotificationLog
                    {
                        AlertKey = alert.AlertKey,
                        UserId = device.UserId,
                        TokenHash = device.TokenHash,
                        Event = alert.Event,
                        Severity = alert.Severity,
                        SentOn = DateTimeOffset.Now
                    });
                    await _alertLogRepository.SaveChangesAsync();
                    sentCount++;
                }
            }

            Logger.LogInformation("Weather alert job inspected {InspectedCount} device(s) and sent {SentCount} notification(s).", inspectedCount, sentCount);
        }

        private async Task<bool> AlreadySentAsync(NotificationDevice device, WeatherAlertSummary alert)
        {
            return await _alertLogRepository.Query()
                .AnyAsync(x => x.TokenHash == device.TokenHash && x.AlertKey == alert.AlertKey);
        }

        private bool ShouldSend(WeatherAlertSummary alert, ISet<string> allowedSeverities)
        {
            return alert != null
                && !string.IsNullOrWhiteSpace(alert.AlertKey)
                && allowedSeverities.Contains((alert.Severity ?? string.Empty).Trim());
        }

        private static string BuildBody(WeatherAlertSummary alert)
        {
            if (!string.IsNullOrWhiteSpace(alert.Headline))
            {
                return alert.Headline.Trim();
            }

            var severity = string.IsNullOrWhiteSpace(alert.Severity) ? "Weather" : alert.Severity.Trim();
            var eventName = string.IsNullOrWhiteSpace(alert.Event) ? "alert" : alert.Event.Trim();
            return $"{severity} {eventName}";
        }

        private static IDictionary<string, string> BuildData(NotificationDevice device, WeatherAlertSummary alert)
        {
            var data = new Dictionary<string, string>
            {
                ["alert_type"] = "weather",
                ["target_app"] = "farmer",
                ["weather_alert_key"] = alert.AlertKey,
                ["weather_event"] = alert.Event ?? "Weather",
                ["weather_severity"] = alert.Severity ?? string.Empty,
                ["latitude"] = Convert.ToString(device.Latitude, System.Globalization.CultureInfo.InvariantCulture),
                ["longitude"] = Convert.ToString(device.Longitude, System.Globalization.CultureInfo.InvariantCulture)
            };

            AddIfPresent(data, "location_name", device.LocationName);
            AddIfPresent(data, "weather_description", alert.Description);
            AddIfPresent(data, "weather_instruction", alert.Instruction);
            AddIfPresent(data, "weather_starts_at", alert.StartsAt);
            AddIfPresent(data, "weather_ends_at", alert.EndsAt);
            return data;
        }

        private static void AddIfPresent(IDictionary<string, string> data, string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                data[key] = value;
            }
        }

        private ISet<string> GetConfiguredSet(string key, IEnumerable<string> fallback)
        {
            var configured = _configuration.GetSection(key).Get<string[]>();
            var values = configured?.Length > 0 ? configured : fallback;
            return new HashSet<string>(values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
        }

        private string GetSetting(string key, string fallback)
        {
            return string.IsNullOrWhiteSpace(_configuration[key]) ? fallback : _configuration[key];
        }
    }
}

