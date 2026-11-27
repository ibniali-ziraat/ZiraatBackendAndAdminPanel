using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.Controllers;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Areas.Core.ViewModels.Weather;
using ECommerce.Module.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ECommerce.Module.Core.Tests.Services
{
    public partial class FarmerDashboardTests
    {
        [Fact]
        public void AggregateJsonIncludesInheritedWeatherWithOrchardDetails()
        {
            var result = new FarmerAggregateDashboardVm
            {
                Weather = new FarmerWeatherDashboardVm
                {
                    Orchards = new List<FarmerOrchardWeatherVm>
                    {
                        new()
                        {
                            OrchardProfileId = 123,
                            OrchardName = "Test orchard",
                            Current = new FarmerWeatherSummaryVm { DataStatus = "available", TemperatureCelsius = 20 },
                            Forecast = JObject.FromObject(new { temperature = 21 }),
                            ForecastStatus = "available",
                            AlertsStatus = "available"
                        }
                    }
                }
            };
            var serializer = Newtonsoft.Json.JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            });
            var json = JObject.FromObject(result, serializer);
            Assert.Single(json.Properties().Where(x => x.Name == "weather"));
            var weather = Assert.IsType<JObject>(json["weather"]);
            Assert.IsType<JArray>(weather["historicalMetrics"]);
            var orchard = Assert.Single(Assert.IsType<JArray>(weather["orchards"]));
            Assert.Equal(123, orchard["orchardProfileId"].Value<long>());
            Assert.Equal(20, orchard["current"]["temperatureCelsius"].Value<decimal>());
            Assert.Equal(21, orchard["forecast"]["temperature"].Value<int>());
            Assert.Equal("available", orchard["forecastStatus"].Value<string>());
            Assert.Equal("available", orchard["alertsStatus"].Value<string>());
            Assert.IsType<JArray>(orchard["alerts"]);
        }

        [Fact]
        public async Task AggregateIncludesEveryOwnedOrchardAndRecordedTotalsOnly()
        {
            var first = SeedOrchard(1, 10, 1);
            first.OrchardId = "ORCH-001";
            first.District = "Srinagar";
            first.Tehsil = "North";
            first.Zone = "A";
            first.PinCode = "190001";
            SeedOrchard(2, 10, 2);
            SeedOrchard(3, 99, 100);
            AddActivity(1, "harvest", 200, null);
            AddActivity(2, "harvest", 300, null);
            AddActivity(1, "sale", 200, 1000);
            AddActivity(2, "sale", 300, 2000);
            AddActivity(1, "expense", null, 100);
            AddActivity(2, "expense", null, 400);
            AddActivity(3, "sale", 100, 999999);
            AddActivity(1, "sale", 100, 9000, date: RecordedOn.AddYears(-1));
            AddActivity(2, "sale", 100, 9000, completed: false);
            await _db.SaveChangesAsync();

            var service = Service();
            var result = await service.GetAggregate(10, 2025);
            Assert.Equal("all-owned-orchards", result.Scope);
            Assert.Equal(2025, result.Year);
            Assert.Equal(new long[] { 1, 2 }, result.Orchards.Select(x => x.OrchardProfileId));
            Assert.Equal("ORCH-001", result.Orchards[0].OrchardId);
            Assert.Equal("Srinagar", result.Orchards[0].District);
            Assert.Equal("North", result.Orchards[0].Tehsil);
            Assert.Equal("A", result.Orchards[0].Zone);
            Assert.Equal("190001", result.Orchards[0].PinCode);
            Assert.Equal(2, result.Summary.TotalOrchards);
            Assert.Equal(3, result.Summary.TotalAreaKanals);
            Assert.Equal(0.15m, result.Summary.TotalAreaHa);
            Assert.Equal(200, result.Summary.TotalTrees);
            Assert.Equal(500, result.Summary.RecordedHarvestKg);
            Assert.Equal(3000, result.Summary.TotalRevenue);
            Assert.Equal(500, result.Summary.TotalCost);
            Assert.Equal(2500, result.Summary.TotalProfit);

            var firstFinancial = await service.GetFinancial(10, 1, 2025);
            var secondFinancial = await service.GetFinancial(10, 2, 2025);
            Assert.Equal(result.Summary.TotalRevenue,
                Metric(firstFinancial.Metrics, "total-revenue") + Metric(secondFinancial.Metrics, "total-revenue"));

            var json = JObject.FromObject(result);
            var legacy = JObject.FromObject(await service.GetDashboard(10, year: 2025));
            foreach (var section in new[] { "Home", "Orchard", "Nutrient", "PestDisease", "Resources", "Financial", "Advisory", "Alerts", "Weather" })
                Assert.True(JToken.DeepEquals(legacy[section], json[section]), section);
            Assert.Equal(new long[] { 1, 2 }, result.Nutrient.Orchards.Select(x => x.OrchardProfileId));
            Assert.Equal(new long[] { 1, 2 }, result.Weather.Orchards.Select(x => x.OrchardProfileId));
        }

        [Fact]
        public async Task EmptyAggregateKeepsMissingMeasurementsNull()
        {
            var result = await Service().GetAggregate(10, 2025);
            Assert.Empty(result.Orchards);
            Assert.Empty(result.Weather.Orchards);
            Assert.Equal(0, result.Summary.TotalOrchards);
            Assert.Equal(0, result.Summary.TotalAreaHa);
            Assert.Equal(0, result.Summary.TotalTrees);
            Assert.Null(result.Summary.RecordedHarvestKg);
            Assert.Null(result.Summary.ProjectedYieldKg);
            Assert.Null(result.Summary.TotalRevenue);
            Assert.Null(result.Summary.TotalCost);
            Assert.Null(result.Summary.TotalProfit);
            _weather.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task AggregatePreservesMissingAndExplicitZeroFinancialAmounts()
        {
            SeedOrchard(1, 10);
            AddActivity(1, "sale", 100, 1000);
            await _db.SaveChangesAsync();
            var missing = await Service().GetAggregate(10, 2025);
            Assert.Null(missing.Summary.TotalCost);
            Assert.Null(missing.Summary.TotalProfit);

            AddActivity(1, "expense", null, 0);
            await _db.SaveChangesAsync();
            var recorded = await Service().GetAggregate(10, 2025);
            Assert.Equal(0, recorded.Summary.TotalCost);
            Assert.Equal(1000, recorded.Summary.TotalProfit);
        }

        [Fact]
        public async Task AggregateKeepsWeatherFailuresLocalAndReusesCurrentConditions()
        {
            var orchard = SeedOrchard(1, 10);
            orchard.Latitude = 34;
            orchard.Longitude = 74;
            orchard.District = "Srinagar";
            SeedOrchard(2, 10);
            var foreign = SeedOrchard(3, 99);
            foreign.Latitude = 35;
            foreign.Longitude = 75;
            AddActivity(1, "sale", 100, 1000);
            _weather.Setup(x => x.GetCurrentSummaryAsync(34, 74, "Srinagar", null))
                .ThrowsAsync(new HttpRequestException("unavailable"));
            _weather.Setup(x => x.GetMeteoblueForecastAsync(It.Is<WeatherForecastRequest>(r => r.Lat == 34 && r.Lon == 74),
                It.IsAny<IDictionary<string, string>>())).ThrowsAsync(new HttpRequestException("unavailable"));
            _weather.Setup(x => x.GetWeatherAlertsAsync(34, 74, null)).ThrowsAsync(new HttpRequestException("unavailable"));
            await _db.SaveChangesAsync();

            var result = await Service().GetAggregate(10, 2025);
            Assert.Equal(1000, result.Summary.TotalRevenue);
            Assert.Equal(2, result.Weather.Orchards.Count);
            Assert.Equal(34, result.Orchards[0].Latitude);
            Assert.Equal(74, result.Orchards[0].Longitude);
            Assert.Equal("unavailable", result.Weather.Orchards[0].Current.DataStatus);
            Assert.Equal("unavailable", result.Weather.Orchards[0].ForecastStatus);
            Assert.Equal("unavailable", result.Weather.Orchards[0].AlertsStatus);
            Assert.Equal("missing", result.Weather.Orchards[1].Current.DataStatus);
            Assert.Same(result.Home.Weather, result.Weather.Orchards[0].Current);
            _weather.Verify(x => x.GetCurrentSummaryAsync(34, 74, "Srinagar", null), Times.Once);
            _weather.Verify(x => x.GetMeteoblueForecastAsync(It.Is<WeatherForecastRequest>(r => r.Lat == 34 && r.Lon == 74),
                It.IsAny<IDictionary<string, string>>()), Times.Once);
            _weather.Verify(x => x.GetWeatherAlertsAsync(34, 74, null), Times.Once);
            _weather.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task AggregateIncludesAccountWideAdvisoryAndAlertContributionsOnce()
        {
            var advisory = new Mock<IFarmerAdvisoryDataSource>();
            var alerts = new Mock<IFarmerAlertDataSource>();
            var from = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var to = from.AddYears(1);
            advisory.Setup(x => x.GetAdvisory(10, from, to)).ReturnsAsync(new FarmerAdvisoryDashboardVm
            {
                RecentActivities = new List<FarmerActivityVm> { new() { Title = "Recorded forum activity", OccurredOn = RecordedOn } }
            });
            alerts.Setup(x => x.GetAlerts(10, from, to)).ReturnsAsync(new FarmerAlertsDashboardVm
            {
                RecentTimeline = new List<FarmerAlertTimelineItemVm> { new() { Id = "1", Title = "Stored notification", OccurredOn = RecordedOn } }
            });
            SeedOrchard(1, 10);
            SeedOrchard(2, 10);
            await _db.SaveChangesAsync();
            var result = await Service(new[] { advisory.Object }, new[] { alerts.Object }).GetAggregate(10, 2025);
            Assert.Single(result.Advisory.RecentActivities);
            Assert.Single(result.Alerts.RecentTimeline);
            advisory.Verify(x => x.GetAdvisory(10, from, to), Times.Once);
            alerts.Verify(x => x.GetAlerts(10, from, to), Times.Once);
        }

        [Theory]
        [InlineData(1999)]
        [InlineData(2101)]
        public async Task AggregateRejectsInvalidReportingYears(int year)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Service().GetAggregate(10, year));
        }

        [Fact]
        public async Task AggregateDefaultsToCurrentUtcYear()
        {
            var before = DateTime.UtcNow.Year;
            var result = await Service().GetAggregate(10);
            Assert.InRange(result.Year, before, DateTime.UtcNow.Year);
        }

        [Theory]
        [InlineData(ClaimTypes.NameIdentifier)]
        [InlineData("sub")]
        public async Task AggregateControllerUsesAuthenticatedUser(string claimType)
        {
            var service = new Mock<IFarmerDashboardService>(MockBehavior.Strict);
            var expected = new FarmerAggregateDashboardVm();
            service.Setup(x => x.GetAggregate(10, 2025)).ReturnsAsync(expected);
            var controller = AggregateController(service.Object, claimType, "10");
            var response = Assert.IsType<OkObjectResult>(await controller.GetAggregate(2025));
            Assert.Same(expected, response.Value);
            service.Verify(x => x.GetAggregate(10, 2025), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("invalid")]
        [InlineData("0")]
        [InlineData("-1")]
        public async Task AggregateControllerRejectsMissingOrInvalidUser(string userId)
        {
            var service = new Mock<IFarmerDashboardService>(MockBehavior.Strict);
            var controller = AggregateController(service.Object, ClaimTypes.NameIdentifier, userId);
            Assert.IsType<UnauthorizedObjectResult>(await controller.GetAggregate(2025));
            service.VerifyNoOtherCalls();
        }

        [Fact]
        public void AggregateRouteRequiresAuthenticationAndValidatesYear()
        {
            Assert.NotNull(Attribute.GetCustomAttribute(typeof(FarmerDashboardController), typeof(AuthorizeAttribute)));
            var action = typeof(FarmerDashboardController).GetMethod(nameof(FarmerDashboardController.GetAggregate));
            Assert.Equal("aggregate", ((HttpGetAttribute)Attribute.GetCustomAttribute(action, typeof(HttpGetAttribute))).Template);
            Assert.Null(Attribute.GetCustomAttribute(action, typeof(AllowAnonymousAttribute)));
            var range = (RangeAttribute)Attribute.GetCustomAttribute(action.GetParameters().Single(), typeof(RangeAttribute));
            Assert.False(range.IsValid(1999));
            Assert.True(range.IsValid(2000));
            Assert.True(range.IsValid(2100));
            Assert.False(range.IsValid(2101));
            Assert.True(((ResponseCacheAttribute)Attribute.GetCustomAttribute(action, typeof(ResponseCacheAttribute))).NoStore);
        }

        private static FarmerDashboardController AggregateController(IFarmerDashboardService service, string claimType, string userId)
        {
            var claims = userId == null ? Array.Empty<Claim>() : new[] { new Claim(claimType, userId) };
            return new FarmerDashboardController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) }
                }
            };
        }
    }
}
