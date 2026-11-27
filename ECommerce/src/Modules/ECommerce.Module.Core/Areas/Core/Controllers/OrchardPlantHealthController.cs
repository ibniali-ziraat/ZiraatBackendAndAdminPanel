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
[Route("api/core/orchard/{orchardProfileId:long}/plant-health")]
[ApiController]
[OrchardAccess]
[Authorize]
public class OrchardPlantHealthController : ControllerBase
{
    private readonly IOrchardPlantHealthService _plantHealthService;
    private readonly IOrchardProfileService _profileService;
    private readonly ILogger<OrchardPlantHealthController> _logger;

    public OrchardPlantHealthController(
        IOrchardPlantHealthService plantHealthService,
        IOrchardProfileService profileService,
        ILogger<OrchardPlantHealthController> logger)
    {
        _plantHealthService = plantHealthService;
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Get plant health data for an orchard profile
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(long orchardProfileId)
    {
        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var plantHealth = await _plantHealthService.GetByOrchardProfileIdAsync(orchardProfileId);
            if (plantHealth == null)
                return NotFound(new { message = "Plant health data not found" });

            return Ok(MapToDto(plantHealth));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving plant health data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create or update plant health data for an orchard profile
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate(long orchardProfileId, [FromBody] PlantHealthDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var plantHealth = new OrchardPlantHealth
            {
                DiseaseHistory = SerializeArray(model.DiseaseHistory),
                PestHistory = SerializeArray(model.PestHistory),
                SpraySchedule = SerializeArray(model.SpraySchedule),
                IsCropInsuranceCovered = model.IsCropInsuranceCovered
            };

            var result = await _plantHealthService.CreateOrUpdateAsync(orchardProfileId, plantHealth);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            var created = await _plantHealthService.GetByOrchardProfileIdAsync(orchardProfileId);
            _logger.LogInformation("Plant health data created/updated for Orchard Profile: {ProfileId}", orchardProfileId);

            return Ok(new { success = true, data = MapToDto(created) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating plant health data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Partially update plant health data
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> PartialUpdate(long orchardProfileId, [FromBody] PlantHealthDto model)
    {
        try
        {
            var existing = await _plantHealthService.GetByOrchardProfileIdAsync(orchardProfileId);
            if (existing == null)
                return NotFound(new { message = "Plant health data not found" });

            if (model.DiseaseHistory != null && model.DiseaseHistory.Length > 0)
                existing.DiseaseHistory = SerializeArray(model.DiseaseHistory);

            if (model.PestHistory != null && model.PestHistory.Length > 0)
                existing.PestHistory = SerializeArray(model.PestHistory);

            if (model.SpraySchedule != null && model.SpraySchedule.Length > 0)
                existing.SpraySchedule = SerializeArray(model.SpraySchedule);

            if (model.IsCropInsuranceCovered)
                existing.IsCropInsuranceCovered = model.IsCropInsuranceCovered;

            var result = await _plantHealthService.CreateOrUpdateAsync(orchardProfileId, existing);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            var updated = await _plantHealthService.GetByOrchardProfileIdAsync(orchardProfileId);
            _logger.LogInformation("Plant health data partially updated for Orchard Profile: {ProfileId}", orchardProfileId);

            return Ok(new { success = true, data = MapToDto(updated) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error partially updating plant health data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete plant health data for an orchard profile
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(long orchardProfileId)
    {
        try
        {
            var success = await _plantHealthService.DeleteAsync(orchardProfileId);
            if (!success)
                return NotFound(new { message = "Plant health data not found" });

            _logger.LogInformation("Plant health data deleted for Orchard Profile: {ProfileId}", orchardProfileId);
            return Ok(new { success = true, message = "Plant health data deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting plant health data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    private PlantHealthDto MapToDto(OrchardPlantHealth plantHealth)
    {
        return new PlantHealthDto
        {
            DiseaseHistory = DeserializeArray(plantHealth.DiseaseHistory),
            PestHistory = DeserializeArray(plantHealth.PestHistory),
            SpraySchedule = DeserializeArray(plantHealth.SpraySchedule),
            IsCropInsuranceCovered = plantHealth.IsCropInsuranceCovered
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
