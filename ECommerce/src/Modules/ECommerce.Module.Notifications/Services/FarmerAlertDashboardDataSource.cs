using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Notifications.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Notifications.Services
{
    public class FarmerAlertDashboardDataSource : IFarmerAlertDataSource
    {
        private readonly IRepositoryWithTypedId<UserNotification, Guid> _users;
        private readonly IRepository<NotificationDetail> _details;
        private readonly IRepository<WeatherAlertNotificationLog> _weather;

        public FarmerAlertDashboardDataSource(IRepositoryWithTypedId<UserNotification, Guid> users,
            IRepository<NotificationDetail> details, IRepository<WeatherAlertNotificationLog> weather)
        {
            _users = users;
            _details = details;
            _weather = weather;
        }

        public async Task<FarmerAlertsDashboardVm> GetAlerts(long userId, DateTimeOffset start, DateTimeOffset to)
        {
            var rows = await (from user in _users.Query().AsNoTracking()
                              join detail in _details.Query().AsNoTracking() on user.NotificationDetailId equals detail.Id
                              where user.UserId == userId && !user.IsDeleted && !detail.IsDeleted && user.CreatedOn >= start && user.CreatedOn < to
                              select new { user.Id, user.State, user.CreatedOn, detail.Data }).ToListAsync();
            var timeline = new List<FarmerAlertTimelineItemVm>();
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Data)) continue;
                try
                {
                    using var doc = JsonDocument.Parse(row.Data);
                    var root = doc.RootElement;
                    var type = Read(root, "AlertType")?.ToLowerInvariant();
                    var category = type switch
                    {
                        "weather" or "weather-alert" => "weather",
                        "pest" or "pest-alert" => "pest",
                        "nutrient" or "nutrient-alert" => "nutrient",
                        "spray" or "spray-advisory" => "spray-advisory",
                        _ => null
                    };
                    if (category == null) continue;
                    timeline.Add(new FarmerAlertTimelineItemVm
                    {
                        Id = row.Id.ToString(), Category = category, Title = Read(root, "Title") ?? Read(root, "Message"),
                        OccurredOn = row.CreatedOn, OccurredText = row.CreatedOn.ToString("O"),
                        IsAcknowledged = row.State == UserNotificationState.Read
                    });
                }
                catch (JsonException) { }
            }
            var sent = _weather.Query().AsNoTracking().Where(x => x.UserId == userId && x.SentOn >= start && x.SentOn < to);
            var weatherCount = await sent.Select(x => x.AlertKey).Distinct().CountAsync();
            var weatherTimeline = await sent.GroupBy(x => new { x.AlertKey, x.Event })
                .Select(x => new { x.Key.AlertKey, x.Key.Event, SentOn = x.Min(y => y.SentOn) })
                .OrderByDescending(x => x.SentOn).Take(20).ToListAsync();
            var acknowledged = timeline.Count(x => x.IsAcknowledged == true);
            var result = new FarmerAlertsDashboardVm
            {
                Summary = new List<FarmerMetricVm>
                {
                    Metric("total-alerts-triggered", "In-App Farm Alerts", timeline.Count, "count", "Stored weather/pest/nutrient/spray alerts only; excludes orders and separate push logs."),
                    Metric("alerts-acknowledged", "In-App Alerts Acknowledged", timeline.Count > 0 ? decimal.Round(acknowledged * 100m / timeline.Count, 2) : null, "%", "Read state / in-app farm alerts. Push delivery is not an acknowledgement."),
                    Metric("weather-alerts-sent", "Distinct Weather Alerts Sent", weatherCount, "count", "Distinct provider alert keys for this user; deduplicated across devices. Not added to in-app totals because no shared event ID links them.")
                },
                RecentTimeline = timeline.OrderByDescending(x => x.OccurredOn).Take(20).ToList()
            };
            foreach (var row in weatherTimeline)
                result.RecentTimeline.Add(new FarmerAlertTimelineItemVm
                {
                    Id = "weather-push:" + row.AlertKey, Category = "weather", Title = row.Event,
                    OccurredOn = row.SentOn, OccurredText = row.SentOn.ToString("O"), IsAcknowledged = null
                });
            result.RecentTimeline = result.RecentTimeline.OrderByDescending(x => x.OccurredOn).Take(20).ToList();
            return result;
        }

        private static string Read(JsonElement root, string name)
        {
            if (root.ValueKind != JsonValueKind.Object) return null;
            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                    return property.Value.GetString();
            }
            foreach (var property in root.EnumerateObject())
                if (string.Equals(property.Name, "Properties", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.Object)
                    return Read(property.Value, name);
            return null;
        }

        private static FarmerMetricVm Metric(string code, string label, decimal? value, string unit, string description) => new()
        {
            Code = code, Label = label, NumericValue = value, Value = value?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Unit = unit, Description = description, Source = "notifications", DataStatus = value.HasValue ? "recorded" : "missing"
        };
    }
}
