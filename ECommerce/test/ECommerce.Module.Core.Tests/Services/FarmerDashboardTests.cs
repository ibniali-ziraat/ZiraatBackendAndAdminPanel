using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Models;
using ECommerce.Module.Core.Areas.Core.Controllers;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Core.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ECommerce.Module.Core.Tests.Services
{
    public partial class FarmerDashboardTests : IDisposable
    {
        private readonly DashboardDb _db = new();
        private readonly Mock<IWeatherService> _weather = new();
        private static readonly DateTimeOffset RecordedOn = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task EmptyDashboardHasNoFabricatedMeasurements()
        {
            var result = await Service().GetDashboard(10, year: 2025);
            Assert.Empty(result.Orchard.GraphicalAnalysis);
            Assert.Null(result.Orchard.SurvivalRatePercent);
            Assert.Null(result.Orchard.AverageTreeAgeYears);
            Assert.Null(result.Orchard.EstimatedProductivity.ProjectedYieldKg);
            Assert.All(result.Financial.Metrics, x => Assert.Null(x.NumericValue));
            Assert.Empty(result.Financial.NetProfitTrend);
            Assert.Empty(result.PestDisease.HighRiskPests);
            Assert.Empty(result.Alerts.RecentTimeline);
            Assert.Null(result.Advisory.EngagementScore.Score);
        }

        [Fact]
        public async Task OrchardAreaAddsAllOwnedOrchardsAndPreservesSmallAreas()
        {
            SeedOrchard(1, 10, 1);
            SeedOrchard(2, 10, 2);
            SeedOrchard(3, 99, 100);
            await _db.SaveChangesAsync();
            var result = await Service().GetOrchard(10, year: 2025);
            Assert.Equal(2, result.TotalOrchards);
            Assert.Equal(3, result.TotalAreaKanals);
            Assert.Equal(0.15m, result.TotalAreaHa);
            Assert.Equal(0.05m, result.GraphicalAnalysis[0].Value);
            Assert.Equal(new long?[] { 1, 2 }, result.GraphicalAnalysis.Select(x => x.OrchardProfileId));
        }

        [Fact]
        public async Task ForeignOrMissingOrchardCannotBeRead()
        {
            SeedOrchard(1, 99);
            await _db.SaveChangesAsync();
            await Assert.ThrowsAsync<KeyNotFoundException>(() => Service().GetDashboard(10, 1, 2025));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => Service().GetOrchard(10, 999, 2025));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => Service().GetWeather(10, 1));
        }

        [Theory]
        [InlineData("kg", 2)]
        [InlineData("Quintals", 200)]
        [InlineData("tonnes", 2000)]
        [InlineData("MT", 2000)]
        public void ProductionUnitsAreConverted(string unit, decimal expected)
        {
            Assert.Equal(expected, FarmerDashboardService.YieldKg(new OrchardProduction { YieldUnit = unit, AverageAnnualYield = 2 }));
        }

        [Theory]
        [InlineData("boxes")]
        [InlineData("")]
        [InlineData(null)]
        public void UnknownProductionUnitsAreNotAssumedToBeKg(string unit)
        {
            Assert.Null(FarmerDashboardService.YieldKg(new OrchardProduction { YieldUnit = unit, AverageAnnualYield = 100 }));
        }

        [Fact]
        public async Task LatestMeasuredSurvivalIsWeightedByAssessedTrees()
        {
            SeedOrchard(1, 10);
            SeedOrchard(2, 10);
            Assessment(1, 100, 80, RecordedOn);
            Assessment(1, 100, 10, RecordedOn.AddDays(-1));
            Assessment(2, 10, 10, RecordedOn);
            await _db.SaveChangesAsync();
            var result = await Service().GetOrchard(10, year: 2025);
            Assert.Equal(81.82m, result.SurvivalRatePercent);
            Assert.Equal(80m, result.Orchards[0].SurvivalRatePercent);
            Assert.Equal(100m, result.Orchards[1].SurvivalRatePercent);
        }

        [Fact]
        public async Task SoilReadingsRemainPerOrchardAndOrganicCarbonIsNotOrganicMatter()
        {
            var first = SeedOrchard(1, 10);
            first.SoilAndWater = new OrchardSoilAndWater { SoilPh = 5, SoilOrganicCarbon = "2%" };
            var second = SeedOrchard(2, 10);
            second.SoilAndWater = new OrchardSoilAndWater { SoilPh = 8 };
            await _db.SaveChangesAsync();
            var result = await Service().GetNutrient(10, year: 2025);
            Assert.Empty(result.Metrics);
            Assert.Equal(5m, Metric(result.Orchards[0].Metrics, "soil-ph"));
            Assert.Equal(8m, Metric(result.Orchards[1].Metrics, "soil-ph"));
            Assert.Null(Metric(result.Orchards[0].Metrics, "organic-matter"));
            Assert.Empty(result.SoilHealthTrend);
        }

        [Fact]
        public async Task FinancialTotalsUseOnlyCompletedOwnedRecordsInPeriod()
        {
            SeedOrchard(1, 10);
            SeedOrchard(2, 99);
            AddActivity(1, "sale", 100, 1500);
            AddActivity(1, "expense", null, 500);
            AddActivity(2, "sale", 100, 999999);
            AddActivity(1, "sale", 100, 9000, date: RecordedOn.AddYears(-1));
            AddActivity(1, "sale", 100, 7000, completed: false);
            await _db.SaveChangesAsync();
            var result = await Service().GetFinancial(10, year: 2025);
            Assert.Equal(1500, Metric(result.Metrics, "total-revenue"));
            Assert.Equal(500, Metric(result.Metrics, "total-cost"));
            Assert.Equal(1000, Metric(result.Metrics, "total-profit"));
            Assert.Equal(200, Metric(result.Metrics, "return-on-investment"));
            Assert.Single(result.NetProfitTrend);
            Assert.Equal(1000, result.NetProfitTrend[0].Value);
        }

        [Fact]
        public async Task MissingExpensesDoNotBecomeZeroCostOrInventProfit()
        {
            SeedOrchard(1, 10);
            AddActivity(1, "sale", 100, 1000);
            await _db.SaveChangesAsync();
            var result = await Service().GetFinancial(10, year: 2025);
            Assert.Null(Metric(result.Metrics, "total-cost"));
            Assert.Null(Metric(result.Metrics, "total-profit"));
            Assert.Null(Metric(result.Metrics, "return-on-investment"));
        }

        [Fact]
        public async Task ZeroAreaAndZeroCostDoNotCauseDivisionByZero()
        {
            SeedOrchard(1, 10, 0);
            AddActivity(1, "sale", 100, 1000);
            AddActivity(1, "expense", null, 0);
            await _db.SaveChangesAsync();
            var result = await Service().GetFinancial(10, year: 2025);
            Assert.Equal(0, Metric(result.Metrics, "total-cost"));
            Assert.Null(Metric(result.Metrics, "cost-of-cultivation"));
            Assert.Null(Metric(result.Metrics, "return-on-investment"));
        }

        [Fact]
        public async Task SprayComplianceUsesDatedScheduledTasksNotProfileText()
        {
            var orchard = SeedOrchard(1, 10);
            orchard.PlantHealth = new OrchardPlantHealth { SpraySchedule = "[\"Neem\",\"Spray B\"]" };
            AddActivity(1, "spray", 250, null, scheduled: RecordedOn);
            AddActivity(1, "spray", 250, null, scheduled: RecordedOn.AddDays(-1), completed: false);
            AddActivity(1, "spray", 250, null);
            await _db.SaveChangesAsync();
            var result = await Service().GetPestDisease(10, year: 2025);
            Assert.Equal(50m, result.SpraySchedule.CompliancePercent);
            Assert.Null(Metric(result.Metrics, "pest-incidence"));
        }

        [Fact]
        public async Task WeatherProviderFailureDoesNotFailHomeOrInventLocation()
        {
            var orchard = SeedOrchard(1, 10);
            orchard.Latitude = 34;
            orchard.Longitude = 74;
            orchard.District = "Recorded district";
            _weather.Setup(x => x.GetCurrentSummaryAsync(34, 74, "Recorded district", null)).ThrowsAsync(new HttpRequestException("provider unavailable"));
            await _db.SaveChangesAsync();
            var result = await Service().GetHome(10, 1, 2025);
            Assert.Equal("unavailable", result.Weather.DataStatus);
            Assert.Equal("Recorded district", result.Weather.Location);
            Assert.Null(result.Weather.TemperatureCelsius);
        }

        [Fact]
        public async Task ActivityWritesAreOwnedAndPersisted()
        {
            SeedOrchard(1, 10);
            SeedOrchard(2, 99);
            await _db.SaveChangesAsync();
            var controller = Controller(10);
            var request = new FarmActivityRequest { Kind = "harvest", OccurredOn = RecordedOn, Quantity = 20 };
            Assert.IsType<NotFoundResult>(await controller.CreateActivity(2, request));
            Assert.Empty(_db.Set<FarmActivity>());
            Assert.IsType<CreatedAtActionResult>(await controller.CreateActivity(1, request));
            var record = Assert.Single(_db.Set<FarmActivity>());
            Assert.Equal(10, record.RecordedById);
            Assert.Equal(1, record.OrchardProfileId);
            Assert.IsType<NotFoundResult>(await Controller(99).UpdateActivity(1, record.Id, request));
            Assert.IsType<NotFoundResult>(await Controller(99).DeleteActivity(1, record.Id));
            var ownRead = await Service().GetOrchard(10, 1, 2025);
            Assert.Equal(20, ownRead.EstimatedProductivity.RecordedHarvestKg);
        }

        [Theory]
        [InlineData("CreateAssessment")]
        [InlineData("UpdateAssessment")]
        [InlineData("DeleteAssessment")]
        public void AssessmentWritesRequireAdminRole(string method)
        {
            var attribute = (AuthorizeAttribute)Attribute.GetCustomAttribute(typeof(FarmerDashboardRecordsController).GetMethod(method), typeof(AuthorizeAttribute));
            Assert.Equal("admin", attribute.Roles);
        }

        [Fact]
        public void InvalidAssessmentCountsAndFutureDatesAreRejected()
        {
            Assert.NotEmpty(Validate(new FarmAssessmentRequest { ObservedOn = RecordedOn, TreesAssessed = 10, LivingTrees = 11 }));
            Assert.NotEmpty(Validate(new FarmAssessmentRequest { ObservedOn = RecordedOn, PestAffectedTrees = 1 }));
            Assert.NotEmpty(Validate(new FarmAssessmentRequest { ObservedOn = DateTimeOffset.UtcNow.AddDays(1), SoilPh = 7 }));
            Assert.NotEmpty(Validate(new FarmAssessmentRequest { ObservedOn = RecordedOn, SoilPh = 15 }));
            Assert.Empty(Validate(new FarmAssessmentRequest { ObservedOn = RecordedOn, TreesAssessed = 10, LivingTrees = 0 }));
        }

        [Fact]
        public void DashboardActionFilterIsNotExposedAsAnotherRoute()
        {
            var method = typeof(FarmerDashboardController).GetMethod(nameof(FarmerDashboardController.OnActionExecutionAsync));
            Assert.NotNull(Attribute.GetCustomAttribute(method, typeof(NonActionAttribute)));
        }

        [Fact]
        public void ActivityValidationPreventsMissingAmountsAndDoubleCounting()
        {
            Assert.NotEmpty(Validate(new FarmActivityRequest { Kind = "sale", OccurredOn = RecordedOn, Quantity = 100 }));
            Assert.NotEmpty(Validate(new FarmActivityRequest { Kind = "fertilizer", OccurredOn = RecordedOn, Quantity = 100, Amount = 10 }));
            Assert.NotEmpty(Validate(new FarmActivityRequest { Kind = "spray", OccurredOn = RecordedOn, IsCompleted = false }));
            Assert.Empty(Validate(new FarmActivityRequest { Kind = "expense", ExpenseCategory = "labor", OccurredOn = RecordedOn, Amount = 0 }));
        }

        private static List<ValidationResult> Validate(object value)
        {
            var errors = new List<ValidationResult>();
            Validator.TryValidateObject(value, new ValidationContext(value), errors, true);
            return errors;
        }

        private OrchardProfile SeedOrchard(long id, long user, decimal kanals = 8)
        {
            var orchard = new OrchardProfile
            {
                UserId = user, Kanals = kanals, OrchardName = "Orchard " + id,
                EstablishmentYear = 2020, CreatedOn = RecordedOn.UtcDateTime,
                Varieties = new List<OrchardVariety> { new() { VarietyName = "Apple", NumberOfTrees = 100 } }
            };
            _db.Entry(orchard).Property(x => x.Id).CurrentValue = id;
            _db.Add(orchard);
            return orchard;
        }

        private void Assessment(long orchard, int trees, int alive, DateTimeOffset date) =>
            _db.Add(new FarmAssessment { OrchardProfileId = orchard, ObservedOn = date, TreesAssessed = trees, LivingTrees = alive });

        private void AddActivity(long orchard, string kind, decimal? quantity, decimal? amount,
            DateTimeOffset? date = null, bool completed = true, DateTimeOffset? scheduled = null) =>
            _db.Add(new FarmActivity { OrchardProfileId = orchard, Kind = kind, Quantity = quantity, Amount = amount,
                OccurredOn = date ?? RecordedOn, IsCompleted = completed, ScheduledFor = scheduled });

        private FarmerDashboardService Service(IEnumerable<IFarmerAdvisoryDataSource> advisorySources = null,
            IEnumerable<IFarmerAlertDataSource> alertSources = null)
        {
            var currency = new Mock<ICurrencyService>();
            currency.Setup(x => x.FormatCurrency(It.IsAny<decimal>())).Returns<decimal>(x => x.ToString());
            return new FarmerDashboardService(Repo<OrchardProfile>(), Repo<FarmActivity>(), Repo<FarmAssessment>(), RepoGuid<FarmOperation>(), RepoGuid<FarmHarvestSaleLot>(), RepoGuid<FarmAdvisoryRecipient>(), currency.Object,
                _weather.Object, advisorySources ?? Array.Empty<IFarmerAdvisoryDataSource>(),
                alertSources ?? Array.Empty<IFarmerAlertDataSource>(), NullLogger<FarmerDashboardService>.Instance);
        }

        private FarmerDashboardRecordsController Controller(long user)
        {
            var controller = new FarmerDashboardRecordsController(Repo<OrchardProfile>(), Repo<FarmActivity>(), Repo<FarmAssessment>());
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.ToString()) }, "test"))
            }};
            return controller;
        }

        private IRepository<T> Repo<T>() where T : class, IEntityWithTypedId<long>
        {
            var mock = new Mock<IRepository<T>>();
            mock.Setup(x => x.Query()).Returns(() => _db.Set<T>());
            mock.Setup(x => x.Add(It.IsAny<T>())).Callback<T>(x => _db.Add(x));
            mock.Setup(x => x.Remove(It.IsAny<T>())).Callback<T>(x => _db.Remove(x));
            mock.Setup(x => x.SaveChangesAsync()).Returns(async () => { await _db.SaveChangesAsync(); });
            return mock.Object;
        }

        private IRepositoryWithTypedId<T, Guid> RepoGuid<T>() where T : class, IEntityWithTypedId<Guid>
        {
            var mock = new Mock<IRepositoryWithTypedId<T, Guid>>();
            mock.Setup(x => x.Query()).Returns(() => _db.Set<T>());
            return mock.Object;
        }

        private static decimal? Metric(IEnumerable<FarmerMetricVm> metrics, string code) => metrics.Single(x => x.Code == code).NumericValue;
        public void Dispose() => _db.Dispose();

        private sealed class DashboardDb : DbContext
        {
            public DashboardDb() : base(new DbContextOptionsBuilder<DashboardDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options) { }
            protected override void OnModelCreating(ModelBuilder builder)
            {
                builder.Entity<OrchardProfile>();
                builder.Entity<FarmActivity>().HasOne(x => x.OrchardProfile).WithMany().HasForeignKey(x => x.OrchardProfileId);
                builder.Entity<FarmAssessment>().HasOne(x => x.OrchardProfile).WithMany().HasForeignKey(x => x.OrchardProfileId);
                new FarmWorkflowModelBuilder().Build(builder);
            }
        }
    }
}
