using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using Xunit;

namespace ECommerce.Module.Core.Tests.Services
{
    public class FarmLabTests
    {
        [Fact]
        public async Task ApprovedVersionIsImmutableAndSupersededHistoryIsRetained()
        {
            using var f = new FarmWorkflowTests.Fixture(); var service = new FarmLabService(f.Db, f.Access.Object); var admin = FarmWorkflowTests.Principal(99, "admin");
            var sample = await service.CreateSample(admin, 10, new LabSampleRequest { SampleType = "soil", SampledAt = DateTime.UtcNow.Date.AddDays(-3), LabName = "Lab" }, true, default);
            var first = await service.AddReport(admin, sample.Id, Report(6.2m), default); await service.Review(admin, first.Id, new() { Decision = "approved" }, default);
            Assert.Equal(409, (await Assert.ThrowsAsync<FarmWorkflowException>(() => service.Review(admin, first.Id, new() { Decision = "approved" }, default))).StatusCode);
            var second = await service.AddReport(admin, sample.Id, Report(6.5m), default); await service.Review(admin, second.Id, new() { Decision = "approved", Notes = "Corrected result" }, default);
            Assert.Equal(2, f.Db.Set<FarmLabReport>().Count()); Assert.Equal("superseded", f.Db.Set<FarmLabReport>().Single(x => x.Id == first.Id).Status); Assert.Equal("approved", f.Db.Set<FarmLabReport>().Single(x => x.Id == second.Id).Status);
        }

        [Fact]
        public async Task LatestUsesSampleDateAndMissingRuleIsUnclassified()
        {
            using var f = new FarmWorkflowTests.Fixture(); var service = new FarmLabService(f.Db, f.Access.Object); var admin = FarmWorkflowTests.Principal(99, "admin");
            var older = await service.CreateSample(admin, 10, new() { SampleType = "soil", SampledAt = DateTime.UtcNow.Date.AddDays(-10) }, true, default);
            var newer = await service.CreateSample(admin, 10, new() { SampleType = "soil", SampledAt = DateTime.UtcNow.Date.AddDays(-2) }, true, default);
            var r1 = await service.AddReport(admin, older.Id, Report(9), default); await service.Review(admin, r1.Id, new() { Decision = "approved" }, default);
            var r2 = await service.AddReport(admin, newer.Id, Report(6.8m), default); await service.Review(admin, r2.Id, new() { Decision = "approved" }, default);
            var json = JsonSerializer.Serialize(await service.Latest(FarmWorkflowTests.Principal(1), 10, "soil", default));
            Assert.Contains("6.8", json); Assert.DoesNotContain("\"Value\":9", json); Assert.Contains("unclassified", json);
        }

        [Fact]
        public async Task EvidenceOnlyReportCannotBecomeAuthoritative()
        {
            using var f = new FarmWorkflowTests.Fixture(); var service = new FarmLabService(f.Db, f.Access.Object); var admin = FarmWorkflowTests.Principal(99, "admin");
            var sample = await service.CreateSample(admin, 10, new() { SampleType = "water", SampledAt = DateTime.UtcNow.Date.AddDays(-1) }, true, default);
            f.Db.Add(new BlobFileReference { BlobName = "farmer/report.pdf", BlobUrl = "private", EntityType = "OrchardProfile", EntityId = 10 }); await f.Db.SaveChangesAsync();
            var blob = f.Db.Set<BlobFileReference>().Single(); var report = await service.AddReport(admin, sample.Id, new() { ReportedAt = DateTime.UtcNow.Date, Source = "upload", Attachments = new[] { new LabAttachmentRequest { BlobFileReferenceId = blob.Id, FileName = "report.pdf", ContentType = "application/pdf", SizeBytes = 100 } } }, default);
            Assert.Equal(409, (await Assert.ThrowsAsync<FarmWorkflowException>(() => service.Review(admin, report.Id, new() { Decision = "approved" }, default))).StatusCode);
        }

        private static LabReportRequest Report(decimal ph) => new() { ReportedAt = DateTime.UtcNow.Date, Source = "accredited-lab", Readings = new[] { new LabReadingRequest { AnalyteCode = "ph", Value = ph, Unit = "pH", Method = "electrode" } } };
    }
}
