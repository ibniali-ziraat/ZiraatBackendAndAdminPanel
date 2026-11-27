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
[Route("api/core/orchard/{orchardProfileId:long}/optional-fields")]
[ApiController]
[OrchardAccess]
[Authorize]
public class OrchardOptionalFieldsController : ControllerBase
{
    private readonly IOrchardOptionalFieldsService _optionalFieldsService;
    private readonly IOrchardProfileService _profileService;
    private readonly ILogger<OrchardOptionalFieldsController> _logger;

    public OrchardOptionalFieldsController(
        IOrchardOptionalFieldsService optionalFieldsService,
        IOrchardProfileService profileService,
        ILogger<OrchardOptionalFieldsController> logger)
    {
        _optionalFieldsService = optionalFieldsService;
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Get optional fields data for an orchard profile
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(long orchardProfileId)
    {
        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var optionalFields = await _optionalFieldsService.GetByOrchardProfileIdAsync(orchardProfileId);
            if (optionalFields == null)
                return NotFound(new { message = "Optional fields data not found" });

            return Ok(MapToDto(optionalFields));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving optional fields data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create or update optional fields data for an orchard profile
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate(long orchardProfileId, [FromBody] OptionalFieldsDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var optionalFields = new OrchardOptionalFields
            {
                GovernmentSchemesAvailed = SerializeArray(model.GovernmentSchemesAvailed),
                IsFpoOrCooperativeMember = model.IsFpoOrCooperativeMember,
                Certifications = SerializeArray(model.Certifications),
                DigitalToolsUsed = SerializeArray(model.DigitalToolsUsed)
            };

            var result = await _optionalFieldsService.CreateOrUpdateAsync(orchardProfileId, optionalFields);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            var created = await _optionalFieldsService.GetByOrchardProfileIdAsync(orchardProfileId);
            _logger.LogInformation("Optional fields data created/updated for Orchard Profile: {ProfileId}", orchardProfileId);

            return Ok(new { success = true, data = MapToDto(created) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating optional fields data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Partially update optional fields data
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> PartialUpdate(long orchardProfileId, [FromBody] OptionalFieldsDto model)
    {
        try
        {
            var existing = await _optionalFieldsService.GetByOrchardProfileIdAsync(orchardProfileId);
            if (existing == null)
                return NotFound(new { message = "Optional fields data not found" });

            if (model.GovernmentSchemesAvailed != null && model.GovernmentSchemesAvailed.Length > 0)
                existing.GovernmentSchemesAvailed = SerializeArray(model.GovernmentSchemesAvailed);

            if (model.Certifications != null && model.Certifications.Length > 0)
                existing.Certifications = SerializeArray(model.Certifications);

            if (model.DigitalToolsUsed != null && model.DigitalToolsUsed.Length > 0)
                existing.DigitalToolsUsed = SerializeArray(model.DigitalToolsUsed);

           // if (model.IsFpoOrCooperativeMember)
                existing.IsFpoOrCooperativeMember = model.IsFpoOrCooperativeMember;

            var result = await _optionalFieldsService.CreateOrUpdateAsync(orchardProfileId, existing);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            var updated = await _optionalFieldsService.GetByOrchardProfileIdAsync(orchardProfileId);
            _logger.LogInformation("Optional fields data partially updated for Orchard Profile: {ProfileId}", orchardProfileId);

            return Ok(new { success = true, data = MapToDto(updated) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error partially updating optional fields data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete optional fields data for an orchard profile
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(long orchardProfileId)
    {
        try
        {
            var success = await _optionalFieldsService.DeleteAsync(orchardProfileId);
            if (!success)
                return NotFound(new { message = "Optional fields data not found" });

            _logger.LogInformation("Optional fields data deleted for Orchard Profile: {ProfileId}", orchardProfileId);
            return Ok(new { success = true, message = "Optional fields data deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting optional fields data for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    private OptionalFieldsDto MapToDto(OrchardOptionalFields optionalFields)
    {
        return new OptionalFieldsDto
        {
            GovernmentSchemesAvailed = DeserializeArray(optionalFields.GovernmentSchemesAvailed),
            IsFpoOrCooperativeMember = optionalFields.IsFpoOrCooperativeMember,
            Certifications = DeserializeArray(optionalFields.Certifications),
            DigitalToolsUsed = DeserializeArray(optionalFields.DigitalToolsUsed)
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
