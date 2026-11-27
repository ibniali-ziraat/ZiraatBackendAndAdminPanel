using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [Authorize(Roles = "admin, vendor")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DashboardController : Controller
    {
        private readonly IAdminDashboardService _adminDashboardService;

        public DashboardController(IAdminDashboardService adminDashboardService)
        {
            _adminDashboardService = adminDashboardService;
        }

        [HttpGet("api/admin/dashboard")]
        [Authorize(AuthenticationSchemes = "Identity.Application", Roles = "admin")]
        [ApiExplorerSettings(IgnoreApi = false)]
        public async Task<IActionResult> Get()
        {
            return Ok(await _adminDashboardService.GetDashboard());
        }

        [Route("admin/dashboard-tpl")]
        public IActionResult HomeTemplate()
        {
            return View();
        }
    }
}
