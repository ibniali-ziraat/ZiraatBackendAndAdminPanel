using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using Xunit;

namespace ECommerce.Module.Core.Tests.Services
{
    public class FarmOperationTests
    {
        private static async Task<(FarmWorkflowTests.Fixture f, FarmOperationService service, FarmAdvisoryRecipient recipient)> Ready(string kind)
        {
            var f = new FarmWorkflowTests.Fixture();
            var draft = new AdvisoryDraftRequest { Kind = kind, Title = kind + " advice", Recommendation = "Reviewed advice", OrchardProfileIds = new long[] { 10 },
                WindowStart = DateTimeOffset.UtcNow, WindowEnd = DateTimeOffset.UtcNow.AddDays(3) };
            var saved = await f.Service.SaveDraft(FarmWorkflowTests.Principal(99, "admin"), null, draft, "draft-" + kind, default);
            await f.Service.Publish(FarmWorkflowTests.Principal(99, "admin"), saved.Id, new() { Version = saved.Version }, "publish-" + kind, default);
            var recipient = f.Db.Set<FarmAdvisoryRecipient>().Single();
            await f.Service.Respond(FarmWorkflowTests.Principal(1), recipient.Id, new(), "ack-" + kind, true, default);
            return (f, new FarmOperationService(f.Db, f.Access.Object), recipient);
        }
        private static MeasuredValueRequest Trees(decimal value = 50) => new() { Value = value, Unit = "trees" };

        [Fact]
        public async Task SprayNormalizesLiquidProductWithoutInferringItFromDose()
        {
            var x = await Ready("spray"); using var f = x.f;
            await x.service.Create(FarmWorkflowTests.Principal(1), x.recipient.Id, "spray", new SprayOperationRequest
            { OperationDate = DateTime.UtcNow.Date, Coverage = Trees(), ProductUsed = "Product", TargetPestDisease = "Scab",
                DoseApplied = new() { Value = 2.5m, Unit = "ml/L" }, TotalMixtureVolume = new() { Value = 200, Unit = "L" },
                ActualProductQuantity = new() { Value = .5m, Unit = "L" }, Cost = 100, Currency = "INR" }, "s1", default);
            var op = Assert.Single(f.Db.Set<FarmOperation>()); Assert.Equal(500, op.MaterialQuantity); Assert.Equal("ml", op.MaterialUnit);
            Assert.Equal(200, op.MixtureTotalLitres); Assert.Equal(2.5m, op.DoseValue);
        }

        [Fact]
        public async Task FertigationNormalizesKgAndPerHectareWaterOnce()
        {
            var x = await Ready("fertigation"); using var f = x.f;
            var request = new FertigationOperationRequest { OperationDate = DateTime.UtcNow.Date, Coverage = new() { Value = .2m, Unit = "ha" },
                FertilizerType = "Urea", QuantityApplied = new() { Value = 12000, Unit = "g" }, IrrigationVolumeApplied = new() { Value = 3200, Unit = "L/ha" },
                Cost = 620, Currency = "INR", LabourHours = 2, EquipmentUsed = new[] { "drip_system" } };
            // The fixture orchard is about 0 ha unless area is explicitly populated.
            f.Db.Set<OrchardProfile>().Single(o => o.Id == 10).Kanals = 8; await f.Db.SaveChangesAsync();
            var first = await x.service.Create(FarmWorkflowTests.Principal(1), x.recipient.Id, "fertigation", request, "offline-1", default);
            var replay = await x.service.Create(FarmWorkflowTests.Principal(1), x.recipient.Id, "fertigation", request, "offline-1", default);
            Assert.Equal(first, replay); var op = Assert.Single(f.Db.Set<FarmOperation>());
            Assert.Equal(12, op.MaterialQuantity); Assert.Equal(640, op.IrrigationTotalLitres); Assert.Equal(620, op.Cost);
        }

        [Fact]
        public async Task OperationVersionIsFallbackIdempotencyKeyWhenHeaderIsMissing()
        {
            var x = await Ready("fertigation"); using var f = x.f;
            f.Db.Set<OrchardProfile>().Single(o => o.Id == 10).Kanals = 8; await f.Db.SaveChangesAsync();
            var request = new FertigationOperationRequest { Version = Guid.NewGuid(), OperationDate = DateTime.UtcNow.Date,
                Coverage = new() { Value = .2m, Unit = "ha" }, FertilizerType = "NPK 19:19:19",
                QuantityApplied = new() { Value = 25, Unit = "kg" }, IrrigationVolumeApplied = new() { Value = 5000, Unit = "L" } };

            var first = await x.service.Create(FarmWorkflowTests.Principal(1), x.recipient.Id, "fertigation", request, null, default);
            var replay = await x.service.Create(FarmWorkflowTests.Principal(1), x.recipient.Id, "fertigation", request, null, default);

            Assert.Equal(first, replay);
            Assert.Single(f.Db.Set<FarmOperation>());
        }

        [Fact]
        public async Task PruningPreservesUnknownCostAndExplicitLabourEquipment()
        {
            var x = await Ready("pruning"); using var f = x.f;
            await x.service.Create(FarmWorkflowTests.Principal(1), x.recipient.Id, "pruning", new PruningOperationRequest
            { OperationDate = DateTime.UtcNow.Date, Coverage = Trees(), PruningType = "maintenance", LabourHours = 6, NumberOfLabourers = 3,
                EquipmentUsed = new[] { "secateurs" }, EquipmentHours = new[] { new EquipmentHoursRequest { Equipment = "secateurs", Hours = 4 } } }, "p1", default);
            var op = Assert.Single(f.Db.Set<FarmOperation>()); Assert.Null(op.Cost); Assert.Equal(6, op.LabourHours); Assert.Equal(4, op.EquipmentHours);
        }

        [Fact]
        public async Task HarvestCountsOnlySoldLotsAndNormalizesQuintalPrice()
        {
            var x = await Ready("harvest"); using var f = x.f;
            var result = await x.service.Create(FarmWorkflowTests.Principal(1), x.recipient.Id, "harvest", new HarvestOperationRequest
            { OperationDate = DateTime.UtcNow.Date, Coverage = Trees(100), QuantityHarvested = new() { Value = 10, Unit = "quintal" }, CostOfHarvesting = 8500, Currency = "INR",
                SaleLots = new[] { new HarvestSaleLotRequest { Quantity = new() { Value = 600, Unit = "kg" }, IsSold = true, PriceReceivedPerUnit = new() { Value = 4200, Unit = "per_quintal" }, BuyerOrMandi = "Mandi" },
                    new HarvestSaleLotRequest { Quantity = new() { Value = 400, Unit = "kg" }, IsSold = false } } }, "h1", default);
            Assert.Equal(25200, result.TotalIncome); var op = Assert.Single(f.Db.Set<FarmOperation>()); Assert.Equal(1000, op.HarvestQuantityKg);
            Assert.Equal(2, f.Db.Set<FarmHarvestSaleLot>().Count()); Assert.Null(f.Db.Set<FarmHarvestSaleLot>().Single(l => !l.IsSold).Income);
        }

        [Fact]
        public async Task HarvestRejectsOversoldQuantityAndFutureDate()
        {
            var x = await Ready("harvest"); using var f = x.f;
            var request = new HarvestOperationRequest { OperationDate = DateTime.UtcNow.Date, Coverage = Trees(), QuantityHarvested = new() { Value = 100, Unit = "kg" },
                SaleLots = new[] { new HarvestSaleLotRequest { Quantity = new() { Value = 101, Unit = "kg" }, IsSold = false } } };
            Assert.Equal(400, (await Assert.ThrowsAsync<FarmWorkflowException>(() => x.service.Create(FarmWorkflowTests.Principal(1), x.recipient.Id, "harvest", request, "bad", default))).StatusCode);
            request.OperationDate = DateTime.UtcNow.Date.AddDays(1);
            Assert.Equal(400, (await Assert.ThrowsAsync<FarmWorkflowException>(() => x.service.Create(FarmWorkflowTests.Principal(1), x.recipient.Id, "harvest", request, "future", default))).StatusCode);
        }

        [Fact]
        public async Task OtherOwnerCannotReadCreateOrVoidOperation()
        {
            var x = await Ready("pruning"); using var f = x.f;
            var receipt = await x.service.Create(FarmWorkflowTests.Principal(1), x.recipient.Id, "pruning", new PruningOperationRequest
            { OperationDate = DateTime.UtcNow.Date, Coverage = Trees(), PruningType = "sanitary" }, "p", default);
            Assert.Equal(404, (await Assert.ThrowsAsync<FarmWorkflowException>(() => x.service.Detail(FarmWorkflowTests.Principal(2), 10, receipt.Id, default))).StatusCode);
            Assert.Equal(404, (await Assert.ThrowsAsync<FarmWorkflowException>(() => x.service.Void(FarmWorkflowTests.Principal(2), 10, receipt.Id, new() { Version = receipt.Version, Reason = "No" }, "v", default))).StatusCode);
        }

        [Fact]
        public async Task VoidRemovesOperationFromPostedHistoryWithoutDeletingAudit()
        {
            var x = await Ready("pruning"); using var f = x.f;
            var receipt = await x.service.Create(FarmWorkflowTests.Principal(1), x.recipient.Id, "pruning", new PruningOperationRequest
            { OperationDate = DateTime.UtcNow.Date, Coverage = Trees(), PruningType = "formative", Cost = 0, Currency = "INR" }, "p", default);
            var result = await x.service.Void(FarmWorkflowTests.Principal(1), 10, receipt.Id, new() { Version = receipt.Version, Reason = "Entered in error" }, "void", default);
            Assert.Equal("void", result.Status); Assert.Equal(0, f.Db.Set<FarmOperation>().Single().Cost); Assert.Contains(f.Db.Set<FarmWorkflowAudit>(), a => a.SubjectId == receipt.Id && a.Action == "operation-voided");
        }
    }
}
