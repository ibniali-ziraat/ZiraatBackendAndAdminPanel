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
[Route("api/core/orchard/{orchardProfileId:long}/production")]
[ApiController]
[OrchardAccess]
[Authorize]
public class OrchardProductionController : ControllerBase
{
    private readonly IOrchardProductionService _productionService;
    private readonly IOrchardProfileService _profileService;
    private readonly ILogger<OrchardProductionController> _logger;

    public OrchardProductionController(
        IOrchardProductionService productionService,
        IOrchardProfileService profileService,
        ILogger<OrchardProductionController> logger)
    {
        _productionService = productionService;
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Get production data for an orchard profile
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(long orchardProfileId)
    {
        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var production = await _productionService.GetByOrchardProfileIdAsync(orchardProfileId);
            if (production == null)
                return NotFound(new { message = "Production data not found" });

            return Ok(MapToDto(production));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving production data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create or update production data for an orchard profile
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate(long orchardProfileId, [FromBody] ProductionCreateDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var production = new OrchardProduction
            {
                AverageAnnualYield = model.AverageAnnualYield,
                YieldUnit = model.YieldUnit,
                PeakHarvestFromMonth = model.PeakHarvestFromMonth,
                PeakHarvestToMonth = model.PeakHarvestToMonth,
                SellingPreference = model.SellingPreference,
                BuyerName = model.BuyerName,
                BuyerContact = model.BuyerContact
            };

            var result = await _productionService.CreateOrUpdateAsync(orchardProfileId, production);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            var created = await _productionService.GetByOrchardProfileIdAsync(orchardProfileId);
            _logger.LogInformation("Production data created/updated for Orchard Profile: {ProfileId}", orchardProfileId);

            return Ok(new { success = true, data = MapToDto(created) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating production data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Partially update production data
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> PartialUpdate(long orchardProfileId, [FromBody] ProductionUpdateDto model)
    {
        try
        {
            var existing = await _productionService.GetByOrchardProfileIdAsync(orchardProfileId);
            if (existing == null)
                return NotFound(new { message = "Production data not found" });

            if (model.AverageAnnualYield.HasValue && model.AverageAnnualYield > 0)
                existing.AverageAnnualYield = model.AverageAnnualYield.Value;

            if (!string.IsNullOrWhiteSpace(model.YieldUnit))
                existing.YieldUnit = model.YieldUnit;

            if (!string.IsNullOrWhiteSpace(model.PeakHarvestFromMonth))
                existing.PeakHarvestFromMonth = model.PeakHarvestFromMonth;

            if (!string.IsNullOrWhiteSpace(model.PeakHarvestToMonth))
                existing.PeakHarvestToMonth = model.PeakHarvestToMonth;

            if (!string.IsNullOrWhiteSpace(model.SellingPreference))
                existing.SellingPreference = model.SellingPreference;

            if (!string.IsNullOrWhiteSpace(model.BuyerName))
                existing.BuyerName = model.BuyerName;

            if (!string.IsNullOrWhiteSpace(model.BuyerContact))
                existing.BuyerContact = model.BuyerContact;

            var result = await _productionService.CreateOrUpdateAsync(orchardProfileId, existing);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            var updated = await _productionService.GetByOrchardProfileIdAsync(orchardProfileId);
            _logger.LogInformation("Production data partially updated for Orchard Profile: {ProfileId}", orchardProfileId);

            return Ok(new { success = true, data = MapToDto(updated) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error partially updating production data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete production data for an orchard profile
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(long orchardProfileId)
    {
        try
        {
            var success = await _productionService.DeleteAsync(orchardProfileId);
            if (!success)
                return NotFound(new { message = "Production data not found" });

            _logger.LogInformation("Production data deleted for Orchard Profile: {ProfileId}", orchardProfileId);
            return Ok(new { success = true, message = "Production data deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting production data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    private ProductionDto MapToDto(OrchardProduction production)
    {
        return new ProductionDto
        {
            AverageAnnualYield = production.AverageAnnualYield,
            YieldUnit = production.YieldUnit,
            PeakHarvestFromMonth = production.PeakHarvestFromMonth,
            PeakHarvestToMonth = production.PeakHarvestToMonth,
            SellingPreference = production.SellingPreference,
            BuyerName = production.BuyerName,
            BuyerContact = production.BuyerContact
        };
    }
}
