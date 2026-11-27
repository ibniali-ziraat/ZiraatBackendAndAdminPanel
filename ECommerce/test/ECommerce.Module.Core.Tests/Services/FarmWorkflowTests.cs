using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Module.Core.Data;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Notifications.Models;
using ECommerce.Module.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ECommerce.Module.Core.Tests.Services
{
    public class FarmWorkflowTests
    {
        public class TestDb : SimplDbContext
        {
            public TestDb(DbContextOptions options)
                : base(options) { }

            protected override void OnModelCreating(ModelBuilder b)
            {
                base.OnModelCreating(b);
                new CoreCustomModelBuilder().Build(b);
                b.Entity<User>()
                    .Ignore(x => x.Roles)
                    .Ignore(x => x.CustomerGroups)
                    .Ignore(x => x.UserAddresses)
                    .Ignore(x => x.DefaultBillingAddress)
                    .Ignore(x => x.DefaultShippingAddress);
                b.Entity<OrchardProfile>()
                    .Ignore(x => x.Locations)
                    .Ignore(x => x.SoilAndWater)
                    .Ignore(x => x.CropNutrition)
                    .Ignore(x => x.PlantHealth)
                    .Ignore(x => x.Infrastructure)
                    .Ignore(x => x.Production)
                    .Ignore(x => x.InputCost)
                    .Ignore(x => x.OptionalFields);
                b.Entity<Profile>();
                b.Entity<UserProfileAddress>();
                b.Entity<OrchardVariety>()
                    .HasOne(x => x.OrchardProfileMain)
                    .WithMany(x => x.Varieties)
                    .HasForeignKey(x => x.OrchardProfileId);
                b.Entity<NotificationDetail>();
                b.Entity<UserNotification>();
                new FarmWorkflowModelBuilder().Build(b);
            }
        }

        public sealed class Fixture : IDisposable
        {
            public TestDb Db { get; }
            public Mock<IUserAccessService> Access { get; } = new();
            public FarmWorkflowService Service { get; }

            public Fixture()
            {
                Db = new TestDb(
                    new DbContextOptionsBuilder()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                        .Options
                );
                Access.Setup(x => x.CanAccessAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(true);
                Access
                    .Setup(x => x.CanAccessAsync(It.IsAny<User>()))
                    .ReturnsAsync((User u) => !u.IsDeleted);
                Db.Users.AddRange(
                    new User { Id = 1, FullName = "Owner" },
                    new User { Id = 2, FullName = "Other" }
                );
                var owned = new OrchardProfile { UserId = 1, OrchardName = "Apple orchard" };
                var other = new OrchardProfile { UserId = 2, OrchardName = "Other orchard" };
                owned.Varieties.Add(
                    new OrchardVariety { VarietyName = "Apple", NumberOfTrees = 100 }
                );
                other.Varieties.Add(
                    new OrchardVariety { VarietyName = "Apple", NumberOfTrees = 100 }
                );
                Db.AddRange(owned, other);
                Db.Entry(owned).Property(x => x.Id).CurrentValue = 10;
                Db.Entry(other).Property(x => x.Id).CurrentValue = 20;
                Db.SaveChanges();
                Service = new FarmWorkflowService(Db, Access.Object);
            }

            public void Dispose() => Db.Dispose();
        }

        public static ClaimsPrincipal Principal(long id, string role = "customer") =>
            new(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                        new Claim(ClaimTypes.Role, role),
                    },
                    "test"
                )
            );

        private static ClaimsPrincipal Admin => Principal(99, "admin");

        private static AdvisoryDraftRequest Draft() =>
            new()
            {
                Kind = "spray",
                Title = "Season advisory",
                Recommendation = "Reviewed recommendation",
                OrchardProfileIds = new long[] { 10 },
                WindowStart = DateTimeOffset.UtcNow,
                WindowEnd = DateTimeOffset.UtcNow.AddDays(3),
            };

        private static async Task<WorkflowReceipt> Published(Fixture f)
        {
            var draft = await f.Service.SaveDraft(Admin, null, Draft(), "create", default);
            return await f.Service.Publish(
                Admin,
                draft.Id,
                new() { Version = draft.Version },
                "publish",
                default
            );
        }

        [Fact]
        public async Task PublishReplayCreatesOneRecipientAndEvent()
        {
            using var f = new Fixture();
            var body = Draft();
            var d = await f.Service.SaveDraft(Admin, null, body, "create", default);
            Assert.Equal(d, await f.Service.SaveDraft(Admin, null, body, "create", default));
            var p = await f.Service.Publish(
                Admin,
                d.Id,
                new() { Version = d.Version },
                "publish",
                default
            );
            Assert.Equal(
                p,
                await f.Service.Publish(
                    Admin,
                    d.Id,
                    new() { Version = d.Version },
                    "publish",
                    default
                )
            );
            Assert.Single(f.Db.Set<FarmAdvisory>());
            Assert.Single(f.Db.Set<FarmAdvisoryRecipient>());
            Assert.Single(f.Db.Set<FarmDeliveryEvent>());
            Assert.Equal(1, f.Db.Set<FarmAdvisoryRecipient>().Single().FarmerId);
        }

        [Fact]
        public async Task ChangedIdempotencyBodyIs409()
        {
            using var f = new Fixture();
            var body = Draft();
            await f.Service.SaveDraft(Admin, null, body, "key", default);
            body.Title = "Changed";
            Assert.Equal(
                409,
                (
                    await Assert.ThrowsAsync<FarmWorkflowException>(
                        () => f.Service.SaveDraft(Admin, null, body, "key", default)
                    )
                ).StatusCode
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("bad key")]
        public async Task IdempotencyKeyRequired(string key)
        {
            using var f = new Fixture();
            Assert.Equal(
                400,
                (
                    await Assert.ThrowsAsync<FarmWorkflowException>(
                        () => f.Service.SaveDraft(Admin, null, Draft(), key, default)
                    )
                ).StatusCode
            );
        }

        [Theory]
        [InlineData("customer")]
        [InlineData("vendor")]
        public async Task NonAdminCannotPublishOrBrowseDirectory(string role)
        {
            using var f = new Fixture();
            var actor = Principal(1, role);
            Assert.Equal(
                403,
                (
                    await Assert.ThrowsAsync<FarmWorkflowException>(
                        () => f.Service.SaveDraft(actor, null, Draft(), "x", default)
                    )
                ).StatusCode
            );
            Assert.Equal(
                403,
                (
                    await Assert.ThrowsAsync<FarmWorkflowException>(
                        () => f.Service.Orchards(actor, null, 1, 25, default)
                    )
                ).StatusCode
            );
        }

        [Fact]
        public async Task InactiveActorCannotUseExistingReceipt()
        {
            using var f = new Fixture();
            var body = Draft();
            await f.Service.SaveDraft(Admin, null, body, "x", default);
            f.Access.Setup(x => x.CanAccessAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(false);
            Assert.Equal(
                401,
                (
                    await Assert.ThrowsAsync<FarmWorkflowException>(
                        () => f.Service.SaveDraft(Admin, null, body, "x", default)
                    )
                ).StatusCode
            );
        }

        [Theory]
        [InlineData(0)]
        [InlineData(999)]
        public async Task InvalidTargetsRejected(long id)
        {
            using var f = new Fixture();
            var body = Draft();
            body.OrchardProfileIds = new[] { id };
            Assert.Equal(
                400,
                (
                    await Assert.ThrowsAsync<FarmWorkflowException>(
                        () => f.Service.SaveDraft(Admin, null, body, "x", default)
                    )
                ).StatusCode
            );
        }

        [Fact]
        public async Task DeletedOwnerPreventsPublication()
        {
            using var f = new Fixture();
            var d = await f.Service.SaveDraft(Admin, null, Draft(), "x", default);
            f.Db.Users.Single(x => x.Id == 1).IsDeleted = true;
            await f.Db.SaveChangesAsync();
            Assert.Equal(
                400,
                (
                    await Assert.ThrowsAsync<FarmWorkflowException>(
                        () =>
                            f.Service.Publish(
                                Admin,
                                d.Id,
                                new() { Version = d.Version },
                                "p",
                                default
                            )
                    )
                ).StatusCode
            );
            Assert.Empty(f.Db.Set<FarmDeliveryEvent>());
        }

        [Fact]
        public async Task StaleDraftAndPublishedEditRejected()
        {
            using var f = new Fixture();
            var d = await Published(f);
            var body = Draft();
            body.Version = d.Version;
            Assert.Equal(
                409,
                (
                    await Assert.ThrowsAsync<FarmWorkflowException>(
                        () => f.Service.SaveDraft(Admin, d.Id, body, "e", default)
                    )
                ).StatusCode
            );
            Assert.Equal(
                409,
                (
                    await Assert.ThrowsAsync<FarmWorkflowException>(
                        () =>
                            f.Service.Cancel(
                                Admin,
                                d.Id,
                                new() { Version = Guid.NewGuid(), Reason = "Changed" },
                                "c",
                                default
                            )
                    )
                ).StatusCode
            );
        }

        [Fact]
        public async Task ReadAndAcknowledgeAreNotCompletion()
        {
            using var f = new Fixture();
            await Published(f);
            var r = f.Db.Set<FarmAdvisoryRecipient>().Single();
            await f.Service.Respond(Principal(1), r.Id, new(), "read", false, default);
            Assert.NotNull(r.ReadOn);
            Assert.Null(r.AcknowledgedOn);
            Assert.Empty(f.Db.Set<FarmAcknowledgment>());
            var body = new AdvisoryAcknowledgeRequest { ActionCompleted = true };
            var ack = await f.Service.Respond(Principal(1), r.Id, body, "ack", true, default);
            Assert.True(ack.OpenOperationForm);
            Assert.NotNull(r.AcknowledgedOn);
            Assert.Null(r.CompletedOn);
            Assert.Empty(f.Db.Set<FarmOperation>());
            Assert.Single(f.Db.Set<FarmAcknowledgment>());
            Assert.Equal(
                ack,
                await f.Service.Respond(Principal(1), r.Id, body, "ack", true, default)
            );
        }

        [Fact]
        public async Task AcknowledgeWithDifferentKeyReturnsOriginalResultWithoutDuplicate()
        {
            using var f = new Fixture();
            await Published(f);
            var r = f.Db.Set<FarmAdvisoryRecipient>().Single();
            var first = await f.Service.Respond(
                Principal(1),
                r.Id,
                new() { ActionCompleted = true },
                "ack-1",
                true,
                default
            );
            var replay = await f.Service.Respond(
                Principal(1),
                r.Id,
                new() { ActionCompleted = false },
                "ack-2",
                true,
                default
            );

            Assert.True(first.OpenOperationForm);
            Assert.True(replay.OpenOperationForm);
            Assert.Single(f.Db.Set<FarmAcknowledgment>());
            Assert.Single(
                f.Db.Set<FarmWorkflowAudit>()
                    .Where(x => x.SubjectId == r.Id && x.Action == "acknowledged")
            );
        }

        [Fact]
        public async Task ReadAndAcknowledgeDoNotRequireIdempotencyHeader()
        {
            using var f = new Fixture();
            await Published(f);
            var r = f.Db.Set<FarmAdvisoryRecipient>().Single();

            await f.Service.Respond(Principal(1), r.Id, new(), null, false, default);
            var result = await f.Service.Respond(
                Principal(1),
                r.Id,
                new() { ActionCompleted = true },
                null,
                true,
                default
            );
            var replay = await f.Service.Respond(
                Principal(1),
                r.Id,
                new() { ActionCompleted = false },
                null,
                true,
                default
            );

            Assert.Equal("acknowledged", result.Status);
            Assert.True(replay.OpenOperationForm);
            Assert.Single(f.Db.Set<FarmAcknowledgment>());
        }

        [Theory]
        [InlineData("customer")]
        [InlineData("vendor")]
        [InlineData("admin")]
        public async Task OtherUserCannotAcknowledgeOrReadAsFarmer(string role)
        {
            using var f = new Fixture();
            await Published(f);
            var r = f.Db.Set<FarmAdvisoryRecipient>().Single();
            Assert.Equal(
                404,
                (
                    await Assert.ThrowsAsync<FarmWorkflowException>(
                        () => f.Service.MyAdvisory(Principal(2, role), r.Id, default)
                    )
                ).StatusCode
            );
            Assert.Equal(
                404,
                (
                    await Assert.ThrowsAsync<FarmWorkflowException>(
                        () => f.Service.Respond(Principal(2, role), r.Id, new(), "x", true, default)
                    )
                ).StatusCode
            );
        }

        [Fact]
        public async Task OwnershipTransferRevokesAccessIncludingReplay()
        {
            using var f = new Fixture();
            await Published(f);
            var r = f.Db.Set<FarmAdvisoryRecipient>().Single();
            var body = new AdvisoryAcknowledgeRequest();
            await f.Service.Respond(Principal(1), r.Id, body, "x", true, default);
            f.Db.Set<OrchardProfile>().Single(x => x.Id == 10).UserId = 2;
            await f.Db.SaveChangesAsync();
            Assert.Equal(
                404,
                (
                    await Assert.ThrowsAsync<FarmWorkflowException>(
                        () => f.Service.Respond(Principal(1), r.Id, body, "x", true, default)
                    )
                ).StatusCode
            );
            Assert.Equal(
                404,
                (
                    await Assert.ThrowsAsync<FarmWorkflowException>(
                        () => f.Service.MyAdvisory(Principal(2), r.Id, default)
                    )
                ).StatusCode
            );
        }

        [Fact]
        public async Task CancellationQueuesDistinctEventAndPreventsAcknowledgment()
        {
            using var f = new Fixture();
            var p = await Published(f);
            var result = await f.Service.Cancel(
                Admin,
                p.Id,
                new() { Version = p.Version, Reason = "Superseded" },
                "c",
                default
            );
            Assert.Equal("cancelled", result.Status);
            Assert.Equal(2, f.Db.Set<FarmDeliveryEvent>().Count());
            var r = f.Db.Set<FarmAdvisoryRecipient>().Single();
            Assert.Equal(
                409,
                (
                    await Assert.ThrowsAsync<FarmWorkflowException>(
                        () => f.Service.Respond(Principal(1), r.Id, new(), "a", true, default)
                    )
                ).StatusCode
            );
        }

        [Fact]
        public async Task ListsPreviewAndPagingUsePersistedData()
        {
            using var f = new Fixture();
            var p = await Published(f);
            f.Db.Set<Profile>()
                .Add(
                    new Profile
                    {
                        Id = 101,
                        CoreUserId = 1,
                        FullName = "Owner",
                    }
                );
            f.Db.Set<UserProfileAddress>()
                .Add(
                    new UserProfileAddress
                    {
                        ProfileId = 101,
                        Village = "Farm Village",
                        Tehsil = "Farm Tehsil",
                        District = "Farm District",
                    }
                );
            await f.Db.SaveChangesAsync();
            using var orchardList = JsonDocument.Parse(
                JsonSerializer.Serialize(await f.Service.Orchards(Admin, "Apple", 1, 25, default))
            );
            var orchard = orchardList.RootElement.GetProperty("items")[0];
            Assert.Equal("Owner", orchard.GetProperty("ownerName").GetString());
            Assert.Equal("Farm Village", orchard.GetProperty("village").GetString());
            Assert.Equal("Farm Tehsil", orchard.GetProperty("tehsil").GetString());
            Assert.Equal("Farm District", orchard.GetProperty("district").GetString());
            Assert.Equal(100, orchard.GetProperty("treeCount").GetInt32());
            Assert.Contains(
                "Season advisory",
                JsonSerializer.Serialize(await f.Service.Inbox(Principal(1), 10, 1, 25, default))
            );
            Assert.Contains(
                "\"total\":0",
                JsonSerializer.Serialize(await f.Service.Inbox(Principal(2), null, 1, 25, default))
            );
            Assert.Contains(
                "Apple orchard",
                JsonSerializer.Serialize(await f.Service.Preview(Admin, p.Id, default))
            );
            Assert.Equal(
                400,
                (
                    await Assert.ThrowsAsync<FarmWorkflowException>(
                        () => f.Service.Orchards(Admin, null, 0, 25, default)
                    )
                ).StatusCode
            );
        }

        [Fact]
        public void ModelEnforcesUniqueRecipientsRequestsAndConcurrency()
        {
            using var f = new Fixture();
            Assert.True(
                f.Db.Model.FindEntityType(typeof(FarmAdvisory))
                    .FindProperty("Version")
                    .IsConcurrencyToken
            );
            Assert.Contains(
                f.Db.Model.FindEntityType(typeof(FarmAdvisoryRecipient)).GetIndexes(),
                x => x.IsUnique
            );
            Assert.Contains(
                f.Db.Model.FindEntityType(typeof(FarmWorkflowRequest)).GetIndexes(),
                x => x.IsUnique
            );
            Assert.Contains(
                f.Db.Model.FindEntityType(typeof(FarmDeliveryEvent)).GetIndexes(),
                x => x.IsUnique
            );
        }
    }
}
