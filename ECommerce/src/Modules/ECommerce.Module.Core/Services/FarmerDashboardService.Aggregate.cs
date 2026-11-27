using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;

namespace ECommerce.Module.Core.Services
{
    public partial class FarmerDashboardService
    {
        public async Task<FarmerAggregateDashboardVm> GetAggregate(long userId, int? year = null)
        {
            var reportingYear = year ?? DateTime.UtcNow.Year;
            // Reuse the section calculations and their scoped data cache; do not call the API or reload per orchard.
            var dashboard = await GetDashboard(userId, year: reportingYear);
            var data = await GetData(userId, null, reportingYear);
            var financial = dashboard.Financial.Metrics.ToDictionary(x => x.Code, x => x.NumericValue);

            return new FarmerAggregateDashboardVm
            {
                Year = dashboard.Year,
                GeneratedAt = dashboard.GeneratedAt,
                Summary = new FarmerDashboardSummaryVm
                {
                    TotalOrchards = dashboard.Orchard.TotalOrchards,
                    TotalAreaHa = dashboard.Orchard.TotalAreaHa,
                    TotalAreaKanals = dashboard.Orchard.TotalAreaKanals,
                    TotalTrees = dashboard.Orchard.TotalTrees,
                    RecordedHarvestKg = dashboard.Orchard.EstimatedProductivity.RecordedHarvestKg,
                    ProjectedYieldKg = dashboard.Orchard.EstimatedProductivity.ProjectedYieldKg,
                    TotalRevenue = financial["total-revenue"],
                    TotalCost = financial["total-cost"],
                    TotalProfit = financial["total-profit"]
                },
                Orchards = data.Orchards.Select(orchard => new FarmerDashboardOrchardVm
                {
                    OrchardProfileId = orchard.Id,
                    OrchardId = orchard.OrchardId,
                    OrchardName = orchard.OrchardName,
                    District = orchard.District,
                    Tehsil = orchard.Tehsil,
                    Zone = orchard.Zone,
                    PinCode = orchard.PinCode,
                    Latitude = orchard.Latitude,
                    Longitude = orchard.Longitude,
                    EstablishmentYear = orchard.EstablishmentYear,
                    AreaHa = Round(Area(orchard)),
                    Kanals = orchard.Kanals,
                    Marlas = orchard.Marlas,
                    TotalTrees = Trees(orchard)
                }).ToList(),
                Home = dashboard.Home,
                Orchard = dashboard.Orchard,
                Nutrient = dashboard.Nutrient,
                PestDisease = dashboard.PestDisease,
                Resources = dashboard.Resources,
                Financial = dashboard.Financial,
                Advisory = dashboard.Advisory,
                Alerts = dashboard.Alerts,
                Weather = dashboard.Weather
            };
        }
    }
}
