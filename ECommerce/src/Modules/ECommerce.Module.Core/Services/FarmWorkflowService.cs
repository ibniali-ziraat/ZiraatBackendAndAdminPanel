using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Module.Core.Data;
using ECommerce.Module.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Core.Services
{
    public partial class FarmWorkflowService
    {
        private readonly SimplDbContext _db;
        private readonly IUserAccessService _access;
        public FarmWorkflowService(SimplDbContext db, IUserAccessService access) { _db = db; _access = access; }

        private async Task<long> Actor(ClaimsPrincipal principal, bool admin = false)
        {
            if (principal.Identity?.IsAuthenticated != true ||
                !long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) ||
                id <= 0 || !await _access.CanAccessAsync(principal))
                throw new FarmWorkflowException(401, "An active account is required.");
            if (admin && !principal.IsInRole("admin")) throw new FarmWorkflowException(403, "Administrator access is required.");
            return id;
        }

        private static void Validate(object request)
        {
            var errors = new List<ValidationResult>();
            if (request == null || !Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
                throw new FarmWorkflowException(400, "Invalid request fields.");
        }
        private static void CheckVersion(Guid expected, Guid current)
        {
            if (expected == Guid.Empty || expected != current) throw new FarmWorkflowException(409, "The record changed. Reload before retrying.");
        }
        private void Audit(Guid id, long actor, string action, string detail = null) => _db.Add(new FarmWorkflowAudit
        { SubjectId = id, ActorId = actor, Action = action, Detail = detail, CreatedOn = DateTimeOffset.UtcNow });

        private async Task<WorkflowReceipt> Mutate(long actor, string scope, string key, object body,
            Func<Task<WorkflowReceipt>> change, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 100 || key.Any(c => c < 33 || c > 126))
                throw new FarmWorkflowException(400, "An Idempotency-Key header of 1-100 printable non-space ASCII characters is required.");
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body))));
            var prior = await _db.Set<FarmWorkflowRequest>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.ActorId == actor && x.Scope == scope && x.Key == key, ct);
            if (prior != null)
            {
                if (prior.BodyHash != hash) throw new FarmWorkflowException(409, "Idempotency key was used with a different request.");
                return JsonSerializer.Deserialize<WorkflowReceipt>(prior.ResponseJson);
            }
            var response = await change();
            _db.Add(new FarmWorkflowRequest { ActorId = actor, Scope = scope, Key = key, BodyHash = hash,
                ResponseJson = JsonSerializer.Serialize(response), CreatedOn = DateTimeOffset.UtcNow });
            // One SaveChanges commits state, audit, idempotency receipt and outbox together.
            try { await _db.SaveChangesAsync(ct); }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                prior = await _db.Set<FarmWorkflowRequest>().AsNoTracking()
                    .SingleOrDefaultAsync(x => x.ActorId == actor && x.Scope == scope && x.Key == key, ct);
                if (prior != null && prior.BodyHash == hash) return JsonSerializer.Deserialize<WorkflowReceipt>(prior.ResponseJson);
                throw new FarmWorkflowException(409, "Concurrent change or invalid reference. Reload before retrying.");
            }
            return response;
        }

        public async Task<WorkflowReceipt> SaveDraft(ClaimsPrincipal user, Guid? id, AdvisoryDraftRequest request, string key, CancellationToken ct)
        {
            var actor = await Actor(user, true); Validate(request);
            return await Mutate(actor, "draft:" + id, key, request, async () =>
            {
                if (request.WindowStart == default || request.WindowEnd <= request.WindowStart || request.WindowEnd <= DateTimeOffset.UtcNow ||
                    request.OrchardProfileIds.Any(x => x <= 0)) throw new FarmWorkflowException(400, "Select valid orchards and a future application window.");
                var targets = request.OrchardProfileIds.Distinct().OrderBy(x => x).ToArray();
                await Targets(targets, ct);
                var a = id.HasValue ? await _db.Set<FarmAdvisory>().SingleOrDefaultAsync(x => x.Id == id, ct) : new FarmAdvisory
                    { CreatedById = actor, CreatedOn = DateTimeOffset.UtcNow };
                if (a == null) throw new FarmWorkflowException(404, "Advisory not found.");
                if (id.HasValue) { CheckVersion(request.Version, a.Version); if (a.Status != "draft") throw new FarmWorkflowException(409, "Published advisories are immutable."); }
                else _db.Add(a);
                a.Kind = request.Kind; a.Title = request.Title.Trim(); a.Recommendation = request.Recommendation.Trim();
                a.Crop = request.Crop; a.Variety = request.Variety; a.GrowthStage = request.GrowthStage;
                a.WindowStart = request.WindowStart.ToUniversalTime(); a.WindowEnd = request.WindowEnd.ToUniversalTime();
                a.TargetOrchardsJson = JsonSerializer.Serialize(targets); a.Version = Guid.NewGuid();
                Audit(a.Id, actor, id.HasValue ? "draft-updated" : "draft-created");
                return new WorkflowReceipt(a.Id, a.Version, a.Status);
            }, ct);
        }

        private async Task<List<OrchardProfile>> Targets(long[] ids, CancellationToken ct)
        {
            var orchards = await _db.Set<OrchardProfile>().AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync(ct);
            if (orchards.Count != ids.Length || orchards.Any(x => !x.UserId.HasValue))
                throw new FarmWorkflowException(400, "Every target orchard must exist and have an owner.");
            foreach (var owner in orchards.Select(x => x.UserId.Value).Distinct())
            {
                var account = await _db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == owner, ct);
                if (account == null || !await _access.CanAccessAsync(account)) throw new FarmWorkflowException(400, "A target orchard has an inactive owner.");
            }
            return orchards;
        }

        public async Task<WorkflowReceipt> Publish(ClaimsPrincipal user, Guid id, WorkflowVersionRequest request, string key, CancellationToken ct)
        {
            var actor = await Actor(user, true); Validate(request);
            return await Mutate(actor, "publish:" + id, key, request, async () =>
            {
                var a = await _db.Set<FarmAdvisory>().SingleOrDefaultAsync(x => x.Id == id, ct);
                if (a == null) throw new FarmWorkflowException(404, "Advisory not found.");
                CheckVersion(request.Version, a.Version);
                if (a.Status != "draft" || a.WindowEnd <= DateTimeOffset.UtcNow) throw new FarmWorkflowException(409, "Only a draft with an open application window can be published.");
                var targets = await Targets(JsonSerializer.Deserialize<long[]>(a.TargetOrchardsJson), ct);
                a.Status = "published"; a.PublishedOn = DateTimeOffset.UtcNow; a.Version = Guid.NewGuid();
                foreach (var orchard in targets)
                {
                    var r = new FarmAdvisoryRecipient { AdvisoryId = a.Id, OrchardProfileId = orchard.Id,
                        FarmerId = orchard.UserId.Value, OrchardName = orchard.OrchardName };
                    _db.Add(r); AddEvent(r.Id, "advisory-published");
                }
                Audit(id, actor, "published", "Recipient count: " + targets.Count);
                return new WorkflowReceipt(id, a.Version, a.Status, targets.Count);
            }, ct);
        }

        private void AddEvent(Guid recipient, string kind) => _db.Add(new FarmDeliveryEvent
        { RecipientId = recipient, Kind = kind, CreatedOn = DateTimeOffset.UtcNow, NextAttemptOn = DateTimeOffset.UtcNow });

        public async Task<WorkflowReceipt> Cancel(ClaimsPrincipal user, Guid id, AdvisoryCancelRequest request, string key, CancellationToken ct)
        {
            var actor = await Actor(user, true); Validate(request);
            return await Mutate(actor, "cancel:" + id, key, request, async () =>
            {
                var a = await _db.Set<FarmAdvisory>().SingleOrDefaultAsync(x => x.Id == id, ct);
                if (a == null) throw new FarmWorkflowException(404, "Advisory not found.");
                CheckVersion(request.Version, a.Version);
                if (a.Status == "cancelled") throw new FarmWorkflowException(409, "Already cancelled.");
                a.Status = "cancelled"; a.CancelledOn = DateTimeOffset.UtcNow; a.CancellationReason = request.Reason.Trim(); a.Version = Guid.NewGuid();
                var recipients = await _db.Set<FarmAdvisoryRecipient>().Where(x => x.AdvisoryId == id).ToListAsync(ct);
                foreach (var r in recipients) { r.Version = Guid.NewGuid(); AddEvent(r.Id, "advisory-cancelled"); }
                Audit(id, actor, "cancelled", a.CancellationReason);
                return new WorkflowReceipt(id, a.Version, a.Status, recipients.Count);
            }, ct);
        }
    }
}
