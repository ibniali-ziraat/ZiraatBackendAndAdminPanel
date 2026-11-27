using System;
using System.Text.Json;
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
[Route("api/core/orchard/{orchardProfileId:long}/soil-water")]
[ApiController]
[OrchardAccess]
[Authorize]
public class OrchardSoilAndWaterController : ControllerBase
{
    private readonly IOrchardSoilAndWaterService _soilWaterService;
    private readonly IOrchardProfileService _profileService;
    private readonly ILogger<OrchardSoilAndWaterController> _logger;

    public OrchardSoilAndWaterController(
        IOrchardSoilAndWaterService soilWaterService,
        IOrchardProfileService profileService,
        ILogger<OrchardSoilAndWaterController> logger)
    {
        _soilWaterService = soilWaterService;
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Get soil and water data for an orchard profile
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(long orchardProfileId)
    {
        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var soilWater = await _soilWaterService.GetByOrchardProfileIdAsync(orchardProfileId);
            if (soilWater == null)
                return NotFound(new { message = "Soil and water data not found" });

            return Ok(MapToDto(soilWater));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving soil and water data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create or update soil and water data for an orchard profile
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate(long orchardProfileId, [FromBody] SoilAndWaterCreateDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var soilWater = new OrchardSoilAndWater
            {
                SoilType = model.SoilType,
                SoilPh = model.SoilPh,
                SoilNitrogen = model.SoilNitrogen,
                SoilPhosphorus = model.SoilPhosphorus,
                SoilPotassium = model.SoilPotassium,
                SoilOrganicCarbon = model.SoilOrganicCarbon,
                SoilMicronutrients = model.SoilMicronutrients,
                SoilTestReportUrls = SerializeArray(model.SoilTestReportUrls),
                SoilHealthStatus = SerializeArray(model.SoilHealthStatus),
                LastSoilTestDate = model.LastSoilTestDate,
                WaterSource = model.WaterSource,
                IrrigationMethod = model.IrrigationMethod,
                WaterAvailability = model.WaterAvailability
            };

            var result = await _soilWaterService.CreateOrUpdateAsync(orchardProfileId, soilWater);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            var created = await _soilWaterService.GetByOrchardProfileIdAsync(orchardProfileId);
            _logger.LogInformation("Soil and water data created/updated for Orchard Profile: {ProfileId}", orchardProfileId);

            return Ok(new { success = true, data = MapToDto(created) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating soil and water data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Partially update soil and water data
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> PartialUpdate(long orchardProfileId, [FromBody] SoilAndWaterUpdateDto model)
    {
        try
        {
            var existing = await _soilWaterService.GetByOrchardProfileIdAsync(orchardProfileId);
            if (existing == null)
                return NotFound(new { message = "Soil and water data not found" });

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(model.SoilType))
                existing.SoilType = model.SoilType;

            if (model.SoilPh.HasValue)
                existing.SoilPh = model.SoilPh;

            if (!string.IsNullOrWhiteSpace(model.SoilNitrogen))
                existing.SoilNitrogen = model.SoilNitrogen;

            if (!string.IsNullOrWhiteSpace(model.SoilPhosphorus))
                existing.SoilPhosphorus = model.SoilPhosphorus;

            if (!string.IsNullOrWhiteSpace(model.SoilPotassium))
                existing.SoilPotassium = model.SoilPotassium;

            if (!string.IsNullOrWhiteSpace(model.SoilOrganicCarbon))
                existing.SoilOrganicCarbon = model.SoilOrganicCarbon;

            if (!string.IsNullOrWhiteSpace(model.SoilMicronutrients))
                existing.SoilMicronutrients = model.SoilMicronutrients;

            if (model.SoilTestReportUrls != null)
                existing.SoilTestReportUrls = SerializeArray(model.SoilTestReportUrls);

            if (model.SoilHealthStatus != null)
                existing.SoilHealthStatus = SerializeArray(model.SoilHealthStatus);

            if (model.LastSoilTestDate.HasValue)
                existing.LastSoilTestDate = model.LastSoilTestDate;

            if (!string.IsNullOrWhiteSpace(model.WaterSource))
                existing.WaterSource = model.WaterSource;

            if (!string.IsNullOrWhiteSpace(model.IrrigationMethod))
                existing.IrrigationMethod = model.IrrigationMethod;

            if (!string.IsNullOrWhiteSpace(model.WaterAvailability))
                existing.WaterAvailability = model.WaterAvailability;

            var result = await _soilWaterService.CreateOrUpdateAsync(orchardProfileId, existing);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            var updated = await _soilWaterService.GetByOrchardProfileIdAsync(orchardProfileId);
            _logger.LogInformation("Soil and water data partially updated for Orchard Profile: {ProfileId}", orchardProfileId);

            return Ok(new { success = true, data = MapToDto(updated) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error partially updating soil and water data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete soil and water data for an orchard profile
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(long orchardProfileId)
    {
        try
        {
            var success = await _soilWaterService.DeleteAsync(orchardProfileId);

            if (!success)
                return NotFound(new { message = "Soil and water data not found" });

            _logger.LogInformation("Soil and water data deleted for Orchard Profile: {ProfileId}", orchardProfileId);
            return Ok(new { success = true, message = "Soil and water data deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting soil and water data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    private SoilAndWaterDto MapToDto(OrchardSoilAndWater soilWater)
    {
        return new SoilAndWaterDto
        {
            SoilType = soilWater.SoilType,
            SoilPh = soilWater.SoilPh,
            SoilNitrogen = soilWater.SoilNitrogen,
            SoilPhosphorus = soilWater.SoilPhosphorus,
            SoilPotassium = soilWater.SoilPotassium,
            SoilOrganicCarbon = soilWater.SoilOrganicCarbon,
            SoilMicronutrients = soilWater.SoilMicronutrients,
            SoilTestReportUrls = DeserializeArray(soilWater.SoilTestReportUrls),
            SoilHealthStatus = DeserializeArray(soilWater.SoilHealthStatus),
            LastSoilTestDate = soilWater.LastSoilTestDate,
            WaterSource = soilWater.WaterSource,
            IrrigationMethod = soilWater.IrrigationMethod,
            WaterAvailability = soilWater.WaterAvailability
        };
    }

    private string SerializeArray(string[] array)
    {
        if (array == null || array.Length == 0)
            return JsonSerializer.Serialize(new string[] { });

        return JsonSerializer.Serialize(array);
    }

    private string[] DeserializeArray(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new string[] { };

        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? new string[] { };
        }
        catch (JsonException)
        {
            return new string[] { };
        }
    }
}
