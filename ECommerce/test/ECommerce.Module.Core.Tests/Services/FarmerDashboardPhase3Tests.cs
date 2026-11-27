using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;
using Xunit;

namespace ECommerce.Module.Core.Tests.Services
{
    public partial class FarmerDashboardTests
    {
        [Fact]
        public async Task TypedOperationsAndLegacyLedgerReconcileExactlyOnce()
        {
            SeedOrchard(1, 10, 8);
            AddActivity(1, "expense", null, 100); AddActivity(1, "sale", 5, 200); AddActivity(1, "harvest", 10, null); AddActivity(1, "labor", 2, null);
            var harvest = new FarmOperation { OrchardProfileId = 1, FarmerId = 10, RecordedById = 10, RecipientId = Guid.NewGuid(), Kind = "harvest", Status = "posted",
                OperationDate = RecordedOn.Date, CoverageValue = 50, CoverageUnit = "trees", HarvestQuantityKg = 20, Cost = 50, Currency = "INR", LabourHours = 3, CreatedOn = RecordedOn, UpdatedOn = RecordedOn };
            var fertigation = new FarmOperation { OrchardProfileId = 1, FarmerId = 10, RecordedById = 10, RecipientId = Guid.NewGuid(), Kind = "fertigation", Status = "posted",
                OperationDate = RecordedOn.Date, CoverageValue = .2m, CoverageUnit = "ha", MaterialQuantity = 12, MaterialUnit = "kg", IrrigationTotalLitres = 640,
                Cost = 25, Currency = "INR", EquipmentHours = 4, CreatedOn = RecordedOn, UpdatedOn = RecordedOn };
            _db.AddRange(harvest, fertigation); _db.Add(new FarmHarvestSaleLot { Operation = harvest, QuantityKg = 10, PricePerKg = 30, Income = 300, IsSold = true, CreatedOn = RecordedOn });
            await _db.SaveChangesAsync();
            var service = Service(); var financial = await service.GetFinancial(10, year: 2025); var resources = await service.GetResources(10, year: 2025); var orchard = await service.GetOrchard(10, year: 2025);
            Assert.Equal(500, Metric(financial.Metrics, "total-revenue")); Assert.Equal(175, Metric(financial.Metrics, "total-cost"));
            Assert.Equal(30, Metric(financial.Metrics, "recorded-harvest")); Assert.Equal(5, Metric(resources.Metrics, "labor-hours"));
            Assert.Equal(4, Metric(resources.Metrics, "equipment-hours")); Assert.Equal(30, orchard.EstimatedProductivity.RecordedHarvestKg);
        }

        [Fact]
        public async Task VoidedAndUnsoldRecordsDoNotCreateFinancialTotals()
        {
            SeedOrchard(1, 10, 8);
            var operation = new FarmOperation { OrchardProfileId = 1, FarmerId = 10, RecordedById = 10, RecipientId = Guid.NewGuid(), Kind = "harvest", Status = "void",
                OperationDate = RecordedOn.Date, CoverageValue = 50, CoverageUnit = "trees", HarvestQuantityKg = 100, Cost = 10, Currency = "INR", CreatedOn = RecordedOn, UpdatedOn = RecordedOn };
            var posted = new FarmOperation { OrchardProfileId = 1, FarmerId = 10, RecordedById = 10, RecipientId = Guid.NewGuid(), Kind = "harvest", Status = "posted",
                OperationDate = RecordedOn.Date, CoverageValue = 50, CoverageUnit = "trees", HarvestQuantityKg = 20, CreatedOn = RecordedOn, UpdatedOn = RecordedOn };
            _db.AddRange(operation, posted); _db.Add(new FarmHarvestSaleLot { Operation = posted, QuantityKg = 20, IsSold = false, CreatedOn = RecordedOn }); await _db.SaveChangesAsync();
            var result = await Service().GetFinancial(10, year: 2025);
            Assert.Null(Metric(result.Metrics, "total-revenue")); Assert.Null(Metric(result.Metrics, "total-cost")); Assert.Equal(20, Metric(result.Metrics, "recorded-harvest"));
        }
    }
}
