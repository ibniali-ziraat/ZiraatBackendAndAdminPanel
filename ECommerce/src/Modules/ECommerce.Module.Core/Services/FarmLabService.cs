using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Module.Core.Data;
using ECommerce.Module.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Core.Services
{
    public class FarmLabService
    {
        private static readonly HashSet<string> Analytes = new(StringComparer.OrdinalIgnoreCase)
        { "ph", "ec", "nitrogen", "phosphorus", "potassium", "organic-matter", "organic-carbon" };
        private readonly SimplDbContext _db; private readonly IUserAccessService _access;
        public FarmLabService(SimplDbContext db, IUserAccessService access) { _db = db; _access = access; }
        private async Task<long> Actor(ClaimsPrincipal user, bool admin = false)
        {
            if (user.Identity?.IsAuthenticated != true || !long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var id) || !await _access.CanAccessAsync(user))
                throw new FarmWorkflowException(401, "An active account is required.");
            if (admin && !user.IsInRole("admin")) throw new FarmWorkflowException(403, "Administrator access is required.");
            return id;
        }
        private static void Page(int page, int size) { if (page < 1 || size < 1 || size > 100) throw new FarmWorkflowException(400, "Invalid paging."); }
        private async Task<OrchardProfile> Orchard(long id, long? owner, CancellationToken ct)
        {
            var row = await _db.Set<OrchardProfile>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && (!owner.HasValue || x.UserId == owner), ct);
            return row ?? throw new FarmWorkflowException(404, "Orchard not found.");
        }
        public async Task<LabReceipt> CreateSample(ClaimsPrincipal user, long orchardId, LabSampleRequest request, bool admin, CancellationToken ct)
        {
            var actor = await Actor(user, admin); var orchard = await Orchard(orchardId, admin ? null : actor, ct);
            if (!request.SampledAt.HasValue || request.SampledAt.Value.Date > DateTime.UtcNow.Date) throw new FarmWorkflowException(400, "A non-future sample date is required.");
            var sample = new FarmLabSample { OrchardProfileId = orchard.Id, FarmerId = orchard.UserId ?? 0, SampleType = request.SampleType.ToLowerInvariant(), SampledAt = request.SampledAt.Value.Date, LabName = request.LabName?.Trim(), Status = "requested", CreatedOn = DateTimeOffset.UtcNow, CreatedById = actor };
            _db.Add(sample); Audit(sample.Id, actor, "lab-sample-created", sample.SampleType); await _db.SaveChangesAsync(ct); return new(sample.Id, sample.Status);
        }
        public async Task<LabReceipt> AddReport(ClaimsPrincipal user, Guid sampleId, LabReportRequest request, CancellationToken ct)
        {
            var actor = await Actor(user, true); var sample = await _db.Set<FarmLabSample>().SingleOrDefaultAsync(x => x.Id == sampleId, ct) ?? throw new FarmWorkflowException(404, "Sample not found.");
            if (!request.ReportedAt.HasValue || request.ReportedAt.Value.Date > DateTime.UtcNow.Date || request.ReportedAt.Value.Date < sample.SampledAt.Date) throw new FarmWorkflowException(400, "Report date must be between sample date and today.");
            if (request.Readings.Length == 0 && request.Attachments.Length == 0) throw new FarmWorkflowException(400, "Readings or evidence are required.");
            var codes = request.Readings.Select(x => x.AnalyteCode?.Trim().ToLowerInvariant()).ToList();
            if (codes.Any(x => !Analytes.Contains(x)) || codes.Distinct().Count() != codes.Count || request.Readings.Any(x => string.IsNullOrWhiteSpace(x.Unit))) throw new FarmWorkflowException(400, "Readings contain an unsupported or duplicate analyte.");
            foreach (var file in request.Attachments)
                if (!await _db.Set<BlobFileReference>().AsNoTracking().AnyAsync(x => x.Id == file.BlobFileReferenceId && x.EntityType == "OrchardProfile" && x.EntityId == sample.OrchardProfileId, ct)) throw new FarmWorkflowException(400, "Attachment does not belong to this orchard.");
            var version = (await _db.Set<FarmLabReport>().Where(x => x.SampleId == sampleId).MaxAsync(x => (int?)x.VersionNumber, ct) ?? 0) + 1;
            var prior = await _db.Set<FarmLabReport>().Where(x => x.SampleId == sampleId).OrderByDescending(x => x.VersionNumber).FirstOrDefaultAsync(ct);
            var report = new FarmLabReport { SampleId = sampleId, VersionNumber = version, ReportedAt = request.ReportedAt.Value.Date, Source = request.Source.Trim(), MethodNotes = request.MethodNotes?.Trim(), SupersedesReportId = prior?.Id, CreatedOn = DateTimeOffset.UtcNow, CreatedById = actor };
            _db.Add(report); _db.AddRange(request.Readings.Select((x, i) => new FarmLabReading { Report = report, AnalyteCode = codes[i], Value = x.Value, Unit = x.Unit.Trim(), Method = x.Method?.Trim() }));
            _db.AddRange(request.Attachments.Select(x => new FarmLabAttachment { Report = report, BlobFileReferenceId = x.BlobFileReferenceId, FileName = x.FileName.Trim(), ContentType = x.ContentType, SizeBytes = x.SizeBytes, CreatedOn = DateTimeOffset.UtcNow }));
            sample.Status = "submitted"; Audit(report.Id, actor, "lab-report-created", $"version:{version}"); await _db.SaveChangesAsync(ct); return new(report.Id, report.Status, version);
        }
        public async Task<LabReceipt> Review(ClaimsPrincipal user, Guid reportId, LabReviewRequest request, CancellationToken ct)
        {
            var actor = await Actor(user, true); var report = await _db.Set<FarmLabReport>().Include(x => x.Sample).SingleOrDefaultAsync(x => x.Id == reportId, ct) ?? throw new FarmWorkflowException(404, "Report not found.");
            if (report.Status != "draft") throw new FarmWorkflowException(409, "Reviewed report versions are immutable.");
            if (request.Decision == "approved" && !await _db.Set<FarmLabReading>().AnyAsync(x => x.ReportId == reportId, ct)) throw new FarmWorkflowException(409, "Evidence without structured readings cannot be approved as numeric results.");
            report.Status = request.Decision; report.VerifiedAt = DateTimeOffset.UtcNow; report.VerifiedById = actor; report.ReviewNotes = request.Notes?.Trim(); report.Sample.Status = request.Decision;
            if (request.Decision == "approved" && report.SupersedesReportId.HasValue) { var old = await _db.Set<FarmLabReport>().SingleOrDefaultAsync(x => x.Id == report.SupersedesReportId, ct); if (old?.Status == "approved") old.Status = "superseded"; }
            Audit(report.Id, actor, "lab-report-" + request.Decision, request.Notes); await _db.SaveChangesAsync(ct); return new(report.Id, report.Status, report.VersionNumber);
        }
        public async Task<object> History(ClaimsPrincipal user, long orchardId, string type, int page, int size, bool admin, CancellationToken ct)
        {
            var actor = await Actor(user, admin); await Orchard(orchardId, admin ? null : actor, ct); Page(page, size);
            var q = _db.Set<FarmLabSample>().AsNoTracking().Where(x => x.OrchardProfileId == orchardId); if (!string.IsNullOrWhiteSpace(type)) q = q.Where(x => x.SampleType == type);
            var total = await q.CountAsync(ct); var samples = await q.OrderByDescending(x => x.SampledAt).ThenByDescending(x => x.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct); var ids = samples.Select(x => x.Id).ToList();
            var reports = await _db.Set<FarmLabReport>().AsNoTracking().Where(x => ids.Contains(x.SampleId)).OrderByDescending(x => x.VersionNumber).ToListAsync(ct);
            return new { page, pageSize = size, total, items = samples.Select(s => new { s.Id, s.OrchardProfileId, s.SampleType, s.SampledAt, s.LabName, s.Status, reports = reports.Where(r => r.SampleId == s.Id).Select(r => new { r.Id, r.VersionNumber, r.Status, r.ReportedAt, r.Source, r.VerifiedAt, r.SupersedesReportId }) }) };
        }
        public async Task<object> Detail(ClaimsPrincipal user, Guid reportId, bool admin, CancellationToken ct, long? orchardId = null)
        {
            var actor = await Actor(user, admin); var report = await _db.Set<FarmLabReport>().AsNoTracking().Include(x => x.Sample).SingleOrDefaultAsync(x => x.Id == reportId && (!orchardId.HasValue || x.Sample.OrchardProfileId == orchardId) && (admin || x.Sample.FarmerId == actor), ct) ?? throw new FarmWorkflowException(404, "Report not found.");
            var readings = await _db.Set<FarmLabReading>().AsNoTracking().Where(x => x.ReportId == reportId).OrderBy(x => x.AnalyteCode).ToListAsync(ct); var files = await _db.Set<FarmLabAttachment>().AsNoTracking().Where(x => x.ReportId == reportId).ToListAsync(ct);
            return new { report.Id, report.SampleId, report.VersionNumber, report.Status, report.ReportedAt, report.Source, report.MethodNotes, report.VerifiedAt, report.ReviewNotes, report.SupersedesReportId, readings = readings.Select(x => new { x.AnalyteCode, x.Value, x.Unit, x.Method }), attachments = files.Select(x => new { x.Id, x.FileName, x.ContentType, x.SizeBytes, x.ScanStatus }) };
        }
        public async Task<object> Latest(ClaimsPrincipal user, long orchardId, string type, CancellationToken ct, string crop = null, string variety = null, string growthStage = null)
        {
            var actor = await Actor(user); await Orchard(orchardId, actor, ct); type = type?.ToLowerInvariant(); if (type is not ("soil" or "water")) throw new FarmWorkflowException(400, "Sample type must be soil or water.");
            var rows = await ApprovedReadings(orchardId, type, ct); var rules = await _db.Set<FarmNutrientRangeRule>().AsNoTracking().Where(x => x.Status == "approved").ToListAsync(ct);
            var latest = rows.GroupBy(x => x.reading.AnalyteCode).Select(g => g.OrderByDescending(x => x.sample.SampledAt).ThenByDescending(x => x.report.VersionNumber).First()).Select(x => new { x.reading.AnalyteCode, x.reading.Value, x.reading.Unit, x.sample.SampledAt, x.report.ReportedAt, x.report.VerifiedAt, reportId = x.report.Id, classification = Classify(x.reading, rules, crop, variety, growthStage) }).ToList();
            return new { orchardProfileId = orchardId, sampleType = type, dataStatus = latest.Count == 0 ? "missing" : "verified", stale = (bool?)null, staleReason = "Retest cadence is not configured.", readings = latest };
        }
        public async Task<object> Trends(ClaimsPrincipal user, long orchardId, string type, string analyte, DateTime? from, DateTime? to, CancellationToken ct)
        {
            var actor = await Actor(user); await Orchard(orchardId, actor, ct); var rows = await ApprovedReadings(orchardId, type, ct); analyte = analyte?.Trim().ToLowerInvariant();
            return rows.Where(x => x.reading.AnalyteCode == analyte && (!from.HasValue || x.sample.SampledAt >= from.Value.Date) && (!to.HasValue || x.sample.SampledAt <= to.Value.Date)).OrderBy(x => x.sample.SampledAt).Select(x => new { x.sample.SampledAt, x.report.ReportedAt, x.report.VerifiedAt, reportId = x.report.Id, x.reading.Value, x.reading.Unit }).ToList();
        }
        private async Task<List<(FarmLabSample sample, FarmLabReport report, FarmLabReading reading)>> ApprovedReadings(long orchardId, string type, CancellationToken ct)
        {
            var raw = await (from s in _db.Set<FarmLabSample>().AsNoTracking() join r in _db.Set<FarmLabReport>().AsNoTracking() on s.Id equals r.SampleId join v in _db.Set<FarmLabReading>().AsNoTracking() on r.Id equals v.ReportId where s.OrchardProfileId == orchardId && s.SampleType == type && r.Status == "approved" select new { s, r, v }).ToListAsync(ct);
            return raw.Select(x => (x.s, x.r, x.v)).ToList();
        }
        private static string Classify(FarmLabReading value, List<FarmNutrientRangeRule> rules, string crop, string variety, string growthStage)
        {
            static bool Match(string rule, string requested) => string.IsNullOrWhiteSpace(rule) || (!string.IsNullOrWhiteSpace(requested) && string.Equals(rule, requested, StringComparison.OrdinalIgnoreCase));
            var rule = rules.Where(x => x.AnalyteCode == value.AnalyteCode && x.Unit == value.Unit && Match(x.Crop, crop) && Match(x.Variety, variety) && Match(x.GrowthStage, growthStage))
                .OrderByDescending(x => (string.IsNullOrWhiteSpace(x.Crop) ? 0 : 1) + (string.IsNullOrWhiteSpace(x.Variety) ? 0 : 1) + (string.IsNullOrWhiteSpace(x.GrowthStage) ? 0 : 1)).ThenByDescending(x => x.ApprovedOn).FirstOrDefault(); if (rule == null) return "unclassified";
            if (rule.Minimum.HasValue && value.Value < rule.Minimum) return "low"; if (rule.Maximum.HasValue && value.Value > rule.Maximum) return "high"; return "within-range";
        }
        public async Task<object> Rules(ClaimsPrincipal user, CancellationToken ct) { await Actor(user, true); return await _db.Set<FarmNutrientRangeRule>().AsNoTracking().OrderBy(x => x.AnalyteCode).ThenByDescending(x => x.CreatedOn).ToListAsync(ct); }
        public async Task<LabReceipt> SaveRule(ClaimsPrincipal user, Guid? id, NutrientRangeRuleRequest request, CancellationToken ct)
        {
            var actor = await Actor(user, true); if (!Analytes.Contains(request.AnalyteCode) || (request.Minimum.HasValue && request.Maximum.HasValue && request.Minimum > request.Maximum)) throw new FarmWorkflowException(400, "Invalid analyte or range.");
            FarmNutrientRangeRule rule; if (id.HasValue) { rule = await _db.Set<FarmNutrientRangeRule>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new FarmWorkflowException(404, "Rule not found."); if (rule.Status != "draft" || rule.Version != request.Version) throw new FarmWorkflowException(409, "Only the current draft can be edited."); }
            else { rule = new FarmNutrientRangeRule { CreatedOn = DateTimeOffset.UtcNow, CreatedById = actor }; _db.Add(rule); }
            rule.AnalyteCode = request.AnalyteCode.Trim().ToLowerInvariant(); rule.Crop = request.Crop?.Trim(); rule.Variety = request.Variety?.Trim(); rule.GrowthStage = request.GrowthStage?.Trim(); rule.Unit = request.Unit.Trim(); rule.Minimum = request.Minimum; rule.Maximum = request.Maximum; rule.Evidence = request.Evidence.Trim(); rule.Version = Guid.NewGuid(); await _db.SaveChangesAsync(ct); return new(rule.Id, rule.Status);
        }
        public async Task<LabReceipt> ApproveRule(ClaimsPrincipal user, Guid id, WorkflowVersionRequest request, CancellationToken ct)
        {
            var actor = await Actor(user, true); var rule = await _db.Set<FarmNutrientRangeRule>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new FarmWorkflowException(404, "Rule not found."); if (rule.Status != "draft" || rule.Version != request.Version) throw new FarmWorkflowException(409, "Rule changed or is already reviewed.");
            rule.Status = "approved"; rule.ApprovedById = actor; rule.ApprovedOn = DateTimeOffset.UtcNow; rule.Version = Guid.NewGuid(); Audit(rule.Id, actor, "nutrient-rule-approved", rule.Evidence); await _db.SaveChangesAsync(ct); return new(rule.Id, rule.Status);
        }
        private void Audit(Guid id, long actor, string action, string detail) => _db.Add(new FarmWorkflowAudit { SubjectId = id, ActorId = actor, Action = action, Detail = detail, CreatedOn = DateTimeOffset.UtcNow });
    }
}
