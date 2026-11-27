using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Module.Core.Data;
using ECommerce.Module.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Core.Services
{
    public class FarmExpansionService
    {
        public static readonly string[] AllowedPermissions = { "operations.read", "operations.write", "labs.read", "financial.read", "support.write" };
        private readonly SimplDbContext _db; private readonly IUserAccessService _access;
        public FarmExpansionService(SimplDbContext db, IUserAccessService access) { _db = db; _access = access; }
        private async Task<(long id, User user)> Actor(ClaimsPrincipal principal, bool admin = false)
        {
            if (principal.Identity?.IsAuthenticated != true || !long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) || !await _access.CanAccessAsync(principal)) throw new FarmWorkflowException(401, "An active account is required.");
            if (admin && !principal.IsInRole("admin")) throw new FarmWorkflowException(403, "Administrator access is required.");
            return (id, await _db.Set<User>().AsNoTracking().SingleAsync(x => x.Id == id));
        }
        private void Audit(Guid id, long actor, string action, string detail = null) => _db.Add(new FarmWorkflowAudit { SubjectId = id, ActorId = actor, Action = action, Detail = detail, CreatedOn = DateTimeOffset.UtcNow });
        public async Task<object> VendorOptions(ClaimsPrincipal user, CancellationToken ct)
        {
            await Actor(user, true);
            return await _db.Set<Vendor>()
                .AsNoTracking()
                .Where(x => x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name })
                .ToListAsync(ct);
        }
        public async Task<object> Delegations(ClaimsPrincipal user, long? vendorId, long? orchardId, CancellationToken ct)
        { await Actor(user, true); var q = _db.Set<FarmVendorDelegation>().AsNoTracking(); if (vendorId.HasValue) q = q.Where(x => x.VendorId == vendorId); if (orchardId.HasValue) q = q.Where(x => x.OrchardProfileId == orchardId); return await q.OrderByDescending(x => x.CreatedOn).Select(x => new { x.Id, x.Version, x.VendorId, VendorName = _db.Set<Vendor>().Where(v => v.Id == x.VendorId).Select(v => v.Name).FirstOrDefault(), x.FarmerId, x.OrchardProfileId, OrchardName = _db.Set<OrchardProfile>().Where(o => o.Id == x.OrchardProfileId).Select(o => o.OrchardName).FirstOrDefault(), x.PermissionsJson, x.Status, x.CreatedOn, x.ExpiresOn, x.RevokedOn, x.RevocationReason }).ToListAsync(ct); }
        public async Task<WorkflowReceipt> Grant(ClaimsPrincipal user, VendorDelegationRequest request, CancellationToken ct)
        {
            var actor = await Actor(user, true); var permissions = request.Permissions?.Select(x => x.Trim().ToLowerInvariant()).Distinct().ToArray() ?? Array.Empty<string>();
            if (permissions.Length == 0 || permissions.Any(x => !AllowedPermissions.Contains(x))) throw new FarmWorkflowException(400, "Unsupported delegation permission.");
            var vendor = await _db.Set<Vendor>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.VendorId && x.IsActive && !x.IsDeleted, ct) ?? throw new FarmWorkflowException(404, "Active vendor not found.");
            var orchard = await _db.Set<OrchardProfile>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.OrchardProfileId, ct) ?? throw new FarmWorkflowException(404, "Orchard not found.");
            if (request.ExpiresOn <= DateTimeOffset.UtcNow) throw new FarmWorkflowException(400, "Expiry must be in the future.");
            var prior = await _db.Set<FarmVendorDelegation>().Where(x => x.VendorId == vendor.Id && x.OrchardProfileId == orchard.Id && x.Status == "active").ToListAsync(ct); foreach (var x in prior) { x.Status = "revoked"; x.RevokedOn = DateTimeOffset.UtcNow; x.RevokedById = actor.id; x.RevocationReason = "Replaced by a new grant."; x.Version = Guid.NewGuid(); }
            var row = new FarmVendorDelegation { VendorId = vendor.Id, FarmerId = orchard.UserId ?? 0, OrchardProfileId = orchard.Id, PermissionsJson = JsonSerializer.Serialize(permissions), CreatedOn = DateTimeOffset.UtcNow, CreatedById = actor.id, ExpiresOn = request.ExpiresOn };
            _db.Add(row); Audit(row.Id, actor.id, "delegation-granted", string.Join(',', permissions)); await _db.SaveChangesAsync(ct); return new(row.Id, row.Version, row.Status);
        }
        public async Task<WorkflowReceipt> Revoke(ClaimsPrincipal user, Guid id, RevokeDelegationRequest request, CancellationToken ct)
        {
            var actor = await Actor(user, true); var row = await _db.Set<FarmVendorDelegation>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new FarmWorkflowException(404, "Delegation not found."); if (row.Status != "active" || row.Version != request.Version) throw new FarmWorkflowException(409, "Delegation changed or is inactive."); row.Status = "revoked"; row.RevokedOn = DateTimeOffset.UtcNow; row.RevokedById = actor.id; row.RevocationReason = request.Reason.Trim(); row.Version = Guid.NewGuid(); Audit(row.Id, actor.id, "delegation-revoked", row.RevocationReason); await _db.SaveChangesAsync(ct); return new(row.Id, row.Version, row.Status);
        }
        private async Task<(long actor, FarmVendorDelegation grant)> VendorGrant(ClaimsPrincipal principal, long orchardId, string permission, CancellationToken ct)
        {
            var actor = await Actor(principal); if (!actor.user.VendorId.HasValue) throw new FarmWorkflowException(403, "Vendor account required.");
            var vendorOk = await _db.Set<Vendor>().AsNoTracking().AnyAsync(x => x.Id == actor.user.VendorId && x.IsActive && !x.IsDeleted, ct); if (!vendorOk) throw new FarmWorkflowException(403, "Vendor is inactive.");
            var now = DateTimeOffset.UtcNow; var rows = await _db.Set<FarmVendorDelegation>().AsNoTracking().Where(x => x.VendorId == actor.user.VendorId && x.OrchardProfileId == orchardId && x.Status == "active" && (!x.ExpiresOn.HasValue || x.ExpiresOn > now)).ToListAsync(ct);
            var grant = rows.FirstOrDefault(x => JsonSerializer.Deserialize<string[]>(x.PermissionsJson).Contains(permission)); return grant == null ? throw new FarmWorkflowException(404, "Delegated orchard access not found.") : (actor.id, grant);
        }
        public async Task<object> VendorOverview(ClaimsPrincipal user, long orchardId, CancellationToken ct)
        {
            var x = await VendorGrant(user, orchardId, "operations.read", ct); var operations = await _db.Set<FarmOperation>().AsNoTracking().Where(o => o.OrchardProfileId == orchardId && o.Status == "posted").OrderByDescending(o => o.OperationDate).Take(50).Select(o => new { o.Id, o.Kind, o.OperationDate, o.CoverageValue, o.CoverageUnit, o.MaterialName, o.MaterialQuantity, o.MaterialUnit }).ToListAsync(ct);
            return new { orchardProfileId = orchardId, permissions = JsonSerializer.Deserialize<string[]>(x.grant.PermissionsJson), dataStatus = "delegated", operations };
        }
        public async Task<object> VendorLabs(ClaimsPrincipal user, long orchardId, CancellationToken ct)
        { await VendorGrant(user, orchardId, "labs.read", ct); return await (from s in _db.Set<FarmLabSample>().AsNoTracking() join r in _db.Set<FarmLabReport>().AsNoTracking() on s.Id equals r.SampleId where s.OrchardProfileId == orchardId && r.Status == "approved" orderby s.SampledAt descending select new { r.Id, s.SampleType, s.SampledAt, r.ReportedAt, r.VerifiedAt, r.Source }).Take(50).ToListAsync(ct); }
        public async Task<WorkflowReceipt> CreateCase(ClaimsPrincipal user, long orchardId, FarmCaseRequest request, CancellationToken ct)
        {
            var actor = await Actor(user); long farmerId;
            if (actor.user.VendorId.HasValue) farmerId = (await VendorGrant(user, orchardId, "support.write", ct)).grant.FarmerId;
            else farmerId = (await _db.Set<OrchardProfile>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == orchardId && x.UserId == actor.id, ct) ?? throw new FarmWorkflowException(404, "Orchard not found.")).UserId ?? 0;
            if (request.ScheduledOn < DateTimeOffset.UtcNow) throw new FarmWorkflowException(400, "Scheduled date cannot be in the past.");
            var row = new FarmSupportCase { OrchardProfileId = orchardId, FarmerId = farmerId, Kind = request.Kind, Title = request.Title.Trim(), Detail = request.Detail?.Trim(), ScheduledOn = request.ScheduledOn, CreatedOn = DateTimeOffset.UtcNow, CreatedById = actor.id };
            _db.Add(row); Audit(row.Id, actor.id, "case-created", row.Kind); await _db.SaveChangesAsync(ct); return new(row.Id, row.Version, row.Status);
        }
        public async Task<object> Cases(ClaimsPrincipal user, long? orchardId, string kind, string status, bool admin, CancellationToken ct)
        {
            var actor = await Actor(user, admin); var q = _db.Set<FarmSupportCase>().AsNoTracking(); if (!admin) q = q.Where(x => x.FarmerId == actor.id); if (orchardId.HasValue) q = q.Where(x => x.OrchardProfileId == orchardId); if (!string.IsNullOrWhiteSpace(kind)) q = q.Where(x => x.Kind == kind); if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status); return await q.OrderByDescending(x => x.CreatedOn).Take(100).Select(x => new { x.Id, x.Version, x.OrchardProfileId, OrchardName = _db.Set<OrchardProfile>().Where(o => o.Id == x.OrchardProfileId).Select(o => o.OrchardName).FirstOrDefault(), x.FarmerId, FarmerName = _db.Set<User>().Where(u => u.Id == x.FarmerId).Select(u => u.FullName).FirstOrDefault(), x.Kind, x.Status, x.Title, x.Detail, x.ExpertResponse, x.CreatedOn, x.ScheduledOn, x.FirstResponseOn, x.ResolvedOn, x.CancelledOn, x.CreatedById, x.AssignedToId }).ToListAsync(ct);
        }
        public async Task<WorkflowReceipt> Transition(ClaimsPrincipal user, Guid id, FarmCaseTransitionRequest request, CancellationToken ct)
        {
            var actor = await Actor(user, true); var row = await _db.Set<FarmSupportCase>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new FarmWorkflowException(404, "Case not found."); if (row.Version != request.Version) throw new FarmWorkflowException(409, "Case changed. Reload before retrying."); var now = DateTimeOffset.UtcNow;
            switch (request.Action) { case "respond": if (string.IsNullOrWhiteSpace(request.Response)) throw new FarmWorkflowException(400, "Response is required."); row.ExpertResponse = request.Response.Trim(); row.FirstResponseOn ??= now; row.Status = "in-progress"; break; case "resolve": case "complete": row.Status = "completed"; row.ResolvedOn = now; break; case "reopen": row.Status = "open"; row.ResolvedOn = null; break; case "cancel": row.Status = "cancelled"; row.CancelledOn = now; break; }
            row.AssignedToId = request.AssignedToId ?? row.AssignedToId; row.Version = Guid.NewGuid(); Audit(row.Id, actor.id, "case-" + request.Action, request.Response); await _db.SaveChangesAsync(ct); return new(row.Id, row.Version, row.Status);
        }
        public async Task<WorkflowReceipt> ArticleRead(ClaimsPrincipal user, ArticleReadRequest request, CancellationToken ct)
        { var actor = await Actor(user); var existing = await _db.Set<FarmArticleRead>().AsNoTracking().SingleOrDefaultAsync(x => x.UserId == actor.id && x.ArticleId == request.ArticleId, ct); if (existing != null) return new(existing.Id, Guid.Empty, "recorded"); var row = new FarmArticleRead { UserId = actor.id, ArticleId = request.ArticleId.Trim(), ReadOn = DateTimeOffset.UtcNow }; _db.Add(row); await _db.SaveChangesAsync(ct); return new(row.Id, Guid.Empty, "recorded"); }
        public async Task<object> AutomationRules(ClaimsPrincipal user, CancellationToken ct) { await Actor(user, true); return await _db.Set<FarmAutomationRule>().AsNoTracking().OrderBy(x => x.Kind).ThenByDescending(x => x.CreatedOn).ToListAsync(ct); }
        public async Task<WorkflowReceipt> SaveAutomation(ClaimsPrincipal user, AutomationRuleRequest request, CancellationToken ct)
        { var actor = await Actor(user, true); try { JsonDocument.Parse(request.ConfigurationJson); } catch (JsonException) { throw new FarmWorkflowException(400, "Configuration must be valid JSON."); } var row = new FarmAutomationRule { Kind = request.Kind, ConfigurationJson = request.ConfigurationJson, Evidence = request.Evidence.Trim(), CreatedOn = DateTimeOffset.UtcNow, CreatedById = actor.id }; _db.Add(row); await _db.SaveChangesAsync(ct); return new(row.Id, row.Version, row.Status); }
        public async Task<WorkflowReceipt> ApproveAutomation(ClaimsPrincipal user, Guid id, WorkflowVersionRequest request, CancellationToken ct)
        { var actor = await Actor(user, true); var row = await _db.Set<FarmAutomationRule>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new FarmWorkflowException(404, "Rule not found."); if (row.Status != "draft" || row.Version != request.Version) throw new FarmWorkflowException(409, "Rule changed or is already reviewed."); row.Status = "approved"; row.ApprovedOn = DateTimeOffset.UtcNow; row.ApprovedById = actor.id; row.Version = Guid.NewGuid(); Audit(row.Id, actor.id, "automation-approved", row.Kind); await _db.SaveChangesAsync(ct); return new(row.Id, row.Version, row.Status); }
    }
}
