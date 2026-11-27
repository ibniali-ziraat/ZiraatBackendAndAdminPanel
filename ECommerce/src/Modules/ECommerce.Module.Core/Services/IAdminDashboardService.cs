using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;

namespace ECommerce.Module.Core.Services
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardVm> GetDashboard();
    }
}
