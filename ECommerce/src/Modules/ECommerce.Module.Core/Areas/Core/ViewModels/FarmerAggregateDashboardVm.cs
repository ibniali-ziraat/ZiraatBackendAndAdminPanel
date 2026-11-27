using System.Collections.Generic;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class FarmerAggregateDashboardVm : FarmerDashboardVm
    {
        public string Scope => "all-owned-orchards";
        public FarmerDashboardSummaryVm Summary { get; set; } = new();
        public IList<FarmerDashboardOrchardVm> Orchards { get; set; } = new List<FarmerDashboardOrchardVm>();
    }

    public class FarmerDashboardSummaryVm
    {
        public int TotalOrchards { get; set; }
        public decimal TotalAreaHa { get; set; }
        public decimal TotalAreaKanals { get; set; }
        public int TotalTrees { get; set; }
        public decimal? RecordedHarvestKg { get; set; }
        public decimal? ProjectedYieldKg { get; set; }
        public decimal? TotalRevenue { get; set; }
        public decimal? TotalCost { get; set; }
        public decimal? TotalProfit { get; set; }
    }

    public class FarmerDashboardOrchardVm
    {
        public long OrchardProfileId { get; set; }
        public string OrchardId { get; set; }
        public string OrchardName { get; set; }
        public string District { get; set; }
        public string Tehsil { get; set; }
        public string Zone { get; set; }
        public string PinCode { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int EstablishmentYear { get; set; }
        public decimal AreaHa { get; set; }
        public decimal Kanals { get; set; }
        public decimal Marlas { get; set; }
        public int TotalTrees { get; set; }
    }
}
