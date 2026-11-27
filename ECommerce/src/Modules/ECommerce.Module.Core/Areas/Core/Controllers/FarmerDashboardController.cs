using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [ApiController]
    [Route("api/core/farmer-dashboard")]
    [Authorize]
    public class FarmerDashboardController : ControllerBase, IAsyncActionFilter
    {
        private readonly IFarmerDashboardService _farmerDashboardService;

        public FarmerDashboardController(IFarmerDashboardService farmerDashboardService)
        {
            _farmerDashboardService = farmerDashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard([FromQuery, Range(1, long.MaxValue)] long? orchardProfileId = null, [FromQuery, Range(2000, 2100)] int? year = null)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "User authentication failed" });
            }

            return Ok(await _farmerDashboardService.GetDashboard(userId, orchardProfileId, year));
        }

        /// <summary>
        /// Complete dashboard, headline totals and orchard list for all orchards owned by the authenticated farmer.
        /// Use the existing section endpoints with orchardProfileId for drill-downs.
        /// </summary>
        [HttpGet("aggregate")]
        [ProducesResponseType(typeof(FarmerAggregateDashboardVm), 200)]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetAggregate([FromQuery, Range(2000, 2100)] int? year = null)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "User authentication failed" });
            }

            return Ok(await _farmerDashboardService.GetAggregate(userId, year));
        }

        [HttpGet("home")]
        public async Task<IActionResult> GetHome([FromQuery, Range(1, long.MaxValue)] long? orchardProfileId = null, [FromQuery, Range(2000, 2100)] int? year = null)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "User authentication failed" });
            }

            return Ok(await _farmerDashboardService.GetHome(userId, orchardProfileId, year));
        }

        [HttpGet("orchard")]
        public async Task<IActionResult> GetOrchard([FromQuery, Range(1, long.MaxValue)] long? orchardProfileId = null, [FromQuery, Range(2000, 2100)] int? year = null)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "User authentication failed" });
            }

            return Ok(await _farmerDashboardService.GetOrchard(userId, orchardProfileId, year));
        }

        [HttpGet("nutrient")]
        public async Task<IActionResult> GetNutrient([FromQuery, Range(1, long.MaxValue)] long? orchardProfileId = null, [FromQuery, Range(2000, 2100)] int? year = null)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "User authentication failed" });
            }

            return Ok(await _farmerDashboardService.GetNutrient(userId, orchardProfileId, year));
        }

        [HttpGet("pest-disease")]
        public async Task<IActionResult> GetPestDisease([FromQuery, Range(1, long.MaxValue)] long? orchardProfileId = null, [FromQuery, Range(2000, 2100)] int? year = null)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "User authentication failed" });
            }

            return Ok(await _farmerDashboardService.GetPestDisease(userId, orchardProfileId, year));
        }

        [HttpGet("resources")]
        public async Task<IActionResult> GetResources([FromQuery, Range(1, long.MaxValue)] long? orchardProfileId = null, [FromQuery, Range(2000, 2100)] int? year = null)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "User authentication failed" });
            }

            return Ok(await _farmerDashboardService.GetResources(userId, orchardProfileId, year));
        }

        [HttpGet("financial")]
        public async Task<IActionResult> GetFinancial([FromQuery, Range(1, long.MaxValue)] long? orchardProfileId = null, [FromQuery, Range(2000, 2100)] int? year = null)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "User authentication failed" });
            }

            return Ok(await _farmerDashboardService.GetFinancial(userId, orchardProfileId, year));
        }

        [HttpGet("advisory")]
        public async Task<IActionResult> GetAdvisory([FromQuery, Range(1, long.MaxValue)] long? orchardProfileId = null, [FromQuery, Range(2000, 2100)] int? year = null)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "User authentication failed" });
            }

            return Ok(await _farmerDashboardService.GetAdvisory(userId, orchardProfileId, year));
        }

        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts([FromQuery, Range(1, long.MaxValue)] long? orchardProfileId = null, [FromQuery, Range(2000, 2100)] int? year = null)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "User authentication failed" });
            }

            return Ok(await _farmerDashboardService.GetAlerts(userId, orchardProfileId, year));
        }

        [HttpGet("weather")]
        public async Task<IActionResult> GetWeather([FromQuery, Range(1, long.MaxValue)] long? orchardProfileId = null)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            return Ok(await _farmerDashboardService.GetWeather(userId, orchardProfileId));
        }

        [NonAction]
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next();
            if (executed.Exception is KeyNotFoundException)
            {
                executed.Result = new NotFoundObjectResult(new { message = "Orchard not found." });
                executed.ExceptionHandled = true;
            }
        }

        private bool TryGetCurrentUserId(out long userId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return long.TryParse(userIdValue, out userId) && userId > 0;
        }
    }
}
