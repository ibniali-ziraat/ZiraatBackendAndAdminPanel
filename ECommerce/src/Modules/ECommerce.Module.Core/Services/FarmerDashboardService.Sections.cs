using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public partial class FarmerDashboardService
    {
        public async Task<FarmerNutrientDashboardVm> GetNutrient(long userId, long? orchardProfileId = null, int? year = null)
        {
            var data = await GetData(userId, orchardProfileId, year);
            var result = new FarmerNutrientDashboardVm { TrendNote = "This compatibility view contains legacy/unverified profile or assessment values. Use the orchard lab latest and trend endpoints for authoritative approved readings; no interpolation is applied." };
            foreach (var orchard in data.Orchards)
            {
                var samples = data.Assessments.Where(x => x.OrchardProfileId == orchard.Id).ToList();
                var operations = data.Current.Where(x => x.OrchardProfileId == orchard.Id).ToList();
                var soil = orchard.SoilAndWater;
                var section = new FarmerOrchardNutrientVm
                {
                    OrchardProfileId = orchard.Id, OrchardName = orchard.OrchardName,
                    Metrics = new List<FarmerMetricVm>
                    {
                        Metric("soil-ph", "Soil pH", samples.FirstOrDefault(x => x.SoilPh.HasValue)?.SoilPh ?? soil?.SoilPh, "pH", "Latest assessment or current profile reading."),
                        Metric("ec-status", "Soil EC", samples.FirstOrDefault(x => x.SoilEc.HasValue)?.SoilEc, "dS/m"),
                        Metric("organic-matter", "Organic Matter", samples.FirstOrDefault(x => x.OrganicMatterPercent.HasValue)?.OrganicMatterPercent, "%", "Organic carbon is not substituted for organic matter."),
                        TextMetric("organic-carbon", "Recorded Organic Carbon", soil?.SoilOrganicCarbon),
                        TextMetric("nitrogen", "N Level", samples.FirstOrDefault(x => x.NitrogenLevel != null)?.NitrogenLevel ?? soil?.SoilNitrogen),
                        TextMetric("phosphorus", "P Level", samples.FirstOrDefault(x => x.PhosphorusLevel != null)?.PhosphorusLevel ?? soil?.SoilPhosphorus),
                        TextMetric("potassium", "K Level", samples.FirstOrDefault(x => x.PotassiumLevel != null)?.PotassiumLevel ?? soil?.SoilPotassium),
                        Metric("fertigation", "Fertigation Compliance", Compliance(data, "fertigation", orchard.Id), "%", "Completed on or before the due time / scheduled activities due in the selected year."),
                        Metric("irrigation-volume", "Irrigation Volume Applied", Ratio(Add(Sum(operations, "irrigation", x => x.Quantity), SumOperations(data.CurrentOperations.Where(x => x.OrchardProfileId == orchard.Id), "fertigation", x => x.IrrigationTotalLitres)), Area(orchard)), "L/ha"),
                        Metric("water-use-efficiency", "Water Use Efficiency", null, "%", "Requires a defined agronomic efficiency model and measured water balance."),
                        Metric("deficiency-symptoms", "Recorded Deficiency Symptoms", samples.FirstOrDefault(x => x.ObservedOn >= data.From && x.DeficiencySymptomsCount.HasValue)?.DeficiencySymptomsCount, "count", "Latest admin seasonal count for this orchard.")
                    }
                };
                foreach (var metric in section.Metrics.Take(7))
                {
                    if (metric.DataStatus != "missing") metric.DataStatus = "legacy-unverified";
                    metric.Source = "legacy-profile-or-assessment";
                }
                foreach (var sample in samples.Where(x => x.ObservedOn >= data.From).OrderBy(x => x.ObservedOn))
                {
                    AddPoint(section.SoilHealthTrend, sample, "ph", sample.SoilPh);
                    AddPoint(section.SoilHealthTrend, sample, "ec-ds-m", sample.SoilEc);
                    AddPoint(section.SoilHealthTrend, sample, "organic-matter-percent", sample.OrganicMatterPercent);
                }
                result.Orchards.Add(section);
            }
            // A farm-wide pH or categorical NPK average would hide individual orchard conditions.
            if (result.Orchards.Count == 1) result.Metrics = result.Orchards[0].Metrics;
            result.SoilHealthTrend = result.Orchards.SelectMany(x => x.SoilHealthTrend).ToList();
            return result;
        }

        public async Task<FarmerPestDiseaseDashboardVm> GetPestDisease(long userId, long? orchardProfileId = null, int? year = null)
        {
            var data = await GetData(userId, orchardProfileId, year);
            var pests = Latest(data, x => x.TreesMonitored.HasValue && x.PestAffectedTrees.HasValue);
            var diseases = Latest(data, x => x.TreesMonitored.HasValue && x.DiseaseAffectedTrees.HasValue);
            var risks = Latest(data, x => x.RiskLevel != null);
            var risk = risks.OrderByDescending(x => x.RiskLevel == "High" ? 3 : x.RiskLevel == "Medium" ? 2 : 1).FirstOrDefault()?.RiskLevel;
            var resistance = Latest(data, x => x.PesticideResistanceSuspected.HasValue);
            var sprays = data.Activities.Where(x => x.Kind == "spray").ToList();
            var last = data.Current.Where(x => x.Kind == "spray").OrderByDescending(x => x.OccurredOn).FirstOrDefault();
            var next = sprays.Where(x => !x.IsCompleted && x.ScheduledFor >= data.AsOf && x.ScheduledFor < data.To).OrderBy(x => x.ScheduledFor).FirstOrDefault();
            var referenceDate = data.AsOf.AddTicks(-1);
            var result = new FarmerPestDiseaseDashboardVm
            {
                Metrics = new List<FarmerMetricVm>
                {
                    Metric("pest-incidence", "Pest Incidence", pests.Count == 0 ? null : Ratio(pests.Sum(x => x.PestAffectedTrees), pests.Sum(x => x.TreesMonitored), 100), "per 100 trees monitored", "Latest admin assessment per orchard; assessed trees only."),
                    Metric("disease-incidence-rate", "Disease Incidence Rate", diseases.Count == 0 ? null : Ratio(diseases.Sum(x => x.DiseaseAffectedTrees), diseases.Sum(x => x.TreesMonitored), 100), "%", "Latest admin assessment per orchard; assessed trees only."),
                    TextMetric("risk-level", "Pest/Disease Risk Level", risk, "Highest latest recorded admin risk; no weather-derived model is assumed."),
                    TextMetric("resistance-developed", "Resistance Suspected", resistance.Count == 0 ? null : resistance.Any(x => x.PesticideResistanceSuspected == true) ? "Yes" : "No", "Admin assessment; not inferred from spray counts."),
                    Metric("spray-compliance", "Spray Compliance", Compliance(data, "spray"), "%"),
                    Metric("photo-diagnoses", "Photo Diagnoses Submitted", null, "count", "No photo diagnosis workflow is currently stored."),
                    Metric("avg-response-time", "Average Expert Response Time", null, "hours", "Requires linked submissions and expert response timestamps.")
                },
                HighRiskPests = data.Orchards.SelectMany(x => JsonItems(x.PlantHealth?.PestHistory)).Distinct()
                    .Select(x => new FarmerRecommendationVm { Title = x, Description = "Recorded pest history; not a current risk classification." }).ToList(),
                DiseaseObservations = data.Orchards.SelectMany(x => JsonItems(x.PlantHealth?.DiseaseHistory)).Distinct()
                    .Select(x => new FarmerRecommendationVm { Title = x, Description = "Recorded disease history." }).ToList(),
                SpraySchedule = new FarmerSprayScheduleVm
                {
                    LastAppliedSpray = last == null ? null : $"{last.Notes ?? "Spray"} ({last.OccurredOn:O})",
                    NextScheduledSpray = next == null ? null : $"{next.Notes ?? "Spray"} ({next.ScheduledFor:O})",
                    MissedSpraysThisMonth = sprays.Count(x => !x.IsCompleted && x.ScheduledFor < data.AsOf &&
                        x.ScheduledFor >= new DateTimeOffset(referenceDate.Year, referenceDate.Month, 1, 0, 0, 0, TimeSpan.Zero)),
                    CompliancePercent = Compliance(data, "spray")
                },
                TrendNote = "Observed incidence per orchard; historical pest lists are not counted as current infestations."
            };
            foreach (var sample in data.Assessments.Where(x => x.ObservedOn >= data.From).OrderBy(x => x.ObservedOn))
            {
                AddPoint(result.PestDiseaseTrend, sample, "pest-per-100-trees", Ratio(sample.PestAffectedTrees, sample.TreesMonitored, 100));
                AddPoint(result.PestDiseaseTrend, sample, "disease-percent", Ratio(sample.DiseaseAffectedTrees, sample.TreesMonitored, 100));
            }
            return result;
        }

        public async Task<FarmerResourceUsageDashboardVm> GetResources(long userId, long? orchardProfileId = null, int? year = null)
        {
            var data = await GetData(userId, orchardProfileId, year);
            var area = data.Orchards.Sum(Area);
            var acres = area / HectaresPerAcre;
            var operationCost = SumOperations(data.CurrentOperations, null, x => x.Cost);
            var cost = Add(Sum(data.Current, "expense", x => x.Amount), operationCost);
            var fertilizerCost = Expense(data, "fertilizer");
            var laborCost = Expense(data, "labor");
            var materialCost = Expense(data, "material");
            var laborHours = Add(Sum(data.Current, "labor", x => x.Quantity), SumOperations(data.CurrentOperations, null, x => x.LabourHours));
            var fertilizer = Ratio(Add(Sum(data.Current, "fertilizer", x => x.Quantity), SumOperations(data.CurrentOperations, "fertigation", x => x.MaterialQuantity)), area);
            return new FarmerResourceUsageDashboardVm
            {
                Metrics = new List<FarmerMetricVm>
                {
                    Metric("fertilizer-consumption", "Fertilizer Consumption", fertilizer, "kg/ha"),
                    Metric("pesticide-usage", "Pesticide Usage", Ratio(Add(Sum(data.Current, "spray", x => x.Quantity), SumOperations(data.CurrentOperations.Where(x => x.MaterialUnit == "ml"), "spray", x => x.MaterialQuantity)), area), "ml/ha", "Only normalized liquid product quantity is counted; dose and coverage alone are insufficient."),
                    Metric("sprays-season", "Sprays Recorded", data.Current.Count(x => x.Kind == "spray") + data.CurrentOperations.Count(x => x.Kind == "spray"), "count"),
                    TextMetric("usage-condition", "Pesticide Over/Under Usage", null, "Requires product-specific dose recommendations and treated area."),
                    Metric("labor-hours", "Labor Hours Logged", laborHours, "hours"),
                    Metric("equipment-hours", "Equipment Hours Logged", Add(Sum(data.Current, "equipment", x => x.Quantity), SumOperations(data.CurrentOperations, null, x => x.EquipmentHours)), "hours"),
                    Metric("machinery-utilization", "Machinery Utilization", null, "%", "Requires available equipment hours as the denominator."),
                    Metric("total-input-cost", "Total Recorded Input Cost", cost, "currency"),
                    Metric("input-cost-per-acre", "Input Cost per Acre", Ratio(cost, acres), "currency/acre"),
                    Metric("input-cost-per-tree", "Input Cost per Tree", Ratio(cost, data.Orchards.Sum(Trees)), "currency/tree")
                },
                ResourceSummary = new FarmerResourceSummaryVm
                {
                    FertilizerCost = Add(fertilizerCost, SumOperations(data.CurrentOperations, "fertigation", x => x.Cost)), FertilizerCostString = Money(Add(fertilizerCost, SumOperations(data.CurrentOperations, "fertigation", x => x.Cost))),
                    LaborCost = laborCost, LaborCostString = Money(laborCost),
                    MaterialCost = materialCost, MaterialCostString = Money(materialCost),
                    OverallExpenditure = cost, OverallExpenditureString = Money(cost)
                },
                InputEfficiency = new FarmerInputEfficiencyVm
                {
                    FertilizerUse = fertilizer,
                    LaborProductivity = Ratio(Add(Sum(data.Current, "harvest", x => x.Quantity), SumOperations(data.CurrentOperations, "harvest", x => x.HarvestQuantityKg)), laborHours)
                },
                PerformanceInsights = !cost.HasValue ? new List<string> { "Record expenses to calculate input costs." } : new List<string>()
            };
        }

        public async Task<FarmerFinancialDashboardVm> GetFinancial(long userId, long? orchardProfileId = null, int? year = null)
        {
            var data = await GetData(userId, orchardProfileId, year);
            var acres = data.Orchards.Sum(Area) / HectaresPerAcre;
            var activeHarvestIds = data.CurrentOperations.Where(x => x.Kind == "harvest").Select(x => x.Id).ToHashSet();
            var lotIncomeValues = data.SaleLots.Where(x => x.IsSold && activeHarvestIds.Contains(x.OperationId) && x.Income.HasValue).Select(x => x.Income.Value).ToList();
            var operationRevenue = lotIncomeValues.Count > 0 ? lotIncomeValues.Sum() : (decimal?)null;
            var revenue = Add(Sum(data.Current, "sale", x => x.Amount), operationRevenue);
            var cost = Add(Sum(data.Current, "expense", x => x.Amount), SumOperations(data.CurrentOperations, null, x => x.Cost));
            var profit = revenue - cost;
            var soldLotValues = data.SaleLots.Where(x => x.IsSold && activeHarvestIds.Contains(x.OperationId)).Select(x => x.QuantityKg).ToList();
            var soldKg = Add(Sum(data.Current, "sale", x => x.Quantity), soldLotValues.Count > 0 ? soldLotValues.Sum() : (decimal?)null);
            var price = soldKg > 0 && revenue.HasValue ? revenue / soldKg : null;
            var previousEnd = data.AsOf.AddYears(-1);
            var previous = data.Activities.Where(x => x.IsCompleted && x.OccurredOn >= data.From.AddYears(-1) && x.OccurredOn < previousEnd);
            var previousOperations = data.Operations.Where(x => x.Status == "posted" && x.OperationDate >= data.From.AddYears(-1).UtcDateTime.Date && x.OperationDate < previousEnd.UtcDateTime.Date);
            var previousYield = Add(Sum(previous, "harvest", x => x.Quantity), SumOperations(previousOperations, "harvest", x => x.HarvestQuantityKg));
            var currentYield = Add(Sum(data.Current, "harvest", x => x.Quantity), SumOperations(data.CurrentOperations, "harvest", x => x.HarvestQuantityKg));
            var result = new FarmerFinancialDashboardVm
            {
                Metrics = new List<FarmerMetricVm>
                {
                    Metric("cost-of-cultivation", "Recorded Cost of Cultivation", Ratio(cost, acres), "currency/acre", "Selected calendar year; recorded expenses only."),
                    Metric("gross-revenue", "Recorded Gross Revenue", Ratio(revenue, acres), "currency/acre", "Produce sale records; customer shopping orders are not farm revenue."),
                    Metric("net-profit", "Recorded Net Profit", Ratio(profit, acres), "currency/acre", "Requires both sales and expense records; excludes unrecorded costs."),
                    Metric("return-on-investment", "Return on Investment", Ratio(profit, cost, 100), "%"),
                    Metric("break-even-yield", "Break-Even Yield", price > 0 ? Ratio(cost / price, acres) : null, "kg/acre", "Recorded cost / unrounded weighted realized sale price / current acreage."),
                    Metric("year-on-year", "Year-on-Year Yield Growth", Ratio(currentYield - previousYield, previousYield, 100), "%", "Recorded harvest compared with the equivalent prior-year date window."),
                    Metric("total-revenue", "Total Recorded Revenue", revenue, "currency"),
                    Metric("total-cost", "Total Recorded Cost", cost, "currency"),
                    Metric("total-profit", "Total Recorded Profit", profit, "currency"),
                    Metric("recorded-harvest", "Recorded Harvest", currentYield, "kg")
                }
            };
            if (cost.HasValue) result.RevenueVsCost.Add(new FarmerChartPointVm { Label = "Cost", Value = cost.Value });
            if (revenue.HasValue) result.RevenueVsCost.Add(new FarmerChartPointVm { Label = "Revenue", Value = revenue.Value });
            var monthlyCashFlow = data.Current.Where(x => x.Kind == "sale" || x.Kind == "expense")
                .GroupBy(x => x.OccurredOn.UtcDateTime.Month)
                .ToDictionary(x => x.Key, x => x.Sum(row => row.Kind == "sale" ? row.Amount ?? 0 : -(row.Amount ?? 0)));
            foreach (var operation in data.CurrentOperations)
            {
                var month = operation.OperationDate.Month;
                monthlyCashFlow[month] = monthlyCashFlow.GetValueOrDefault(month) - (operation.Cost ?? 0);
                if (operation.Kind == "harvest") monthlyCashFlow[month] += data.SaleLots
                    .Where(x => x.OperationId == operation.Id && x.IsSold).Sum(x => x.Income ?? 0);
            }
            foreach (var month in monthlyCashFlow.OrderBy(x => x.Key))
            {
                result.NetProfitTrend.Add(new FarmerChartPointVm
                {
                    Label = $"{data.From.Year}-{month.Key:00}", Series = "recorded-net-cash-flow",
                    Value = month.Value
                });
            }
            if (!cost.HasValue || !revenue.HasValue)
                result.RecentInsights.Add(new FarmerFinancialInsightVm { Code = "incomplete-ledger", Title = "Add sales and expense records before interpreting profitability.", Status = "Data required" });
            if (profit < 0)
                result.RecentInsights.Add(new FarmerFinancialInsightVm { Code = "recorded-loss", Title = "Recorded expenses exceed recorded sales in this period.", Status = "Review" });
            return result;
        }

        public async Task<FarmerAdvisoryDashboardVm> GetAdvisory(long userId, long? orchardProfileId = null, int? year = null)
        {
            var data = await GetData(userId, orchardProfileId, year);
            var result = new FarmerAdvisoryDashboardVm
            {
                Scope = "Forum metrics: user; advisory compliance: selected orchards.",
                EngagementScore = new FarmerEngagementScoreVm { Label = "No engagement scoring model is configured." },
                KeyMetrics = new List<FarmerMetricVm>
                {
                    Metric("articles-read", "Knowledge Articles Read", null, "count", "Article read events are not tracked."),
                    Metric("seasonal-advisory-compliance", "Seasonal Advisory Compliance", WorkflowCompliance(data), "%", "Posted operations completed on/before the advisory deadline / published, non-cancelled recipients due as of now."),
                    Metric("expert-consultations", "Expert Consultations", null, "count", "Consultation booking records are not implemented."),
                    Metric("complaints-registered", "Complaints Registered", null, "count", "No complaint lifecycle is stored."),
                    Metric("complaints-resolved", "Complaints Resolved", null, "count", "Requires complaint resolution records.")
                }
            };
            foreach (var source in _advisorySources)
            {
                var contribution = await source.GetAdvisory(userId, data.From, data.AsOf);
                foreach (var metric in contribution.KeyMetrics) result.KeyMetrics.Add(metric);
                foreach (var activity in contribution.RecentActivities) result.RecentActivities.Add(activity);
            }
            result.RecentActivities = result.RecentActivities.OrderByDescending(x => x.OccurredOn).Take(10).ToList();
            return result;
        }

        public async Task<FarmerAlertsDashboardVm> GetAlerts(long userId, long? orchardProfileId = null, int? year = null)
        {
            var data = await GetData(userId, orchardProfileId, year);
            var result = new FarmerAlertsDashboardVm
            {
                Scope = "Notifications: user (not orchard-linked); pending actions: selected orchards.",
                Summary = new List<FarmerMetricVm>
                {
                    Metric("pending-actions", "Pending Scheduled Actions", data.Activities.Count(x => !x.IsCompleted && x.ScheduledFor >= data.From && x.ScheduledFor < data.To), "count"),
                    Metric("advisory-tickets-resolved", "Advisory Tickets Resolved", null, "%", "No advisory ticket lifecycle is stored."),
                    Metric("services-booked", "Services Booked", null, "count", "No service booking workflow is stored.")
                },
                Categories = new List<FarmerAlertCategoryVm>
                {
                    new() { Code = "weather", Label = "Weather Alerts" },
                    new() { Code = "pest", Label = "Pest Alerts" },
                    new() { Code = "nutrient", Label = "Nutrient Alerts" },
                    new() { Code = "spray-advisory", Label = "Spray Advisory" }
                }
            };
            foreach (var source in _alertSources)
            {
                var contribution = await source.GetAlerts(userId, data.From, data.AsOf);
                foreach (var metric in contribution.Summary) result.Summary.Add(metric);
                foreach (var item in contribution.RecentTimeline) result.RecentTimeline.Add(item);
            }
            result.RecentTimeline = result.RecentTimeline.OrderByDescending(x => x.OccurredOn).Take(20).ToList();
            return result;
        }

        private static decimal? Expense(DashboardData data, string category) =>
            Sum(data.Current.Where(x => x.ExpenseCategory == category), "expense", x => x.Amount);

        private static decimal? Compliance(DashboardData data, string kind, long? orchardId = null)
        {
            var due = data.Activities.Where(x => x.Kind == kind && (!orchardId.HasValue || x.OrchardProfileId == orchardId) &&
                x.ScheduledFor >= data.From && x.ScheduledFor < data.AsOf).ToList();
            return Ratio(due.Count(x => x.IsCompleted && x.OccurredOn <= x.ScheduledFor), due.Count, 100);
        }

        private static decimal? WorkflowCompliance(DashboardData data)
        {
            var due = data.AdvisoryRecipients.Where(x => x.Advisory.Status == "published" && x.Advisory.WindowEnd <= data.AsOf).ToList();
            if (due.Count == 0) return null;
            var onTime = due.Count(r => data.Operations.Any(o => o.RecipientId == r.Id && o.Status == "posted" && o.OperationDate.Date <= r.Advisory.WindowEnd.UtcDateTime.Date));
            return Ratio(onTime, due.Count, 100);
        }

        private static void AddPoint(IList<FarmerChartPointVm> points, FarmAssessment sample, string series, decimal? value)
        {
            if (value.HasValue) points.Add(new FarmerChartPointVm
            {
                OrchardProfileId = sample.OrchardProfileId, Label = sample.ObservedOn.ToString("yyyy-MM-dd"),
                ObservedOn = sample.ObservedOn, Series = series, Value = value.Value
            });
        }
    }
}
