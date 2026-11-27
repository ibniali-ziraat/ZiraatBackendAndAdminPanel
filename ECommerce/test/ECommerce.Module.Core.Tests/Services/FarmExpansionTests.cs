using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using Xunit;

namespace ECommerce.Module.Core.Tests.Services
{
    public class FarmExpansionTests
    {
        private static async Task<(FarmWorkflowTests.Fixture f, FarmExpansionService service)> Ready()
        {
            var f = new FarmWorkflowTests.Fixture(); f.Db.Add(new User { Id = 99, FullName = "Admin" }); var vendor = new Vendor { Name = "Vendor", Slug = "vendor", IsActive = true }; f.Db.Add(vendor); f.Db.Entry(vendor).Property(x => x.Id).CurrentValue = 7; f.Db.Add(new User { Id = 5, FullName = "Vendor user", VendorId = 7 }); await f.Db.SaveChangesAsync(); return (f, new FarmExpansionService(f.Db, f.Access.Object));
        }
        [Fact]
        public async Task CommercialAssociationAloneDoesNotGrantFarmAccess()
        {
            var x = await Ready(); using var f = x.f;
            Assert.Equal(404, (await Assert.ThrowsAsync<FarmWorkflowException>(() => x.service.VendorOverview(FarmWorkflowTests.Principal(5, "vendor"), 10, default))).StatusCode);
        }
        [Fact]
        public async Task RevocationImmediatelyDeniesVendorAndFieldFilteringOmitsCosts()
        {
            var x = await Ready(); using var f = x.f; var admin = FarmWorkflowTests.Principal(99, "admin");
            var grant = await x.service.Grant(admin, new() { VendorId = 7, OrchardProfileId = 10, Permissions = new[] { "operations.read" } }, default);
            f.Db.Add(new FarmOperation { OrchardProfileId = 10, FarmerId = 1, RecordedById = 1, RecipientId = Guid.NewGuid(), Kind = "spray", OperationDate = DateTime.UtcNow.Date, Status = "posted", CoverageValue = 10, CoverageUnit = "trees", Cost = 500, Currency = "INR", CreatedOn = DateTimeOffset.UtcNow }); await f.Db.SaveChangesAsync();
            var json = System.Text.Json.JsonSerializer.Serialize(await x.service.VendorOverview(FarmWorkflowTests.Principal(5, "vendor"), 10, default)); Assert.DoesNotContain("Cost", json); Assert.DoesNotContain("500", json);
            await x.service.Revoke(admin, grant.Id, new() { Version = grant.Version, Reason = "Consent withdrawn" }, default);
            Assert.Equal(404, (await Assert.ThrowsAsync<FarmWorkflowException>(() => x.service.VendorOverview(FarmWorkflowTests.Principal(5, "vendor"), 10, default))).StatusCode);
        }
        [Fact]
        public async Task CasesTrackResponseCompletionAndArticleReadsDeduplicate()
        {
            var x = await Ready(); using var f = x.f; var owner = FarmWorkflowTests.Principal(1); var admin = FarmWorkflowTests.Principal(99, "admin");
            var created = await x.service.CreateCase(owner, 10, new() { Kind = "diagnosis", Title = "Leaf spots" }, default); var responded = await x.service.Transition(admin, created.Id, new() { Version = created.Version, Action = "respond", Response = "Reviewed" }, default); var done = await x.service.Transition(admin, created.Id, new() { Version = responded.Version, Action = "resolve" }, default); Assert.Equal("completed", done.Status);
            var first = await x.service.ArticleRead(owner, new() { ArticleId = "pruning-101" }, default); var replay = await x.service.ArticleRead(owner, new() { ArticleId = "pruning-101" }, default); Assert.Equal(first.Id, replay.Id); Assert.Single(f.Db.Set<FarmArticleRead>());
        }
    }
}
