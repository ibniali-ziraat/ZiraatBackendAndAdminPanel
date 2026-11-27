using System.Threading.Tasks;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [ApiController]
    [Route("api/vendor-dashboard")]
    [Authorize(Roles = "admin, vendor, Vendor")]
    public class VendorDashboardController : ControllerBase
    {
        private readonly IVendorDashboardService _vendorDashboardService;
        private readonly IWorkContext _workContext;

        public VendorDashboardController(
            IVendorDashboardService vendorDashboardService,
            IWorkContext workContext)
        {
            _vendorDashboardService = vendorDashboardService;
            _workContext = workContext;
        }

        [HttpGet("{vendorId:long}")]
        public async Task<IActionResult> GetDashboard(long vendorId)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            return Ok(await _vendorDashboardService.GetDashboard(vendorId));
        }

        [HttpGet("{vendorId:long}/home")]
        public async Task<IActionResult> GetHome(long vendorId)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            return Ok(await _vendorDashboardService.GetHome(vendorId));
        }

        [HttpGet("{vendorId:long}/inventory")]
        public async Task<IActionResult> GetInventory(long vendorId)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            return Ok(await _vendorDashboardService.GetInventory(vendorId));
        }

        [HttpGet("{vendorId:long}/sales")]
        public async Task<IActionResult> GetSales(long vendorId)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            return Ok(await _vendorDashboardService.GetSales(vendorId));
        }

        [HttpGet("{vendorId:long}/credit")]
        public async Task<IActionResult> GetCredit(long vendorId)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            return Ok(await _vendorDashboardService.GetCredit(vendorId));
        }

        [HttpGet("{vendorId:long}/fees")]
        public async Task<IActionResult> GetFees(long vendorId)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            return Ok(await _vendorDashboardService.GetFees(vendorId));
        }

        [HttpGet("{vendorId:long}/reports")]
        public async Task<IActionResult> GetReports(long vendorId)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            return Ok(await _vendorDashboardService.GetReports(vendorId));
        }

        [HttpGet("{vendorId:long}/offers")]
        public async Task<IActionResult> GetOffers(long vendorId)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            return Ok(await _vendorDashboardService.GetOffers(vendorId));
        }

        [HttpGet("{vendorId:long}/offers/types")]
        public async Task<IActionResult> GetOfferTypes(long vendorId)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            var offers = await _vendorDashboardService.GetOffers(vendorId);
            return Ok(offers.VendorSpecificOffers);
        }

        [HttpGet("{vendorId:long}/offers/vendor")]
        public async Task<IActionResult> GetVendorOffers(long vendorId, [FromQuery] string offerType = null, [FromQuery] bool includeInactive = false)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            return Ok(await _vendorDashboardService.GetVendorOffers(vendorId, offerType, includeInactive));
        }

        [HttpGet("{vendorId:long}/offers/vendor/{offerId:long}")]
        public async Task<IActionResult> GetVendorOffer(long vendorId, long offerId)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            try
            {
                return Ok(await _vendorDashboardService.GetVendorOffer(vendorId, offerId));
            }
            catch (System.InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost("{vendorId:long}/offers/vendor")]
        public async Task<IActionResult> CreateVendorOffer(long vendorId, [FromBody] VendorOfferFormVm model)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                var offer = await _vendorDashboardService.CreateVendorOffer(vendorId, model, currentUser.Id);
                return Ok(offer);
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{vendorId:long}/offers/vendor/{offerId:long}")]
        public async Task<IActionResult> UpdateVendorOffer(long vendorId, long offerId, [FromBody] VendorOfferFormVm model)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                return Ok(await _vendorDashboardService.UpdateVendorOffer(vendorId, offerId, model, currentUser.Id));
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (System.InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{vendorId:long}/offers/vendor/{offerId:long}")]
        public async Task<IActionResult> DeleteVendorOffer(long vendorId, long offerId)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                await _vendorDashboardService.DeleteVendorOffer(vendorId, offerId, currentUser.Id);
                return NoContent();
            }
            catch (System.InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost("{vendorId:long}/offers/vendor/{offerId:long}/activate")]
        public async Task<IActionResult> ActivateVendorOffer(long vendorId, long offerId)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                return Ok(await _vendorDashboardService.SetVendorOfferStatus(vendorId, offerId, true, currentUser.Id));
            }
            catch (System.InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost("{vendorId:long}/offers/vendor/{offerId:long}/deactivate")]
        public async Task<IActionResult> DeactivateVendorOffer(long vendorId, long offerId)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                return Ok(await _vendorDashboardService.SetVendorOfferStatus(vendorId, offerId, false, currentUser.Id));
            }
            catch (System.InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("{vendorId:long}/offers/platform")]
        public async Task<IActionResult> GetPlatformOffers(long vendorId)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            return Ok(_vendorDashboardService.GetPlatformOffers(vendorId));
        }

        [HttpGet("{vendorId:long}/offers/platform/{code}")]
        public async Task<IActionResult> GetPlatformOffer(long vendorId, string code)
        {
            var access = await ValidateVendorAccess(vendorId);
            if (access != null)
            {
                return access;
            }

            var offer = _vendorDashboardService.GetPlatformOffer(vendorId, code);
            if (offer == null)
            {
                return NotFound(new { error = "Platform offer not found" });
            }

            return Ok(offer);
        }

        private async Task<IActionResult> ValidateVendorAccess(long vendorId)
        {
            var currentUser = await _workContext.GetCurrentUser();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
            {
                return Forbid("You don't have permission to access this vendor dashboard");
            }

            return null;
        }
    }
}
