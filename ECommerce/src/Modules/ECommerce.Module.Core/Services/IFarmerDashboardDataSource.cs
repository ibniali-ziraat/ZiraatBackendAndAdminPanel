using System;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;

namespace ECommerce.Module.Core.Services
{
    public interface IFarmerAdvisoryDataSource
    {
        Task<FarmerAdvisoryDashboardVm> GetAdvisory(long userId, DateTimeOffset from, DateTimeOffset to);
    }

    public interface IFarmerAlertDataSource
    {
        Task<FarmerAlertsDashboardVm> GetAlerts(long userId, DateTimeOffset from, DateTimeOffset to);
    }
}
