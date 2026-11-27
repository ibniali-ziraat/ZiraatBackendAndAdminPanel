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
[Route("api/core/orchard/{orchardProfileId:long}/crop-nutrition")]
[ApiController]
[OrchardAccess]
[Authorize]
public class OrchardCropNutritionController : ControllerBase
{
    private readonly IOrchardCropNutritionService _cropNutritionService;
    private readonly IOrchardProfileService _profileService;
    private readonly ILogger<OrchardCropNutritionController> _logger;

    public OrchardCropNutritionController(
        IOrchardCropNutritionService cropNutritionService,
        IOrchardProfileService profileService,
        ILogger<OrchardCropNutritionController> logger)
    {
        _cropNutritionService = cropNutritionService;
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Get crop nutrition data for an orchard profile
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(long orchardProfileId)
    {
        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var cropNutrition = await _cropNutritionService.GetByOrchardProfileIdAsync(orchardProfileId);
            if (cropNutrition == null)
                return NotFound(new { message = "Crop nutrition data not found" });

            return Ok(MapToDto(cropNutrition));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving crop nutrition data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create or update crop nutrition data for an orchard profile
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate(long orchardProfileId, [FromBody] CropNutritionCreateDto model)
    {
         if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var cropNutrition = new OrchardCropNutrition
            {
                FertilizerPractices = model.FertilizerPractices,
                HasFertigationFacility = model.HasFertigationFacility,
                NutritionPlan = model.NutritionPlan,
                IsMulchingApplied = model.IsMulchingApplied,
                LastFertilizerApplicationDate = model.LastFertilizerApplicationDate,
                FertilizerApplicationMode = model.FertilizerApplicationMode
            };

            var result = await _cropNutritionService.CreateOrUpdateAsync(orchardProfileId, cropNutrition);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            var created = await _cropNutritionService.GetByOrchardProfileIdAsync(orchardProfileId);
            _logger.LogInformation("Crop nutrition data created/updated for Orchard Profile: {ProfileId}", orchardProfileId);

            return Ok(new { success = true, data = MapToDto(created) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating crop nutrition data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Partially update crop nutrition data
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> PartialUpdate(long orchardProfileId, [FromBody] CropNutritionUpdateDto model)
    {
        try
        {
            var existing = await _cropNutritionService.GetByOrchardProfileIdAsync(orchardProfileId);
            if (existing == null)
                return NotFound(new { message = "Crop nutrition data not found" });

            if (!string.IsNullOrWhiteSpace(model.FertilizerPractices))
                existing.FertilizerPractices = model.FertilizerPractices;

            if (model.HasFertigationFacility.HasValue)
                existing.HasFertigationFacility = model.HasFertigationFacility.Value;

            if (!string.IsNullOrWhiteSpace(model.NutritionPlan))
                existing.NutritionPlan = model.NutritionPlan;

            if (model.IsMulchingApplied.HasValue)
                existing.IsMulchingApplied = model.IsMulchingApplied.Value;

            if (model.LastFertilizerApplicationDate.HasValue)
                existing.LastFertilizerApplicationDate = model.LastFertilizerApplicationDate;

            if (!string.IsNullOrWhiteSpace(model.FertilizerApplicationMode))
                existing.FertilizerApplicationMode = model.FertilizerApplicationMode;

            var result = await _cropNutritionService.CreateOrUpdateAsync(orchardProfileId, existing);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            var updated = await _cropNutritionService.GetByOrchardProfileIdAsync(orchardProfileId);
            _logger.LogInformation("Crop nutrition data partially updated for Orchard Profile: {ProfileId}", orchardProfileId);

            return Ok(new { success = true, data = MapToDto(updated) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error partially updating crop nutrition data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete crop nutrition data for an orchard profile
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(long orchardProfileId)
    {
        try
        {
            var success = await _cropNutritionService.DeleteAsync(orchardProfileId);
            if (!success)
                return NotFound(new { message = "Crop nutrition data not found" });

            _logger.LogInformation("Crop nutrition data deleted for Orchard Profile: {ProfileId}", orchardProfileId);
            return Ok(new { success = true, message = "Crop nutrition data deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting crop nutrition data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    private CropNutritionDto MapToDto(OrchardCropNutrition cropNutrition)
    {
        return new CropNutritionDto
        {
            FertilizerPractices = cropNutrition.FertilizerPractices,
            HasFertigation = cropNutrition.HasFertigationFacility,
            NutritionPlan = cropNutrition.NutritionPlan,
            IsMulchingApplied = cropNutrition.IsMulchingApplied,
            LastFertilizerApplicationDate = cropNutrition.LastFertilizerApplicationDate,
            FertilizerApplicationMode = cropNutrition.FertilizerApplicationMode
        };
    }
}
