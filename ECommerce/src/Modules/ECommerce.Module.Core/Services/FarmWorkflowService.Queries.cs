using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Core.Services
{
    public partial class FarmWorkflowService
    {
        private static void Page(int page, int pageSize)
        {
            if (page < 1 || page > 100000 || pageSize < 1 || pageSize > 100)
                throw new FarmWorkflowException(
                    400,
                    "Page must be positive and pageSize must be 1-100."
                );
        }

        public async Task<object> Orchards(
            ClaimsPrincipal user,
            string search,
            int page,
            int pageSize,
            CancellationToken ct,
            string district = null
        )
        {
            await Actor(user, true);
            Page(page, pageSize);
            var q = _db.Set<OrchardProfile>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(x =>
                    x.OrchardName.Contains(search)
                    || x.District.Contains(search)
                    || x.OrchardId.Contains(search)
                );
            if (!string.IsNullOrWhiteSpace(district))
                q = q.Where(x => x.District == district);
            var districts = await _db.Set<OrchardProfile>()
                .AsNoTracking()
                .Where(x => x.District != null && x.District != "")
                .Select(x => x.District)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct);
            return new
            {
                total = await q.CountAsync(ct),
                page,
                pageSize,
                districts,
                items = await q.OrderBy(x => x.OrchardName)
                    .ThenBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new
                    {
                        x.Id,
                        x.OrchardId,
                        x.OrchardName,
                        x.UserId,
                        ownerName = _db.Set<User>()
                            .Where(u => u.Id == x.UserId)
                            .Select(u => u.FullName)
                            .FirstOrDefault(),
                        village = _db.Set<UserProfileAddress>()
                            .Where(a =>
                                _db.Set<Profile>()
                                    .Any(p => p.Id == a.ProfileId && p.CoreUserId == x.UserId)
                            )
                            .OrderBy(a => a.Id)
                            .Select(a => a.Village)
                            .FirstOrDefault(),
                        district = x.District
                            ?? _db.Set<UserProfileAddress>()
                                .Where(a =>
                                    _db.Set<Profile>()
                                        .Any(p => p.Id == a.ProfileId && p.CoreUserId == x.UserId)
                                )
                                .OrderBy(a => a.Id)
                                .Select(a => a.District)
                                .FirstOrDefault(),
                        tehsil = x.Tehsil
                            ?? _db.Set<UserProfileAddress>()
                                .Where(a =>
                                    _db.Set<Profile>()
                                        .Any(p => p.Id == a.ProfileId && p.CoreUserId == x.UserId)
                                )
                                .OrderBy(a => a.Id)
                                .Select(a => a.Tehsil)
                                .FirstOrDefault(),
                        x.Kanals,
                        x.Marlas,
                        treeCount = x.Varieties.Sum(v => (int?)v.NumberOfTrees) ?? 0,
                        varieties = x.Varieties.Select(v => v.VarietyName).ToArray(),
                    })
                    .ToListAsync(ct),
            };
        }

        public async Task<object> Advisories(
            ClaimsPrincipal user,
            string status,
            int page,
            int pageSize,
            CancellationToken ct
        )
        {
            await Actor(user, true);
            Page(page, pageSize);
            var q = _db.Set<FarmAdvisory>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(x => x.Status == status);
            return new
            {
                total = await q.CountAsync(ct),
                page,
                pageSize,
                items = await q.OrderByDescending(x => x.CreatedOn)
                    .ThenBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new
                    {
                        x.Id,
                        x.Version,
                        x.Title,
                        x.Kind,
                        x.Status,
                        x.WindowStart,
                        x.WindowEnd,
                        x.CreatedOn,
                        recipientCount = _db.Set<FarmAdvisoryRecipient>()
                            .Count(r => r.AdvisoryId == x.Id),
                    })
                    .ToListAsync(ct),
            };
        }

        public async Task<object> Advisory(ClaimsPrincipal user, Guid id, CancellationToken ct)
        {
            await Actor(user, true);
            var a = await _db.Set<FarmAdvisory>()
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id, ct);
            if (a == null)
                throw new FarmWorkflowException(404, "Advisory not found.");
            return new
            {
                a.Id,
                a.Version,
                a.Kind,
                a.Title,
                a.Recommendation,
                a.Crop,
                a.Variety,
                a.GrowthStage,
                a.Status,
                a.WindowStart,
                a.WindowEnd,
                a.CreatedOn,
                a.PublishedOn,
                a.CancelledOn,
                a.CancellationReason,
                orchardProfileIds = JsonSerializer.Deserialize<long[]>(a.TargetOrchardsJson),
            };
        }

        public async Task<object> Preview(ClaimsPrincipal user, Guid id, CancellationToken ct)
        {
            await Actor(user, true);
            var a = await _db.Set<FarmAdvisory>()
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id, ct);
            if (a == null)
                throw new FarmWorkflowException(404, "Advisory not found.");
            var orchards = await Targets(
                JsonSerializer.Deserialize<long[]>(a.TargetOrchardsJson),
                ct
            );
            return new
            {
                a.Version,
                count = orchards.Count,
                items = orchards.Select(x => new
                {
                    x.Id,
                    x.OrchardName,
                    farmerId = x.UserId,
                    x.District,
                }),
            };
        }

        public async Task<object> Recipients(
            ClaimsPrincipal user,
            Guid id,
            int page,
            int pageSize,
            CancellationToken ct
        )
        {
            await Actor(user, true);
            Page(page, pageSize);
            if (!await _db.Set<FarmAdvisory>().AnyAsync(x => x.Id == id, ct))
                throw new FarmWorkflowException(404, "Advisory not found.");
            var q = _db.Set<FarmAdvisoryRecipient>().AsNoTracking().Where(x => x.AdvisoryId == id);
            var now = DateTimeOffset.UtcNow;
            return new
            {
                total = await q.CountAsync(ct),
                page,
                pageSize,
                items = await q.OrderBy(x => x.OrchardProfileId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new
                    {
                        x.Id,
                        x.OrchardProfileId,
                        x.OrchardName,
                        x.FarmerId,
                        x.ReadOn,
                        x.AcknowledgedOn,
                        x.CompletedOn,
                        isOverdue = x.Advisory.Status == "published"
                            && x.Advisory.WindowEnd < now
                            && x.CompletedOn == null,
                    })
                    .ToListAsync(ct),
            };
        }

        public async Task<object> AuditTrail(
            ClaimsPrincipal user,
            Guid id,
            int page,
            int pageSize,
            CancellationToken ct
        )
        {
            await Actor(user, true);
            Page(page, pageSize);
            var q = _db.Set<FarmWorkflowAudit>()
                .AsNoTracking()
                .Where(x =>
                    x.SubjectId == id
                    || _db.Set<FarmAdvisoryRecipient>()
                        .Any(r => r.Id == x.SubjectId && r.AdvisoryId == id)
                );
            return new
            {
                total = await q.CountAsync(ct),
                items = await q.OrderByDescending(x => x.CreatedOn)
                    .ThenBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct),
            };
        }

        public async Task<object> Deliveries(
            ClaimsPrincipal user,
            Guid id,
            int page,
            int pageSize,
            CancellationToken ct
        )
        {
            await Actor(user, true);
            Page(page, pageSize);
            var q = _db.Set<FarmDeliveryEvent>()
                .AsNoTracking()
                .Where(x =>
                    _db.Set<FarmAdvisoryRecipient>()
                        .Any(r => r.Id == x.RecipientId && r.AdvisoryId == id)
                );
            return new
            {
                total = await q.CountAsync(ct),
                items = await q.OrderBy(x => x.CreatedOn)
                    .ThenBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new
                    {
                        eventId = x.Id,
                        x.RecipientId,
                        x.Kind,
                        x.Attempts,
                        x.NextAttemptOn,
                        x.DeliveredOn,
                        x.Outcome,
                        x.LastError,
                    })
                    .ToListAsync(ct),
            };
        }

        private IQueryable<FarmAdvisoryRecipient> Owned(long actor) =>
            _db.Set<FarmAdvisoryRecipient>()
                .Where(x => x.FarmerId == actor && x.Orchard.UserId == actor);

        public async Task<object> Inbox(
            ClaimsPrincipal user,
            long? orchardProfileId,
            int page,
            int pageSize,
            CancellationToken ct
        )
        {
            var actor = await Actor(user);
            Page(page, pageSize);
            if (
                orchardProfileId.HasValue
                && !await _db.Set<OrchardProfile>()
                    .AnyAsync(x => x.Id == orchardProfileId && x.UserId == actor, ct)
            )
                throw new FarmWorkflowException(404, "Orchard not found.");
            var q = Owned(actor).AsNoTracking();
            if (orchardProfileId.HasValue)
                q = q.Where(x => x.OrchardProfileId == orchardProfileId);
            var now = DateTimeOffset.UtcNow;
            return new
            {
                total = await q.CountAsync(ct),
                page,
                pageSize,
                items = await q.OrderByDescending(x => x.Advisory.PublishedOn)
                    .ThenBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new
                    {
                        recipientId = x.Id,
                        x.Version,
                        x.AdvisoryId,
                        x.OrchardProfileId,
                        x.OrchardName,
                        x.Advisory.Title,
                        x.Advisory.Kind,
                        x.Advisory.Status,
                        x.Advisory.WindowStart,
                        x.Advisory.WindowEnd,
                        x.ReadOn,
                        x.AcknowledgedOn,
                        x.CompletedOn,
                        isOverdue = x.Advisory.Status == "published"
                            && x.Advisory.WindowEnd < now
                            && x.CompletedOn == null,
                    })
                    .ToListAsync(ct),
            };
        }

        public async Task<object> MyAdvisory(ClaimsPrincipal user, Guid id, CancellationToken ct)
        {
            var actor = await Actor(user);
            var r = await Owned(actor)
                .AsNoTracking()
                .Include(x => x.Advisory)
                .SingleOrDefaultAsync(x => x.Id == id, ct);
            if (r == null)
                throw new FarmWorkflowException(404, "Advisory recipient not found.");
            var a = r.Advisory;
            return new
            {
                recipientId = r.Id,
                r.Version,
                r.AdvisoryId,
                r.OrchardProfileId,
                r.OrchardName,
                r.ReadOn,
                r.AcknowledgedOn,
                r.CompletedOn,
                a.Title,
                a.Kind,
                a.Recommendation,
                a.Crop,
                a.Variety,
                a.GrowthStage,
                a.Status,
                a.WindowStart,
                a.WindowEnd,
                a.CancellationReason,
                isOverdue = a.Status == "published"
                    && a.WindowEnd < DateTimeOffset.UtcNow
                    && r.CompletedOn == null,
            };
        }

        public async Task<WorkflowReceipt> Respond(
            ClaimsPrincipal user,
            Guid id,
            AdvisoryAcknowledgeRequest request,
            string key,
            bool acknowledge,
            CancellationToken ct
        )
        {
            var actor = await Actor(user);
            Validate(request);
            // Read/acknowledge are monotonic and protected by a unique recipient acknowledgement.
            // Generate a request key when clients such as Swagger do not provide one.
            if (string.IsNullOrWhiteSpace(key))
                key = "server-" + Guid.NewGuid().ToString("N");
            // Authorization is rechecked even for a stored idempotent replay.
            var r = await Owned(actor)
                .Include(x => x.Advisory)
                .SingleOrDefaultAsync(x => x.Id == id, ct);
            if (r == null)
                throw new FarmWorkflowException(404, "Advisory recipient not found.");
            var existingAcknowledgment = acknowledge
                ? await _db.Set<FarmAcknowledgment>()
                    .AsNoTracking()
                    .SingleOrDefaultAsync(x => x.RecipientId == id, ct)
                : null;
            try
            {
                return await Mutate(
                    actor,
                    (acknowledge ? "ack:" : "read:") + id,
                    key,
                    request,
                    () =>
                    {
                        if (existingAcknowledgment != null)
                            return Task.FromResult(
                                new WorkflowReceipt(
                                    id,
                                    r.Version,
                                    "acknowledged",
                                    0,
                                    existingAcknowledgment.OpenOperationForm
                                )
                            );
                        if (acknowledge && r.Advisory.Status != "published")
                            throw new FarmWorkflowException(
                                409,
                                "Cancelled advisory cannot be acknowledged."
                            );
                        var now = DateTimeOffset.UtcNow;
                        var changed = false;
                        if (!r.ReadOn.HasValue)
                        {
                            r.ReadOn = now;
                            changed = true;
                        }
                        if (acknowledge && !r.AcknowledgedOn.HasValue)
                        {
                            r.AcknowledgedOn = now;
                            changed = true;
                            _db.Add(
                                new FarmAcknowledgment
                                {
                                    RecipientId = id,
                                    ActorId = actor,
                                    CreatedOn = now,
                                    OpenOperationForm = request.ActionCompleted,
                                }
                            );
                        }
                        if (changed)
                        {
                            r.Version = Guid.NewGuid();
                            Audit(id, actor, acknowledge ? "acknowledged" : "read");
                        }
                        return Task.FromResult(
                            new WorkflowReceipt(
                                id,
                                r.Version,
                                acknowledge ? "acknowledged" : "read",
                                0,
                                acknowledge && request.ActionCompleted
                            )
                        );
                    },
                    ct
                );
            }
            catch (FarmWorkflowException error) when (acknowledge && error.StatusCode == 409)
            {
                // A simultaneous request may win the unique RecipientId insert. Treat that as the same successful acknowledgement.
                var winner = await _db.Set<FarmAcknowledgment>()
                    .AsNoTracking()
                    .SingleOrDefaultAsync(x => x.RecipientId == id, ct);
                if (winner == null)
                    throw;
                var currentVersion = await Owned(actor)
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => x.Version)
                    .SingleAsync(ct);
                return new WorkflowReceipt(
                    id,
                    currentVersion,
                    "acknowledged",
                    0,
                    winner.OpenOperationForm
                );
            }
        }
    }
}
