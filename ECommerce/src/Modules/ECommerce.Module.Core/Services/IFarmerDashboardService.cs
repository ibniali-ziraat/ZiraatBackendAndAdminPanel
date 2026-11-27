using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;

namespace ECommerce.Module.Core.Services
{
    public interface IFarmerDashboardService
    {
        Task<FarmerAggregateDashboardVm> GetAggregate(long userId, int? year = null);

        Task<FarmerWeatherDashboardVm> GetWeather(long userId, long? orchardProfileId = null);

        Task<FarmerDashboardVm> GetDashboard(long userId, long? orchardProfileId = null, int? year = null);

        Task<FarmerDashboardHomeVm> GetHome(long userId, long? orchardProfileId = null, int? year = null);

        Task<FarmerOrchardDashboardVm> GetOrchard(long userId, long? orchardProfileId = null, int? year = null);

        Task<FarmerNutrientDashboardVm> GetNutrient(long userId, long? orchardProfileId = null, int? year = null);

        Task<FarmerPestDiseaseDashboardVm> GetPestDisease(long userId, long? orchardProfileId = null, int? year = null);

        Task<FarmerResourceUsageDashboardVm> GetResources(long userId, long? orchardProfileId = null, int? year = null);

        Task<FarmerFinancialDashboardVm> GetFinancial(long userId, long? orchardProfileId = null, int? year = null);

        Task<FarmerAdvisoryDashboardVm> GetAdvisory(long userId, long? orchardProfileId = null, int? year = null);

        Task<FarmerAlertsDashboardVm> GetAlerts(long userId, long? orchardProfileId = null, int? year = null);
    }
}
