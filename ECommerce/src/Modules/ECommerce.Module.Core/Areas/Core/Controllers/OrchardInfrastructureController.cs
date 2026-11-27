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
[Route("api/core/orchard/{orchardProfileId:long}/infrastructure")]
[ApiController]
[OrchardAccess]
[Authorize]
public class OrchardInfrastructureController : ControllerBase
{
    private readonly IOrchardInfrastructureService _infrastructureService;
    private readonly IOrchardProfileService _profileService;
    private readonly ILogger<OrchardInfrastructureController> _logger;

    public OrchardInfrastructureController(
        IOrchardInfrastructureService infrastructureService,
        IOrchardProfileService profileService,
        ILogger<OrchardInfrastructureController> logger)
    {
        _infrastructureService = infrastructureService;
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Get infrastructure data for an orchard profile
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(long orchardProfileId)
    {
        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var infrastructure = await _infrastructureService.GetByOrchardProfileIdAsync(orchardProfileId);
            if (infrastructure == null)
                return NotFound(new { message = "Infrastructure data not found" });

            return Ok(MapToDto(infrastructure));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving infrastructure data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create or update infrastructure data for an orchard profile
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate(long orchardProfileId, [FromBody] InfrastructureDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var infrastructure = new OrchardInfrastructure
            {
                EquipmentOwned = SerializeArray(model.EquipmentOwned),
                StorageFacilities = SerializeArray(model.StorageFacilities),
                OtherTools = SerializeArray(model.OtherTools),
                PermanentLabor = model.PermanentLabor,
                SeasonalLabor = model.SeasonalLabor,
                ContractBasedLabor = model.ContractBasedLabor
            };

            var result = await _infrastructureService.CreateOrUpdateAsync(orchardProfileId, infrastructure);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            var created = await _infrastructureService.GetByOrchardProfileIdAsync(orchardProfileId);
            _logger.LogInformation("Infrastructure data created/updated for Orchard Profile: {ProfileId}", orchardProfileId);

            return Ok(new { success = true, data = MapToDto(created) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating infrastructure data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Partially update infrastructure data
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> PartialUpdate(long orchardProfileId, [FromBody] InfrastructureDto model)
    {
        try
        {
            var existing = await _infrastructureService.GetByOrchardProfileIdAsync(orchardProfileId);
            if (existing == null)
                return NotFound(new { message = "Infrastructure data not found" });

            if (model.EquipmentOwned != null && model.EquipmentOwned.Length > 0)
                existing.EquipmentOwned = SerializeArray(model.EquipmentOwned);

            if (model.StorageFacilities != null && model.StorageFacilities.Length > 0)
                existing.StorageFacilities = SerializeArray(model.StorageFacilities);

            if (model.OtherTools != null && model.OtherTools.Length > 0)
                existing.OtherTools = SerializeArray(model.OtherTools);

            if (model.PermanentLabor > 0)
                existing.PermanentLabor = model.PermanentLabor;

            if (model.SeasonalLabor > 0)
                existing.SeasonalLabor = model.SeasonalLabor;

            if (model.ContractBasedLabor > 0)
                existing.ContractBasedLabor = model.ContractBasedLabor;

            var result = await _infrastructureService.CreateOrUpdateAsync(orchardProfileId, existing);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            var updated = await _infrastructureService.GetByOrchardProfileIdAsync(orchardProfileId);
            _logger.LogInformation("Infrastructure data partially updated for Orchard Profile: {ProfileId}", orchardProfileId);

            return Ok(new { success = true, data = MapToDto(updated) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error partially updating infrastructure data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete infrastructure data for an orchard profile
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(long orchardProfileId)
    {
        try
        {
            var success = await _infrastructureService.DeleteAsync(orchardProfileId);
            if (!success)
                return NotFound(new { message = "Infrastructure data not found" });

            _logger.LogInformation("Infrastructure data deleted for Orchard Profile: {ProfileId}", orchardProfileId);
            return Ok(new { success = true, message = "Infrastructure data deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting infrastructure data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    private InfrastructureDto MapToDto(OrchardInfrastructure infrastructure)
    {
        return new InfrastructureDto
        {
            EquipmentOwned = DeserializeArray(infrastructure.EquipmentOwned),
            StorageFacilities = DeserializeArray(infrastructure.StorageFacilities),
            OtherTools = DeserializeArray(infrastructure.OtherTools),
            PermanentLabor = infrastructure.PermanentLabor,
            SeasonalLabor = infrastructure.SeasonalLabor,
            ContractBasedLabor = infrastructure.ContractBasedLabor
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
