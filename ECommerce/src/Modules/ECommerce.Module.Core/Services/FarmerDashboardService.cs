using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Areas.Core.ViewModels.Weather;
using ECommerce.Module.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Module.Core.Services
{
    public partial class FarmerDashboardService : IFarmerDashboardService
    {
        private const decimal KanalToHectare = 0.0505857m;
        private const decimal HectaresPerAcre = 0.40468564224m;
        private readonly IRepository<OrchardProfile> _orchards;
        private readonly IRepository<FarmActivity> _activities;
        private readonly IRepository<FarmAssessment> _assessments;
        private readonly IRepositoryWithTypedId<FarmOperation, Guid> _operations;
        private readonly IRepositoryWithTypedId<FarmHarvestSaleLot, Guid> _saleLots;
        private readonly IRepositoryWithTypedId<FarmAdvisoryRecipient, Guid> _advisoryRecipients;
        private readonly ICurrencyService _currency;
        private readonly IWeatherService _weather;
        private readonly IEnumerable<IFarmerAdvisoryDataSource> _advisorySources;
        private readonly IEnumerable<IFarmerAlertDataSource> _alertSources;
        private readonly ILogger<FarmerDashboardService> _logger;
        private readonly Dictionary<(long, long?, int), DashboardData> _loaded = new();
        private readonly Dictionary<long, FarmerWeatherSummaryVm> _currentWeather = new();

        public FarmerDashboardService(IRepository<OrchardProfile> orchards, IRepository<FarmActivity> activities,
            IRepository<FarmAssessment> assessments, IRepositoryWithTypedId<FarmOperation, Guid> operations,
            IRepositoryWithTypedId<FarmHarvestSaleLot, Guid> saleLots, IRepositoryWithTypedId<FarmAdvisoryRecipient, Guid> advisoryRecipients,
            ICurrencyService currency, IWeatherService weather,
            IEnumerable<IFarmerAdvisoryDataSource> advisorySources, IEnumerable<IFarmerAlertDataSource> alertSources,
            ILogger<FarmerDashboardService> logger)
        {
            _orchards = orchards;
            _activities = activities;
            _assessments = assessments;
            _operations = operations;
            _saleLots = saleLots;
            _advisoryRecipients = advisoryRecipients;
            _currency = currency;
            _weather = weather;
            _advisorySources = advisorySources;
            _alertSources = alertSources;
            _logger = logger;
        }

        public async Task<FarmerDashboardVm> GetDashboard(long userId, long? orchardProfileId = null, int? year = null)
        {
            return new FarmerDashboardVm
            {
                Year = year ?? DateTime.UtcNow.Year,
                GeneratedAt = DateTimeOffset.UtcNow,
                Home = await GetHome(userId, orchardProfileId, year),
                Orchard = await GetOrchard(userId, orchardProfileId, year),
                Nutrient = await GetNutrient(userId, orchardProfileId, year),
                PestDisease = await GetPestDisease(userId, orchardProfileId, year),
                Resources = await GetResources(userId, orchardProfileId, year),
                Financial = await GetFinancial(userId, orchardProfileId, year),
                Advisory = await GetAdvisory(userId, orchardProfileId, year),
                Alerts = await GetAlerts(userId, orchardProfileId, year),
                Weather = await GetWeather(userId, orchardProfileId)
            };
        }

        public async Task<FarmerDashboardHomeVm> GetHome(long userId, long? orchardProfileId = null, int? year = null)
        {
            var data = await GetData(userId, orchardProfileId, year);
            var end = data.AsOf;
            var start = end.AddDays(-7);
            var previous = start.AddDays(-7);
            var first = data.Orchards.FirstOrDefault();
            return new FarmerDashboardHomeVm
            {
                Weather = first == null ? new FarmerWeatherSummaryVm { UnavailableReason = "No orchard registered." } : await CurrentWeather(first),
                QuickActions = new List<FarmerQuickActionVm>
                {
                    new() { Code = "add-orchard", Label = "Add Orchard" },
                    new() { Code = "log-harvest", Label = "Log Harvest" },
                    new() { Code = "log-expense", Label = "Log Expense" },
                    new() { Code = "log-spray", Label = "Log Spray" }
                },
                FarmOverview = new FarmerOverviewVm
                {
                    Period = $"Seven days ending {end:yyyy-MM-dd} (UTC)",
                    ActivityComparison = new List<FarmerActivityComparisonVm>
                    {
                        Activity("Orchards", data.Orchards.Count(x => x.CreatedOn >= start.UtcDateTime && x.CreatedOn < end.UtcDateTime),
                            data.Orchards.Count(x => x.CreatedOn >= previous.UtcDateTime && x.CreatedOn < start.UtcDateTime)),
                        Activity("Harvest", CountActivities(data, "harvest", start, end), CountActivities(data, "harvest", previous, start)),
                        Activity("Spray", CountActivities(data, "spray", start, end), CountActivities(data, "spray", previous, start))
                    }
                },
                Tips = data.Orchards.Where(x => !x.Latitude.HasValue || !x.Longitude.HasValue)
                    .Select(x => new FarmerTipVm { Code = "orchard-location", Title = $"Add coordinates for {x.OrchardName} to enable weather." }).ToList(),
                LegalDisclaimer = "Field assessments and advisories require local expert validation."
            };
        }

        public async Task<FarmerOrchardDashboardVm> GetOrchard(long userId, long? orchardProfileId = null, int? year = null)
        {
            var data = await GetData(userId, orchardProfileId, year);
            var area = data.Orchards.Sum(Area);
            var trees = data.Orchards.Sum(Trees);
            var details = data.Orchards.Select(o =>
            {
                var rows = data.Assessments.Where(x => x.OrchardProfileId == o.Id).ToList();
                var survival = rows.FirstOrDefault(x => x.TreesAssessed.HasValue && x.LivingTrees.HasValue);
                return new FarmerOrchardDetailVm
                {
                    OrchardProfileId = o.Id, OrchardName = o.OrchardName, AreaHa = Round(Area(o)),
                    EstablishmentYear = o.EstablishmentYear, TotalTrees = Trees(o),
                    PlantDensityTreesPerHa = Ratio(Trees(o), Area(o)),
                    SurvivalRatePercent = Ratio(survival?.LivingTrees, survival?.TreesAssessed, 100),
                    AverageTreeAgeYears = rows.FirstOrDefault(x => x.AverageTreeAgeYears.HasValue)?.AverageTreeAgeYears,
                    YieldKgPerTree = rows.FirstOrDefault(x => x.YieldKgPerTree.HasValue)?.YieldKgPerTree,
                    YieldKgPerHectare = rows.FirstOrDefault(x => x.YieldKgPerHectare.HasValue)?.YieldKgPerHectare,
                    LatestAssessmentOn = rows.FirstOrDefault()?.ObservedOn,
                    Varieties = o.Varieties.Select(v => new FarmerVarietyVm
                    {
                        Name = v.VarietyName, Trees = v.NumberOfTrees, TreesLessThan5Years = v.TreesLessThan5Years,
                        TreesFiveToTenYears = v.TreesFiveToTenYears, TreesMoreThan10Years = v.TreesMoreThan10Years
                    }).ToList()
                };
            }).ToList();
            var survivalRows = Latest(data, x => x.TreesAssessed.HasValue && x.LivingTrees.HasValue);
            var projected = data.Orchards.Select(x => YieldKg(x.Production)).ToList();
            var completeProjection = projected.Count > 0 && projected.All(x => x.HasValue) ? projected.Sum() : null;
            var harvest = Add(Sum(data.Current, "harvest", x => x.Quantity), SumOperations(data.CurrentOperations, "harvest", x => x.HarvestQuantityKg));
            return new FarmerOrchardDashboardVm
            {
                TotalOrchards = data.Orchards.Count, TotalAreaHa = Round(area),
                TotalAreaKanals = Round(data.Orchards.Sum(x => x.Kanals + x.Marlas / 20)),
                TotalAverageAreaHa = data.Orchards.Count > 0 ? Round(area / data.Orchards.Count) : 0,
                TotalAverageAreaText = $"{Round(area):0.##} ha total",
                TotalTrees = trees, PlantDensityTreesPerHa = Ratio(trees, area),
                SurvivalRatePercent = survivalRows.Count > 0 ? Ratio(survivalRows.Sum(x => x.LivingTrees), survivalRows.Sum(x => x.TreesAssessed), 100) : null,
                AverageTreeAgeYears = Weighted(details, x => x.AverageTreeAgeYears, x => x.TotalTrees),
                AssessmentScope = "Latest recorded values per orchard as of the selected year; survival covers assessed trees only. See per-orchard coverage and dates.",
                Orchards = details,
                EstimatedProductivity = new FarmerProductivityVm
                {
                    ProjectedYieldKg = completeProjection,
                    DataStatus = completeProjection.HasValue ? "recorded-profile-estimate" : "missing-or-unsupported-yield-unit",
                    EstimatedYieldKgPerTree = Weighted(details, x => x.YieldKgPerTree, x => x.TotalTrees),
                    YieldKgPerHectare = Weighted(details, x => x.YieldKgPerHectare, x => x.AreaHa),
                    RecordedHarvestKg = harvest
                },
                GraphicalAnalysis = details.Select(x => new FarmerChartPointVm
                {
                    OrchardProfileId = x.OrchardProfileId, Label = x.OrchardName, Series = "area-ha", Value = x.AreaHa
                }).ToList(),
                ActionPlan = data.Orchards.Where(x => x.SoilAndWater?.LastSoilTestDate == null)
                    .Select(x => new FarmerActionPlanItemVm
                    {
                        Title = $"Soil test record: {x.OrchardName}", Description = "No soil test date is recorded.",
                        Priority = "DATA_REQUIRED"
                    }).ToList(),
                SmartInsight = details.Any(x => !x.LatestAssessmentOn.HasValue) ? "Some orchards have no admin field assessment." : null
            };
        }

        private async Task<DashboardData> GetData(long userId, long? orchardProfileId, int? selectedYear)
        {
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
            var year = selectedYear ?? DateTime.UtcNow.Year;
            if (year < 2000 || year > 2100) throw new ArgumentOutOfRangeException(nameof(selectedYear));
            var key = (userId, orchardProfileId, year);
            if (_loaded.TryGetValue(key, out var cached)) return cached;
            var from = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var to = from.AddYears(1);
            var now = DateTimeOffset.UtcNow;
            var asOf = now < to ? now : to;
            var orchardQuery = _orchards.Query().AsNoTracking().Where(x => x.UserId == userId);
            if (orchardProfileId.HasValue) orchardQuery = orchardQuery.Where(x => x.Id == orchardProfileId.Value);
            var orchards = await orchardQuery.Include(x => x.Varieties).Include(x => x.SoilAndWater)
                .Include(x => x.CropNutrition).Include(x => x.PlantHealth).Include(x => x.Infrastructure)
                .Include(x => x.Production).AsSplitQuery().OrderBy(x => x.Id).ToListAsync();
            if (orchardProfileId.HasValue && orchards.Count == 0) throw new KeyNotFoundException("Orchard not found.");
            var ids = orchards.Select(x => x.Id).ToList();
            var previousFrom = from.AddYears(-1);
            var activities = ids.Count == 0 ? new List<FarmActivity>() : await _activities.Query().AsNoTracking()
                .Where(x => ids.Contains(x.OrchardProfileId) &&
                    ((x.OccurredOn >= previousFrom && x.OccurredOn < to) ||
                     (x.ScheduledFor >= from && x.ScheduledFor < to)))
                .ToListAsync();
            var assessments = ids.Count == 0 ? new List<FarmAssessment>() : await _assessments.Query().AsNoTracking()
                .Where(x => ids.Contains(x.OrchardProfileId) && x.ObservedOn < to && x.ObservedOn <= now)
                .OrderByDescending(x => x.ObservedOn).ThenByDescending(x => x.Id).ToListAsync();
            var operations = ids.Count == 0 ? new List<FarmOperation>() : await _operations.Query().AsNoTracking()
                .Where(x => ids.Contains(x.OrchardProfileId) && x.OperationDate >= previousFrom.UtcDateTime.Date && x.OperationDate < to.UtcDateTime.Date)
                .ToListAsync();
            var operationIds = operations.Select(x => x.Id).ToList();
            var lots = operationIds.Count == 0 ? new List<FarmHarvestSaleLot>() : await _saleLots.Query().AsNoTracking()
                .Where(x => operationIds.Contains(x.OperationId)).ToListAsync();
            var recipients = ids.Count == 0 ? new List<FarmAdvisoryRecipient>() : await _advisoryRecipients.Query().AsNoTracking()
                .Include(x => x.Advisory).Where(x => ids.Contains(x.OrchardProfileId) && x.Advisory.WindowStart < to && x.Advisory.WindowEnd >= from)
                .ToListAsync();
            var data = new DashboardData
            {
                From = from, To = to, AsOf = asOf, Orchards = orchards, Activities = activities, Assessments = assessments,
                Current = activities.Where(x => x.IsCompleted && x.OccurredOn >= from && x.OccurredOn < asOf).ToList(),
                Operations = operations, CurrentOperations = operations.Where(x => x.Status == "posted" && x.OperationDate >= from.UtcDateTime.Date && x.OperationDate < asOf.UtcDateTime.Date).ToList(),
                SaleLots = lots, AdvisoryRecipients = recipients
            };
            _loaded[key] = data;
            return data;
        }

        private static decimal Area(OrchardProfile o) => (o.Kanals + o.Marlas / 20) * KanalToHectare;
        private static int Trees(OrchardProfile o) => o.Varieties.Sum(x => x.NumberOfTrees);
        private static decimal Round(decimal x) => decimal.Round(x, 2, MidpointRounding.AwayFromZero);
        private string Money(decimal? x) => x.HasValue ? _currency.FormatCurrency(x.Value) : null;
        private static decimal? Ratio(decimal? numerator, decimal? denominator, decimal factor = 1) =>
            numerator.HasValue && denominator > 0 ? Round(numerator.Value / denominator.Value * factor) : null;

        private static decimal? Weighted(List<FarmerOrchardDetailVm> rows, Func<FarmerOrchardDetailVm, decimal?> value, Func<FarmerOrchardDetailVm, decimal> weight)
        {
            if (rows.Count == 0 || rows.Any(x => !value(x).HasValue || weight(x) <= 0)) return null;
            return Ratio(rows.Sum(x => value(x).Value * weight(x)), rows.Sum(weight));
        }

        public static decimal? YieldKg(OrchardProduction production)
        {
            if (production == null || production.AverageAnnualYield < 0) return null;
            return production.YieldUnit?.Trim().ToLowerInvariant() switch
            {
                "kg" or "kgs" or "kilogram" or "kilograms" => production.AverageAnnualYield,
                "quintal" or "quintals" or "qtl" => production.AverageAnnualYield * 100,
                "tonne" or "tonnes" or "metric ton" or "mt" => production.AverageAnnualYield * 1000,
                _ => null
            };
        }

        private static decimal? Sum(IEnumerable<FarmActivity> rows, string kind, Func<FarmActivity, decimal?> value)
        {
            var values = rows.Where(x => kind == null || x.Kind == kind).Select(value).Where(x => x.HasValue).ToList();
            return values.Count > 0 ? values.Sum() : null;
        }
        private static decimal? SumOperations(IEnumerable<FarmOperation> rows, string kind, Func<FarmOperation, decimal?> value)
        {
            var values = rows.Where(x => kind == null || x.Kind == kind).Select(value).Where(x => x.HasValue).ToList();
            return values.Count > 0 ? values.Sum() : null;
        }
        private static decimal? Add(decimal? first, decimal? second) => first.HasValue || second.HasValue ? (first ?? 0) + (second ?? 0) : null;

        private static int CountActivities(DashboardData data, string kind, DateTimeOffset from, DateTimeOffset to) =>
            data.Activities.Count(x => x.Kind == kind && x.IsCompleted && x.OccurredOn >= from && x.OccurredOn < to);

        private static FarmerActivityComparisonVm Activity(string label, int current, int previous) => new()
        {
            Label = label, CurrentValue = current, PreviousValue = previous, ChangePercent = Ratio(current - previous, previous, 100)
        };

        internal static FarmerMetricVm Metric(string code, string label, decimal? value, string unit = null, string description = null, string source = "farm-records") => new()
        {
            Code = code, Label = label, NumericValue = value.HasValue ? Round(value.Value) : null,
            Value = value.HasValue ? Round(value.Value).ToString("0.##", CultureInfo.InvariantCulture) : null,
            Unit = unit, Description = description, Source = source, DataStatus = value.HasValue ? "recorded" : "missing"
        };

        private static FarmerMetricVm TextMetric(string code, string label, string value, string description = null) => new()
        {
            Code = code, Label = label, Value = value, Description = description,
            DataStatus = string.IsNullOrWhiteSpace(value) ? "missing" : "recorded", Source = "orchard-profile-or-assessment"
        };

        private static List<FarmAssessment> Latest(DashboardData data, Func<FarmAssessment, bool> predicate) =>
            data.Assessments.Where(predicate).GroupBy(x => x.OrchardProfileId).Select(x => x.First()).ToList();

        private static List<string> JsonItems(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return new List<string>();
            try
            {
                using var doc = JsonDocument.Parse(value);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return new List<string>();
                return doc.RootElement.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            }
            catch (JsonException)
            {
                return value.TrimStart().StartsWith("[") || value.TrimStart().StartsWith("{")
                    ? new List<string>() : new List<string> { value };
            }
        }

        private sealed class DashboardData
        {
            public DateTimeOffset From { get; set; }
            public DateTimeOffset To { get; set; }
            public DateTimeOffset AsOf { get; set; }
            public List<OrchardProfile> Orchards { get; set; }
            public List<FarmActivity> Activities { get; set; }
            public List<FarmActivity> Current { get; set; }
            public List<FarmAssessment> Assessments { get; set; }
            public List<FarmOperation> Operations { get; set; }
            public List<FarmOperation> CurrentOperations { get; set; }
            public List<FarmHarvestSaleLot> SaleLots { get; set; }
            public List<FarmAdvisoryRecipient> AdvisoryRecipients { get; set; }
        }
    }
}
