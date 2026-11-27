using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [ApiController, Area("Core"), Authorize]
    [Route("api/core/farmer-dashboard/orchards/{orchardProfileId:long}")]
    public class FarmerDashboardRecordsController : ControllerBase
    {
        private readonly IRepository<OrchardProfile> _orchards;
        private readonly IRepository<FarmActivity> _activities;
        private readonly IRepository<FarmAssessment> _assessments;

        public FarmerDashboardRecordsController(IRepository<OrchardProfile> orchards, IRepository<FarmActivity> activities, IRepository<FarmAssessment> assessments)
        {
            _orchards = orchards;
            _activities = activities;
            _assessments = assessments;
        }

        [HttpGet("activities")]
        public async Task<IActionResult> Activities(long orchardProfileId, [FromQuery, Range(2000, 2100)] int? year = null, [FromQuery, Range(1, 100000)] int page = 1, [FromQuery, Range(1, 100)] int pageSize = 50)
        {
            if (!TryUser(out var userId)) return Unauthorized();
            if (!await CanAccess(orchardProfileId, userId)) return NotFound();
            var query = _activities.Query().AsNoTracking().Where(x => x.OrchardProfileId == orchardProfileId);
            if (year.HasValue)
            {
                var start = new DateTimeOffset(year.Value, 1, 1, 0, 0, 0, TimeSpan.Zero);
                var end = start.AddYears(1);
                query = query.Where(x => x.OccurredOn >= start && x.OccurredOn < end);
            }
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(x => x.OccurredOn).ThenByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return Ok(new { total, page, pageSize, items });
        }

        [HttpPost("activities")]
        public async Task<IActionResult> CreateActivity(long orchardProfileId, FarmActivityRequest request)
        {
            if (!TryUser(out var userId)) return Unauthorized();
            if (!await CanAccess(orchardProfileId, userId)) return NotFound();
            var record = new FarmActivity { OrchardProfileId = orchardProfileId, RecordedById = userId, CreatedOn = DateTimeOffset.UtcNow };
            Apply(record, request);
            _activities.Add(record);
            await _activities.SaveChangesAsync();
            return CreatedAtAction(nameof(Activities), new { orchardProfileId }, record);
        }

        [HttpPut("activities/{id:long}")]
        public async Task<IActionResult> UpdateActivity(long orchardProfileId, long id, FarmActivityRequest request)
        {
            if (!TryUser(out var userId)) return Unauthorized();
            if (!await CanAccess(orchardProfileId, userId)) return NotFound();
            var record = await _activities.Query().FirstOrDefaultAsync(x => x.Id == id && x.OrchardProfileId == orchardProfileId);
            if (record == null) return NotFound();
            Apply(record, request);
            await _activities.SaveChangesAsync();
            return Ok(record);
        }

        [HttpDelete("activities/{id:long}")]
        public async Task<IActionResult> DeleteActivity(long orchardProfileId, long id)
        {
            if (!TryUser(out var userId)) return Unauthorized();
            if (!await CanAccess(orchardProfileId, userId)) return NotFound();
            var record = await _activities.Query().FirstOrDefaultAsync(x => x.Id == id && x.OrchardProfileId == orchardProfileId);
            if (record == null) return NotFound();
            _activities.Remove(record);
            await _activities.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("assessments")]
        public async Task<IActionResult> Assessments(long orchardProfileId, [FromQuery, Range(1, 100000)] int page = 1, [FromQuery, Range(1, 100)] int pageSize = 50)
        {
            if (!TryUser(out var userId)) return Unauthorized();
            if (!await CanAccess(orchardProfileId, userId)) return NotFound();
            var query = _assessments.Query().AsNoTracking().Where(x => x.OrchardProfileId == orchardProfileId);
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(x => x.ObservedOn).ThenByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return Ok(new { total, page, pageSize, items });
        }

        [Authorize(Roles = "admin"), HttpPost("assessments")]
        public async Task<IActionResult> CreateAssessment(long orchardProfileId, FarmAssessmentRequest request)
        {
            if (!TryUser(out var userId)) return Unauthorized();
            if (!await CanAccess(orchardProfileId, userId)) return NotFound();
            var record = new FarmAssessment { OrchardProfileId = orchardProfileId, RecordedById = userId, CreatedOn = DateTimeOffset.UtcNow };
            Apply(record, request);
            _assessments.Add(record);
            await _assessments.SaveChangesAsync();
            return CreatedAtAction(nameof(Assessments), new { orchardProfileId }, record);
        }

        [Authorize(Roles = "admin"), HttpPut("assessments/{id:long}")]
        public async Task<IActionResult> UpdateAssessment(long orchardProfileId, long id, FarmAssessmentRequest request)
        {
            if (!TryUser(out var userId)) return Unauthorized();
            if (!await CanAccess(orchardProfileId, userId)) return NotFound();
            var record = await _assessments.Query().FirstOrDefaultAsync(x => x.Id == id && x.OrchardProfileId == orchardProfileId);
            if (record == null) return NotFound();
            Apply(record, request);
            await _assessments.SaveChangesAsync();
            return Ok(record);
        }

        [Authorize(Roles = "admin"), HttpDelete("assessments/{id:long}")]
        public async Task<IActionResult> DeleteAssessment(long orchardProfileId, long id)
        {
            if (!TryUser(out var userId)) return Unauthorized();
            if (!await CanAccess(orchardProfileId, userId)) return NotFound();
            var record = await _assessments.Query().FirstOrDefaultAsync(x => x.Id == id && x.OrchardProfileId == orchardProfileId);
            if (record == null) return NotFound();
            _assessments.Remove(record);
            await _assessments.SaveChangesAsync();
            return NoContent();
        }

        private bool TryUser(out long userId) => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId) && userId > 0;

        private Task<bool> CanAccess(long id, long userId)
        {
            var admin = User.IsInRole("admin");
            return _orchards.Query().AnyAsync(x => x.Id == id && (x.UserId == userId || admin));
        }

        private static void Apply(FarmActivity record, FarmActivityRequest request)
        {
            record.Kind = request.Kind;
            record.OccurredOn = request.OccurredOn.ToUniversalTime();
            record.ScheduledFor = request.ScheduledFor?.ToUniversalTime();
            record.IsCompleted = request.IsCompleted;
            record.Quantity = request.Quantity;
            record.Amount = request.Amount;
            record.ExpenseCategory = request.ExpenseCategory;
            record.Notes = request.Notes;
            record.UpdatedOn = DateTimeOffset.UtcNow;
        }

        private static void Apply(FarmAssessment record, FarmAssessmentRequest request)
        {
            record.ObservedOn = request.ObservedOn.ToUniversalTime();
            record.TreesAssessed = request.TreesAssessed;
            record.LivingTrees = request.LivingTrees;
            record.AverageTreeAgeYears = request.AverageTreeAgeYears;
            record.YieldKgPerTree = request.YieldKgPerTree;
            record.YieldKgPerHectare = request.YieldKgPerHectare;
            record.TreesMonitored = request.TreesMonitored;
            record.PestAffectedTrees = request.PestAffectedTrees;
            record.DiseaseAffectedTrees = request.DiseaseAffectedTrees;
            record.RiskLevel = request.RiskLevel;
            record.PesticideResistanceSuspected = request.PesticideResistanceSuspected;
            record.SoilPh = request.SoilPh;
            record.SoilEc = request.SoilEc;
            record.OrganicMatterPercent = request.OrganicMatterPercent;
            record.NitrogenLevel = request.NitrogenLevel;
            record.PhosphorusLevel = request.PhosphorusLevel;
            record.PotassiumLevel = request.PotassiumLevel;
            record.DeficiencySymptomsCount = request.DeficiencySymptomsCount;
            record.Notes = request.Notes;
            record.UpdatedOn = DateTimeOffset.UtcNow;
        }
    }
}
