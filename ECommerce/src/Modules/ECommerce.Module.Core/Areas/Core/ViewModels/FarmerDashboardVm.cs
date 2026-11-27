using System;
using System.Collections.Generic;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class FarmerDashboardVm
    {
        public int Year { get; set; }
        public DateTimeOffset GeneratedAt { get; set; }
        public FarmerDashboardHomeVm Home { get; set; }

        public FarmerOrchardDashboardVm Orchard { get; set; }

        public FarmerNutrientDashboardVm Nutrient { get; set; }

        public FarmerPestDiseaseDashboardVm PestDisease { get; set; }

        public FarmerResourceUsageDashboardVm Resources { get; set; }

        public FarmerFinancialDashboardVm Financial { get; set; }

        public FarmerAdvisoryDashboardVm Advisory { get; set; }

        public FarmerAlertsDashboardVm Alerts { get; set; }
        public FarmerWeatherDashboardVm Weather { get; set; }
    }

    public class FarmerDashboardHomeVm
    {
        public FarmerWeatherSummaryVm Weather { get; set; } = new FarmerWeatherSummaryVm();

        public IList<FarmerQuickActionVm> QuickActions { get; set; } = new List<FarmerQuickActionVm>();

        public FarmerOverviewVm FarmOverview { get; set; } = new FarmerOverviewVm();

        public IList<FarmerTipVm> Tips { get; set; } = new List<FarmerTipVm>();

        public string LegalDisclaimer { get; set; }
    }

    public class FarmerWeatherSummaryVm
    {
        public string DataStatus { get; set; } = "missing";
        public long? OrchardProfileId { get; set; }
        public string UnavailableReason { get; set; }
        public string Location { get; set; }

        public decimal? TemperatureCelsius { get; set; }

        public decimal? FeelsLikeCelsius { get; set; }

        public decimal? HumidityPercent { get; set; }

        public decimal? WindSpeedKmph { get; set; }

        public string StormRisk { get; set; }
    }

    public class FarmerQuickActionVm
    {
        public string Code { get; set; }

        public string Label { get; set; }
    }

    public class FarmerOverviewVm
    {
        public string Period { get; set; }

        public IList<FarmerActivityComparisonVm> ActivityComparison { get; set; } = new List<FarmerActivityComparisonVm>();
    }

    public class FarmerActivityComparisonVm
    {
        public string Label { get; set; }

        public int CurrentValue { get; set; }

        public int PreviousValue { get; set; }

        public decimal? ChangePercent { get; set; }
    }

    public class FarmerTipVm
    {
        public string Code { get; set; }

        public string Title { get; set; }
    }

    public class FarmerOrchardDashboardVm
    {
        public decimal TotalAreaHa { get; set; }
        public decimal TotalAreaKanals { get; set; }
        public int TotalOrchards { get; set; }
        public int TotalTrees { get; set; }
        public decimal? PlantDensityTreesPerHa { get; set; }
        public string AssessmentScope { get; set; }
        public IList<FarmerOrchardDetailVm> Orchards { get; set; } = new List<FarmerOrchardDetailVm>();
        public decimal TotalAverageAreaHa { get; set; }

        public string TotalAverageAreaText { get; set; }

        public decimal? SurvivalRatePercent { get; set; }

        public decimal? AverageTreeAgeYears { get; set; }

        public string SmartInsight { get; set; }

        public FarmerProductivityVm EstimatedProductivity { get; set; } = new FarmerProductivityVm();

        public IList<FarmerChartPointVm> GraphicalAnalysis { get; set; } = new List<FarmerChartPointVm>();

        public IList<FarmerActionPlanItemVm> ActionPlan { get; set; } = new List<FarmerActionPlanItemVm>();
    }

    public class FarmerProductivityVm
    {
        public decimal? EstimatedYieldKgPerTree { get; set; }

        public decimal? ProjectedYieldKg { get; set; }
        public decimal? YieldKgPerHectare { get; set; }
        public decimal? RecordedHarvestKg { get; set; }
        public string DataStatus { get; set; }

        public decimal? RevenueEstimate { get; set; }

        public string RevenueEstimateString { get; set; }
    }

    public class FarmerChartPointVm
    {
        public long? OrchardProfileId { get; set; }
        public string Series { get; set; }
        public DateTimeOffset? ObservedOn { get; set; }
        public string Label { get; set; }

        public decimal Value { get; set; }
    }

    public class FarmerActionPlanItemVm
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string ExpectedImpact { get; set; }

        public string Priority { get; set; }
    }

    public class FarmerNutrientDashboardVm
    {
        public IList<FarmerOrchardNutrientVm> Orchards { get; set; } = new List<FarmerOrchardNutrientVm>();
        public IList<FarmerMetricVm> Metrics { get; set; } = new List<FarmerMetricVm>();

        public IList<FarmerChartPointVm> SoilHealthTrend { get; set; } = new List<FarmerChartPointVm>();

        public string TrendNote { get; set; }
    }

    public class FarmerPestDiseaseDashboardVm
    {
        public IList<FarmerMetricVm> Metrics { get; set; } = new List<FarmerMetricVm>();

        public IList<FarmerRecommendationVm> HighRiskPests { get; set; } = new List<FarmerRecommendationVm>();

        public IList<FarmerRecommendationVm> DiseaseObservations { get; set; } = new List<FarmerRecommendationVm>();

        public FarmerSprayScheduleVm SpraySchedule { get; set; } = new FarmerSprayScheduleVm();

        public IList<FarmerChartPointVm> PestDiseaseTrend { get; set; } = new List<FarmerChartPointVm>();

        public string TrendNote { get; set; }
    }

    public class FarmerResourceUsageDashboardVm
    {
        public IList<FarmerMetricVm> Metrics { get; set; } = new List<FarmerMetricVm>();

        public FarmerResourceSummaryVm ResourceSummary { get; set; } = new FarmerResourceSummaryVm();

        public FarmerInputEfficiencyVm InputEfficiency { get; set; } = new FarmerInputEfficiencyVm();

        public IList<string> PerformanceInsights { get; set; } = new List<string>();
    }

    public class FarmerFinancialDashboardVm
    {
        public IList<FarmerMetricVm> Metrics { get; set; } = new List<FarmerMetricVm>();

        public IList<FarmerChartPointVm> RevenueVsCost { get; set; } = new List<FarmerChartPointVm>();

        public IList<FarmerChartPointVm> NetProfitTrend { get; set; } = new List<FarmerChartPointVm>();

        public IList<FarmerFinancialInsightVm> RecentInsights { get; set; } = new List<FarmerFinancialInsightVm>();
    }

    public class FarmerAdvisoryDashboardVm
    {
        public string Scope { get; set; } = "user";
        public IList<FarmerMetricVm> KeyMetrics { get; set; } = new List<FarmerMetricVm>();

        public FarmerEngagementScoreVm EngagementScore { get; set; } = new FarmerEngagementScoreVm();

        public IList<string> ParticipationHeat { get; set; } = new List<string>();

        public IList<FarmerActivityVm> RecentActivities { get; set; } = new List<FarmerActivityVm>();

        public string KnowledgeMessage { get; set; }
    }

    public class FarmerAlertsDashboardVm
    {
        public string Scope { get; set; } = "user";
        public IList<FarmerMetricVm> Summary { get; set; } = new List<FarmerMetricVm>();

        public IList<FarmerAlertCategoryVm> Categories { get; set; } = new List<FarmerAlertCategoryVm>();

        public IList<FarmerAlertTimelineItemVm> RecentTimeline { get; set; } = new List<FarmerAlertTimelineItemVm>();
    }

    public class FarmerMetricVm
    {
        public string DataStatus { get; set; } = "missing";
        public string Unit { get; set; }
        public string Source { get; set; }
        public string Code { get; set; }

        public string Label { get; set; }

        public string Value { get; set; }

        public string Description { get; set; }

        public decimal? NumericValue { get; set; }
    }

    public class FarmerRecommendationVm
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string Recommended { get; set; }
    }

    public class FarmerSprayScheduleVm
    {
        public string LastAppliedSpray { get; set; }

        public string NextScheduledSpray { get; set; }

        public int? MissedSpraysThisMonth { get; set; }
        public decimal? CompliancePercent { get; set; }
    }

    public class FarmerResourceSummaryVm
    {
        public decimal? FertilizerCost { get; set; }

        public string FertilizerCostString { get; set; }

        public decimal? LaborCost { get; set; }

        public string LaborCostString { get; set; }

        public decimal? MaterialCost { get; set; }

        public string MaterialCostString { get; set; }

        public decimal? OverallExpenditure { get; set; }

        public string OverallExpenditureString { get; set; }
    }

    public class FarmerInputEfficiencyVm
    {
        public decimal? FertilizerUse { get; set; }

        public decimal? LaborProductivity { get; set; }

        public decimal? CostEfficiency { get; set; }
    }

    public class FarmerFinancialInsightVm
    {
        public string Code { get; set; }

        public string Title { get; set; }

        public string Status { get; set; }
    }

    public class FarmerEngagementScoreVm
    {
        public int? Score { get; set; }

        public int? MaxScore { get; set; }

        public string Label { get; set; }

        public int? Percentile { get; set; }
    }

    public class FarmerActivityVm
    {
        public DateTimeOffset? OccurredOn { get; set; }
        public string Title { get; set; }

        public string Reference { get; set; }

        public string Status { get; set; }
    }

    public class FarmerAlertCategoryVm
    {
        public string Code { get; set; }

        public string Label { get; set; }
    }

    public class FarmerAlertTimelineItemVm
    {
        public string Id { get; set; }
        public DateTimeOffset? OccurredOn { get; set; }
        public bool? IsAcknowledged { get; set; }
        public string Category { get; set; }

        public string Title { get; set; }

        public string OccurredText { get; set; }
    }

    public class FarmerOrchardDetailVm
    {
        public long OrchardProfileId { get; set; }
        public string OrchardName { get; set; }
        public decimal AreaHa { get; set; }
        public int EstablishmentYear { get; set; }
        public int TotalTrees { get; set; }
        public decimal? PlantDensityTreesPerHa { get; set; }
        public decimal? SurvivalRatePercent { get; set; }
        public decimal? AverageTreeAgeYears { get; set; }
        public decimal? YieldKgPerTree { get; set; }
        public decimal? YieldKgPerHectare { get; set; }
        public DateTimeOffset? LatestAssessmentOn { get; set; }
        public IList<FarmerVarietyVm> Varieties { get; set; } = new List<FarmerVarietyVm>();
    }

    public class FarmerVarietyVm
    {
        public string Name { get; set; }
        public int Trees { get; set; }
        public int TreesLessThan5Years { get; set; }
        public int TreesFiveToTenYears { get; set; }
        public int TreesMoreThan10Years { get; set; }
    }

    public class FarmerOrchardNutrientVm
    {
        public long OrchardProfileId { get; set; }
        public string OrchardName { get; set; }
        public IList<FarmerMetricVm> Metrics { get; set; } = new List<FarmerMetricVm>();
        public IList<FarmerChartPointVm> SoilHealthTrend { get; set; } = new List<FarmerChartPointVm>();
    }
}
