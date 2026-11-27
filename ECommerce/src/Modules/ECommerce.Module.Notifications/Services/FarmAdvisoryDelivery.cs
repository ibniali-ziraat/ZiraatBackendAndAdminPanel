using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Extensions;
using ECommerce.Module.Core.Data;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Notifications.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Module.Notifications.Services
{
    public class FarmAdvisoryDelivery
    {
        private readonly SimplDbContext _db;
        private readonly IUserAccessService _access;
        private readonly INotificationDeviceService _devices;
        private readonly IFirebaseCloudMessagingService _firebase;
        private readonly IEnumerable<IRealTimeNotifier> _notifiers;
        private readonly ISettingService _settings;
        private readonly IConfiguration _config;
        private readonly ILogger<FarmAdvisoryDelivery> _logger;
        public FarmAdvisoryDelivery(SimplDbContext db, IUserAccessService access, INotificationDeviceService devices,
            IFirebaseCloudMessagingService firebase, IEnumerable<IRealTimeNotifier> notifiers, ISettingService settings,
            IConfiguration config, ILogger<FarmAdvisoryDelivery> logger)
        {
            _db = db;
            _access = access;
            _devices = devices;
            _firebase = firebase;
            _notifiers = notifiers;
            _settings = settings;
            _config = config;
            _logger = logger;
        }

        public async Task<bool> ProcessNext(CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;
            var e = await _db.Set<FarmDeliveryEvent>().Where(x => x.DeliveredOn == null && x.NextAttemptOn <= now &&
                (x.LeaseUntil == null || x.LeaseUntil < now)).OrderBy(x => x.CreatedOn).FirstOrDefaultAsync(ct);
            if (e == null) return false;
            e.LeaseUntil = now.AddMinutes(5); e.Version = Guid.NewGuid(); e.Attempts++;
            try { await _db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException) { _db.ChangeTracker.Clear(); return true; }
            try
            {
                e.Outcome = await Deliver(e, ct);
                e.DeliveredOn = DateTimeOffset.UtcNow; e.LastError = null;
                _logger.LogInformation(
                    "Farm notification {EventId} for recipient {RecipientId} completed with outcome {Outcome}.",
                    e.Id, e.RecipientId, e.Outcome);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception error)
            {
                // Provider messages can contain tokens or credentials. Store only a safe error category.
                e.LastError = error.GetType().Name;
                e.NextAttemptOn = DateTimeOffset.UtcNow.AddSeconds(Math.Min(3600, 15 * Math.Pow(2, Math.Min(e.Attempts, 8))));
                _logger.LogWarning(
                    error,
                    "Farm notification {EventId} for recipient {RecipientId} failed on attempt {Attempt}.",
                    e.Id, e.RecipientId, e.Attempts);
            }
            e.LeaseUntil = null; e.Version = Guid.NewGuid();
            try { await _db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException) { _db.ChangeTracker.Clear(); }
            return true;
        }

        private async Task<string> Deliver(FarmDeliveryEvent e, CancellationToken ct)
        {
            var r = await _db.Set<FarmAdvisoryRecipient>().AsNoTracking().Include(x => x.Advisory).Include(x => x.Orchard)
                .SingleAsync(x => x.Id == e.RecipientId, ct);
            var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == r.FarmerId, ct);
            if (user == null || r.Orchard.UserId != r.FarmerId || !await _access.CanAccessAsync(user))
                return "suppressed-inactive-recipient";
            if (e.Kind == "advisory-published" && r.Advisory.Status == "cancelled")
                return "superseded-by-cancellation";
            var title = e.Kind == "advisory-cancelled"
                ? "Advisory cancelled"
                : string.IsNullOrWhiteSpace(r.Advisory.Title) ? "New orchard advisory" : r.Advisory.Title.Trim();
            // Recommendations stay in the authenticated API, not on a device lock screen.
            var message = e.Kind == "advisory-cancelled" ? "An orchard advisory has been cancelled." : "An orchard advisory is ready for review.";
            var data = new AlertNotificationData { Title = title, Message = message, AlertType = e.Kind, TargetApp = "customer" };
            data["eventId"] = e.Id.ToString(); data["advisoryId"] = r.AdvisoryId.ToString();
            data["recipientId"] = r.Id.ToString(); data["orchardProfileId"] = r.OrchardProfileId.ToString(CultureInfo.InvariantCulture);
            data["advisoryKind"] = r.Advisory.Kind ?? "advisory";
            data["schemaVersion"] = "1";
            var notification = await _db.Set<UserNotification>().SingleOrDefaultAsync(x => x.Id == e.Id, ct);
            NotificationDetail detail;
            if (notification == null)
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(ct);
                detail = new NotificationDetail
                {
                    NotificationName = "Farm.Advisory",
                    Data = data.ToJsonString(),
                    DataTypeName = typeof(AlertNotificationData).AssemblyQualifiedName,
                    CreatedOn = e.CreatedOn,
                    Severity = NotificationSeverity.Info
                };
                _db.Add(detail);
                try
                {
                    await _db.SaveChangesAsync(ct);
                    notification = new UserNotification(e.Id) { UserId = r.FarmerId, CreatedOn = e.CreatedOn, NotificationDetailId = detail.Id };
                    _db.Add(notification);
                    await _db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    _db.Entry(detail).State = EntityState.Detached;
                    if (notification != null) _db.Entry(notification).State = EntityState.Detached;
                    throw;
                }
            }
            else detail = await _db.Set<NotificationDetail>().SingleAsync(x => x.Id == notification.NotificationDetailId, ct);

            var dto = notification.ToUserNotificationDto(detail.ToNotificationDetailDto());
            foreach (var notifier in _notifiers.OfType<SignalRRealTimeNotifier>())
                await notifier.SendNotificationsAsync(new[] { dto });

            if (string.Equals(await _settings.GetSettingValueForUserAsync(r.FarmerId, SettingDefinitions.Names.ReceiveNotifications), "false", StringComparison.OrdinalIgnoreCase))
                return "inbox-only-preference";
            if (string.Equals(_config["Firebase:Enabled"], "false", StringComparison.OrdinalIgnoreCase)) return "inbox-only-push-disabled";
            if (!_firebase.IsConfigured) throw new InvalidOperationException("Firebase is not configured.");
            var tokens = await _devices.GetTokensForUsersAsync(new[] { r.FarmerId });
            if (tokens.Count == 0) return "inbox-only-no-devices";
            var payload = data.Properties.Where(x => x.Value != null).ToDictionary(x => x.Key, x => Convert.ToString(x.Value, CultureInfo.InvariantCulture));
            payload["user_notification_id"] = e.Id.ToString();
            payload["alert_type"] = e.Kind;
            payload["target_app"] = "customer";
            payload["advisory_id"] = r.AdvisoryId.ToString();
            payload["recipient_id"] = r.Id.ToString();
            payload["orchard_profile_id"] = r.OrchardProfileId.ToString(CultureInfo.InvariantCulture);
            payload["advisory_kind"] = r.Advisory.Kind ?? "advisory";
            payload["schema_version"] = "1";
            var result = await _firebase.SendMulticastAsync(tokens, title, message, payload);
            if (result.FailureCount > 0 || !result.Success) throw new InvalidOperationException("Push delivery did not succeed for every device.");
            _logger.LogInformation(
                "Farm FCM push sent for event {EventId} to {SuccessCount} device(s) for farmer {FarmerId}.",
                e.Id, result.SuccessCount, r.FarmerId);
            return "inbox-and-push";
        }
    }

    public class FarmAdvisoryDeliveryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FarmAdvisoryDeliveryWorker> _logger;
        public FarmAdvisoryDeliveryWorker(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<FarmAdvisoryDeliveryWorker> logger)
        { _scopes = scopes; _configuration = configuration; _logger = logger; }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_configuration.GetValue<bool>("FarmWorkflow:DeliveryEnabled"))
                    {
                        for (var i = 0; i < 25; i++)
                        {
                            using var scope = _scopes.CreateScope();
                            if (!await scope.ServiceProvider.GetRequiredService<FarmAdvisoryDelivery>().ProcessNext(stoppingToken)) break;
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception error) { _logger.LogWarning("Farm advisory delivery paused: {ErrorType}", error.GetType().Name); }
                try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            }
        }
    }
}
