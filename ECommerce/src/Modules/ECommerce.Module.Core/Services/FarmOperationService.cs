using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class FarmOperationService
    {
        private readonly SimplDbContext _db;
        private readonly IUserAccessService _access;
        public FarmOperationService(SimplDbContext db, IUserAccessService access)
        { _db = db; _access = access; }

        private async Task<long> Actor(ClaimsPrincipal principal, bool admin = false)
        {
            if (principal.Identity?.IsAuthenticated != true || !long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) || id <= 0 || !await _access.CanAccessAsync(principal))
                throw new FarmWorkflowException(401, "An active account is required.");
            if (admin && !principal.IsInRole("admin")) throw new FarmWorkflowException(403, "Administrator access is required.");
            return id;
        }
        private static void ValidateBase(FarmOperationRequest request)
        {
            if (request == null || request.OperationDate == null || request.Coverage == null || request.Coverage.Value <= 0)
                throw new FarmWorkflowException(400, "Operation date and positive coverage are required.");
            request.EquipmentUsed ??= Array.Empty<string>(); request.EquipmentHours ??= Array.Empty<EquipmentHoursRequest>();
            if (request.OperationDate.Value.Date > DateTime.UtcNow.Date) throw new FarmWorkflowException(400, "Operation date cannot be in the future.");
            if (request.Cost.HasValue && !string.Equals(request.Currency, "INR", StringComparison.OrdinalIgnoreCase))
                throw new FarmWorkflowException(400, "Currency must be INR when cost is supplied.");
            if (request.LabourHours < 0 || request.NumberOfLabourers < 1 || request.EquipmentHours.Any(x => x == null || x.Hours <= 0))
                throw new FarmWorkflowException(400, "Labour and equipment values are invalid.");
            if (request.EquipmentUsed.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 64) || request.EquipmentUsed.Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.EquipmentUsed.Length)
                throw new FarmWorkflowException(400, "Equipment values must be unique names.");
        }
        private static decimal QuantityKg(MeasuredValueRequest value)
        {
            if (value == null || value.Value <= 0) throw new FarmWorkflowException(400, "Quantity must be positive.");
            return value.Unit?.Trim().ToLowerInvariant() switch
            { "kg" => value.Value, "g" => value.Value / 1000m, "quintal" => value.Value * 100m, _ => throw new FarmWorkflowException(400, "Quantity unit must be kg, g or quintal.") };
        }
        private static (decimal value, string unit) ProductQuantity(MeasuredValueRequest value)
        {
            if (value == null || value.Value <= 0) throw new FarmWorkflowException(400, "Actual product quantity must be positive.");
            return value.Unit?.Trim().ToLowerInvariant() switch
            { "ml" => (value.Value, "ml"), "l" => (value.Value * 1000m, "ml"), "g" => (value.Value, "g"), "kg" => (value.Value * 1000m, "g"), _ => throw new FarmWorkflowException(400, "Actual product quantity must use ml, L, g or kg.") };
        }
        private static decimal PricePerKg(MeasuredValueRequest value)
        {
            if (value == null || value.Value <= 0) throw new FarmWorkflowException(400, "Price must be positive.");
            return value.Unit?.Trim().ToLowerInvariant() switch
            { "per_kg" or "inr/kg" => value.Value, "per_quintal" or "inr/quintal" => value.Value / 100m, _ => throw new FarmWorkflowException(400, "Price unit must be per_kg or per_quintal.") };
        }
        private static decimal IrrigationLitres(MeasuredValueRequest value, MeasuredValueRequest coverage)
        {
            if (value == null || value.Value <= 0) throw new FarmWorkflowException(400, "Irrigation volume must be positive.");
            return value.Unit?.Trim().ToLowerInvariant() switch
            {
                "l" or "litre" or "litres" => value.Value,
                "m3" => value.Value * 1000m,
                "l/ha" when coverage?.Unit?.Trim().ToLowerInvariant() == "ha" => value.Value * coverage.Value,
                "m3/ha" when coverage?.Unit?.Trim().ToLowerInvariant() == "ha" => value.Value * 1000m * coverage.Value,
                _ => throw new FarmWorkflowException(400, "Irrigation unit must be L, m3, L/ha or m3/ha; per-hectare units require ha coverage.")
            };
        }
        private static decimal AreaHa(OrchardProfile orchard) => (orchard.Kanals + orchard.Marlas / 20m) * 0.0505857m;
        private static void ValidateCoverage(MeasuredValueRequest coverage, OrchardProfile orchard)
        {
            var unit = coverage.Unit?.Trim().ToLowerInvariant();
            if (unit == "ha" && coverage.Value > AreaHa(orchard) + 0.0001m) throw new FarmWorkflowException(400, "Coverage exceeds orchard area.");
            if (unit == "trees" && coverage.Value > orchard.Varieties.Sum(x => x.NumberOfTrees)) throw new FarmWorkflowException(400, "Coverage exceeds orchard tree count.");
            if (unit != "ha" && unit != "trees") throw new FarmWorkflowException(400, "Coverage unit must be ha or trees.");
        }
        private void Audit(Guid id, long actor, string action, string detail = null) => _db.Add(new FarmWorkflowAudit
        { SubjectId = id, ActorId = actor, Action = action, Detail = detail, CreatedOn = DateTimeOffset.UtcNow });

        public async Task<OperationReceipt> Create(ClaimsPrincipal user, Guid recipientId, string kind, FarmOperationRequest request, string key, CancellationToken ct)
        {
            var actor = await Actor(user);
            if (request is HarvestOperationRequest harvest && harvest.CostOfHarvesting.HasValue) request.Cost = harvest.CostOfHarvesting;
            ValidateBase(request);
            if (string.IsNullOrWhiteSpace(key) && request.Version != Guid.Empty)
                key = "version-" + request.Version.ToString("N");
            var recipient = await _db.Set<FarmAdvisoryRecipient>().Include(x => x.Advisory).Include(x => x.Orchard).ThenInclude(x => x.Varieties)
                .SingleOrDefaultAsync(x => x.Id == recipientId && x.FarmerId == actor && x.Orchard.UserId == actor, ct);
            if (recipient == null) throw new FarmWorkflowException(404, "Advisory recipient not found.");
            if (recipient.Advisory.Status != "published" || recipient.AcknowledgedOn == null) throw new FarmWorkflowException(409, "A published advisory must be acknowledged first.");
            if (!string.Equals(recipient.Advisory.Kind, kind, StringComparison.OrdinalIgnoreCase)) throw new FarmWorkflowException(409, "Operation type does not match the advisory.");
            ValidateCoverage(request.Coverage, recipient.Orchard);
            return await Mutate(actor, "operation:create:" + recipientId + ":" + kind, key, request, () =>
            {
                var now = DateTimeOffset.UtcNow;
                var op = new FarmOperation { RecipientId = recipientId, OrchardProfileId = recipient.OrchardProfileId, FarmerId = actor, RecordedById = actor,
                    Kind = kind, OperationDate = request.OperationDate.Value.Date, CoverageValue = request.Coverage.Value,
                    CoverageUnit = request.Coverage.Unit.Trim().ToLowerInvariant(), LabourHours = request.LabourHours,
                    NumberOfLabourers = request.NumberOfLabourers, EquipmentJson = JsonSerializer.Serialize(request.EquipmentUsed),
                    EquipmentHours = request.EquipmentHours.Sum(x => x.Hours), Cost = request.Cost,
                    Currency = request.Cost.HasValue ? "INR" : null, Notes = request.Notes?.Trim(), CreatedOn = now, UpdatedOn = now, CompletedOn = now };
                decimal? income = null;
                if (request is SprayOperationRequest s)
                {
                    if (string.IsNullOrWhiteSpace(s.ProductUsed) || string.IsNullOrWhiteSpace(s.TargetPestDisease) || s.DoseApplied == null || s.DoseApplied.Value <= 0)
                        throw new FarmWorkflowException(400, "Product, target and positive dose are required.");
                    op.MaterialName = s.ProductUsed.Trim(); op.Target = s.TargetPestDisease.Trim(); op.DoseValue = s.DoseApplied.Value; op.DoseUnit = s.DoseApplied.Unit?.Trim();
                    if (s.ActualProductQuantity != null) { var normalized = ProductQuantity(s.ActualProductQuantity); op.MaterialQuantity = normalized.value; op.MaterialUnit = normalized.unit; }
                    if (s.TotalMixtureVolume != null) op.MixtureTotalLitres = IrrigationLitres(s.TotalMixtureVolume, null);
                }
                else if (request is FertigationOperationRequest f)
                {
                    if (f.QuantityApplied == null || f.QuantityApplied.Value <= 0 || string.IsNullOrWhiteSpace(f.FertilizerType)) throw new FarmWorkflowException(400, "Fertilizer and positive quantity are required.");
                    op.MaterialName = f.FertilizerType.Trim(); op.MaterialQuantity = QuantityKg(f.QuantityApplied); op.MaterialUnit = "kg";
                    op.IrrigationTotalLitres = IrrigationLitres(f.IrrigationVolumeApplied, f.Coverage);
                }
                else if (request is PruningOperationRequest p)
                {
                    var type = p.PruningType?.Trim().ToLowerInvariant();
                    if (type is not ("maintenance" or "formative" or "rejuvenation" or "sanitary")) throw new FarmWorkflowException(400, "Invalid pruning type.");
                    op.PruningType = type;
                }
                else if (request is HarvestOperationRequest h)
                {
                    h.SaleLots ??= Array.Empty<HarvestSaleLotRequest>(); op.HarvestQuantityKg = QuantityKg(h.QuantityHarvested);
                    var lots = new List<FarmHarvestSaleLot>(); decimal soldOrAllocated = 0, total = 0;
                    foreach (var item in h.SaleLots)
                    {
                        var kg = QuantityKg(item.Quantity); soldOrAllocated += kg;
                        decimal? price = null, lotIncome = null;
                        if (item.IsSold)
                        {
                            if (item.PriceReceivedPerUnit == null || item.PriceReceivedPerUnit.Value <= 0) throw new FarmWorkflowException(400, "Sold lots require a positive price.");
                            price = PricePerKg(item.PriceReceivedPerUnit); lotIncome = decimal.Round(kg * price.Value, 2, MidpointRounding.AwayFromZero); total += lotIncome.Value;
                        }
                        else if (item.PriceReceivedPerUnit != null) throw new FarmWorkflowException(400, "Unsold lots cannot have a received price.");
                        lots.Add(new FarmHarvestSaleLot { Operation = op, QuantityKg = kg, PricePerKg = price, Income = lotIncome, IsSold = item.IsSold, BuyerOrMandi = item.BuyerOrMandi?.Trim(), CreatedOn = now });
                    }
                    if (soldOrAllocated > op.HarvestQuantityKg + 0.0001m) throw new FarmWorkflowException(400, "Sale lot quantities exceed harvested quantity.");
                    _db.AddRange(lots); income = total;
                }
                else throw new FarmWorkflowException(400, "Supported operation types are spray, fertigation, pruning and harvest.");
                _db.Add(op); recipient.CompletedOn = now; recipient.Version = Guid.NewGuid();
                Audit(op.Id, actor, "operation-created", kind);
                return Task.FromResult(new OperationReceipt(op.Id, op.Version, op.Status, op.Kind, income));
            }, ct);
        }

        private async Task<OperationReceipt> Mutate(long actor, string scope, string key, object body, Func<Task<OperationReceipt>> change, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 100 || key.Any(c => c < 33 || c > 126)) throw new FarmWorkflowException(400, "A valid Idempotency-Key is required.");
            var json = JsonSerializer.Serialize(body); var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)));
            var prior = await _db.Set<FarmWorkflowRequest>().AsNoTracking().SingleOrDefaultAsync(x => x.ActorId == actor && x.Scope == scope && x.Key == key, ct);
            if (prior != null) { if (prior.BodyHash != hash) throw new FarmWorkflowException(409, "Idempotency key was used with a different request."); return JsonSerializer.Deserialize<OperationReceipt>(prior.ResponseJson); }
            var result = await change();
            _db.Add(new FarmWorkflowRequest { ActorId = actor, Scope = scope, Key = key, BodyHash = hash, ResponseJson = JsonSerializer.Serialize(result), CreatedOn = DateTimeOffset.UtcNow });
            try { await _db.SaveChangesAsync(ct); }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                prior = await _db.Set<FarmWorkflowRequest>().AsNoTracking().SingleOrDefaultAsync(x => x.ActorId == actor && x.Scope == scope && x.Key == key, ct);
                if (prior != null && prior.BodyHash == hash) return JsonSerializer.Deserialize<OperationReceipt>(prior.ResponseJson);
                throw new FarmWorkflowException(409, "Concurrent or duplicate operation. Reload before retrying.");
            }
            return result;
        }

        public async Task<object> ListOwned(ClaimsPrincipal user, long orchardId, int? year, int page, int pageSize, CancellationToken ct)
        {
            var actor = await Actor(user); if (page < 1 || pageSize < 1 || pageSize > 100) throw new FarmWorkflowException(400, "Invalid paging.");
            if (!await _db.Set<OrchardProfile>().AnyAsync(x => x.Id == orchardId && x.UserId == actor, ct)) throw new FarmWorkflowException(404, "Orchard not found.");
            var q = _db.Set<FarmOperation>().AsNoTracking().Where(x => x.OrchardProfileId == orchardId && x.FarmerId == actor);
            if (year.HasValue) { if (year < 2000 || year > 2100) throw new FarmWorkflowException(400, "Invalid year."); q = q.Where(x => x.OperationDate.Year == year); }
            return await Page(q, page, pageSize, ct);
        }
        public async Task<object> ListAdmin(ClaimsPrincipal user, long? orchardId, string kind, string status, int page, int pageSize, CancellationToken ct)
        {
            await Actor(user, true); if (page < 1 || pageSize < 1 || pageSize > 100) throw new FarmWorkflowException(400, "Invalid paging.");
            var q = _db.Set<FarmOperation>().AsNoTracking();
            if (orchardId.HasValue) q = q.Where(x => x.OrchardProfileId == orchardId);
            if (!string.IsNullOrWhiteSpace(kind)) q = q.Where(x => x.Kind == kind);
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
            return await Page(q, page, pageSize, ct);
        }
        private async Task<object> Page(IQueryable<FarmOperation> q, int page, int pageSize, CancellationToken ct) => new
        {
            total = await q.CountAsync(ct), page, pageSize,
            items = await q.OrderByDescending(x => x.OperationDate).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new { x.Id, x.Version, x.OrchardProfileId, OrchardName = _db.Set<OrchardProfile>().Where(o => o.Id == x.OrchardProfileId).Select(o => o.OrchardName).FirstOrDefault(), x.Kind, x.Status, x.OperationDate, x.CoverageValue, x.CoverageUnit,
                    x.LabourHours, x.EquipmentHours, x.Cost, x.Currency, x.MaterialName, x.MaterialQuantity, x.MaterialUnit,
                    x.IrrigationTotalLitres, x.PruningType, x.HarvestQuantityKg, totalIncome = _db.Set<FarmHarvestSaleLot>().Where(l => l.OperationId == x.Id && l.IsSold).Sum(l => l.Income) }).ToListAsync(ct)
        };
        public async Task<object> Detail(ClaimsPrincipal user, long orchardId, Guid id, CancellationToken ct, bool admin = false)
        {
            var actor = await Actor(user, admin);
            var q = _db.Set<FarmOperation>().AsNoTracking().Where(x => x.Id == id && x.OrchardProfileId == orchardId);
            if (!admin) q = q.Where(x => x.FarmerId == actor && _db.Set<OrchardProfile>().Any(o => o.Id == orchardId && o.UserId == actor));
            var op = await q.SingleOrDefaultAsync(ct); if (op == null) throw new FarmWorkflowException(404, "Operation not found.");
            var lots = await _db.Set<FarmHarvestSaleLot>().AsNoTracking().Where(x => x.OperationId == id).OrderBy(x => x.CreatedOn).ToListAsync(ct);
            var orchardName = await _db.Set<OrchardProfile>().AsNoTracking().Where(x => x.Id == orchardId).Select(x => x.OrchardName).SingleAsync(ct);
            return new { operation = op, orchardName, saleLots = lots, totalIncome = lots.Where(x => x.IsSold).Sum(x => x.Income ?? 0) };
        }
        public async Task<OperationReceipt> Void(ClaimsPrincipal user, long orchardId, Guid id, OperationVoidRequest request, string key, CancellationToken ct)
        {
            var actor = await Actor(user); var op = await _db.Set<FarmOperation>().SingleOrDefaultAsync(x => x.Id == id && x.OrchardProfileId == orchardId && x.FarmerId == actor && _db.Set<OrchardProfile>().Any(o => o.Id == orchardId && o.UserId == actor), ct);
            if (op == null) throw new FarmWorkflowException(404, "Operation not found.");
            return await Mutate(actor, "operation:void:" + id, key, request, () =>
            {
                if (request.Version == Guid.Empty || request.Version != op.Version) throw new FarmWorkflowException(409, "The operation changed. Reload before retrying.");
                if (string.IsNullOrWhiteSpace(request.Reason)) throw new FarmWorkflowException(400, "Void reason is required.");
                if (op.Status == "void") throw new FarmWorkflowException(409, "Operation is already void.");
                op.Status = "void"; op.VoidedOn = DateTimeOffset.UtcNow; op.VoidedById = actor; op.VoidReason = request.Reason.Trim(); op.UpdatedOn = DateTimeOffset.UtcNow; op.Version = Guid.NewGuid();
                Audit(id, actor, "operation-voided", op.VoidReason);
                return Task.FromResult(new OperationReceipt(op.Id, op.Version, op.Status, op.Kind));
            }, ct);
        }
    }
}
