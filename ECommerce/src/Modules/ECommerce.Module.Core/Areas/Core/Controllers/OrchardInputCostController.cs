using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Module.Core.Extensions;
using Microsoft.Extensions.Logging;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Core.Areas.Core.Controllers;

[Area("Core")]
[Route("api/core/orchard/{orchardProfileId:long}/input-cost")]
[ApiController]
[OrchardAccess]
[Authorize]
public class OrchardInputCostController : ControllerBase
{
    private readonly IOrchardInputCostService _inputCostService;
    private readonly IOrchardProfileService _profileService;
    private readonly ILogger<OrchardInputCostController> _logger;

    public OrchardInputCostController(
        IOrchardInputCostService inputCostService,
        IOrchardProfileService profileService,
        ILogger<OrchardInputCostController> logger)
    {
        _inputCostService = inputCostService;
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Get input cost data for an orchard profile
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(long orchardProfileId)
    {
        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var inputCost = await _inputCostService.GetByOrchardProfileIdAsync(orchardProfileId);
            if (inputCost == null)
                return NotFound(new { message = "Input cost data not found" });

            return Ok(MapToDto(inputCost));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving input cost data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create or update input cost data for an orchard profile
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate(long orchardProfileId, [FromBody] InputCostDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var inputCost = new OrchardInputCost
            {
                PesticideSupplierLocation = model.PesticideSupplierLocation,
                PaymentType = model.PaymentType,
                CreditPeriod = model.CreditPeriod
            };

            var result = await _inputCostService.CreateOrUpdateAsync(orchardProfileId, inputCost);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            var created = await _inputCostService.GetByOrchardProfileIdAsync(orchardProfileId);
            _logger.LogInformation("Input cost data created/updated for Orchard Profile: {ProfileId}", orchardProfileId);

            return Ok(new { success = true, data = MapToDto(created) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating input cost data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Partially update input cost data
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> PartialUpdate(long orchardProfileId, [FromBody] InputCostDto model)
    {
        try
        {
            var existing = await _inputCostService.GetByOrchardProfileIdAsync(orchardProfileId);
            if (existing == null)
                return NotFound(new { message = "Input cost data not found" });

            if (!string.IsNullOrWhiteSpace(model.PesticideSupplierLocation))
                existing.PesticideSupplierLocation = model.PesticideSupplierLocation;

            if (!string.IsNullOrWhiteSpace(model.PaymentType))
                existing.PaymentType = model.PaymentType;

            if (!string.IsNullOrWhiteSpace(model.CreditPeriod))
                existing.CreditPeriod = model.CreditPeriod;

            var result = await _inputCostService.CreateOrUpdateAsync(orchardProfileId, existing);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            var updated = await _inputCostService.GetByOrchardProfileIdAsync(orchardProfileId);
            _logger.LogInformation("Input cost data partially updated for Orchard Profile: {ProfileId}", orchardProfileId);

            return Ok(new { success = true, data = MapToDto(updated) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error partially updating input cost data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete input cost data for an orchard profile
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(long orchardProfileId)
    {
        try
        {
            var success = await _inputCostService.DeleteAsync(orchardProfileId);
            if (!success)
                return NotFound(new { message = "Input cost data not found" });

            _logger.LogInformation("Input cost data deleted for Orchard Profile: {ProfileId}", orchardProfileId);
            return Ok(new { success = true, message = "Input cost data deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting input cost data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    private InputCostDto MapToDto(OrchardInputCost inputCost)
    {
        return new InputCostDto
        {
            PesticideSupplierLocation = inputCost.PesticideSupplierLocation,
            PaymentType = inputCost.PaymentType,
            CreditPeriod = inputCost.CreditPeriod
        };
    }
}
