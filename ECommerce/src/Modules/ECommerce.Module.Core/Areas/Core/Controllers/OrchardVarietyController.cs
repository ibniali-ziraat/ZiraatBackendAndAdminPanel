using System;
using System.Linq;
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
[Route("api/core/orchard/{orchardProfileId:long}/varieties")]
[ApiController]
[OrchardAccess]
[Authorize]
public class OrchardVarietyController : ControllerBase
{
    private readonly IOrchardVarietyService _varietyService;
    private readonly IOrchardProfileService _profileService;
    private readonly ILogger<OrchardVarietyController> _logger;

    public OrchardVarietyController(
        IOrchardVarietyService varietyService,
        IOrchardProfileService profileService,
        ILogger<OrchardVarietyController> logger)
    {
        _varietyService = varietyService;
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Get all varieties for a specific orchard profile
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(long orchardProfileId)
    {
        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var varieties = await _varietyService.GetByOrchardProfileIdAsync(orchardProfileId);
            var dtos = varieties.Select(MapToDto).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving varieties for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a single variety by ID
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long orchardProfileId, long id)
    {
        try
        {
            var variety = await _varietyService.GetByIdAsync(id);
            if (variety == null || variety.OrchardProfileId != orchardProfileId)
                return NotFound(new { message = "Variety not found" });

            return Ok(MapToDto(variety));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving variety {VarietyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new variety for the orchard profile
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(long orchardProfileId, [FromBody] VarietyCreateDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var variety = new OrchardVariety
            {
                VarietyName = model.VarietyName,
                NumberOfTrees = model.NumberOfTrees,
                RootstockUsed = model.RootstockUsed,
                PlantDensity = model.PlantDensity,
                TreesLessThan5Years = model.TreesLessThan5Years,
                TreesFiveToTenYears = model.TreesFiveToTenYears,
                TreesMoreThan10Years = model.TreesMoreThan10Years
            };

            var varietyId = await _varietyService.CreateAsync(orchardProfileId, variety);
            var created = await _varietyService.GetByIdAsync(varietyId);

            _logger.LogInformation("Variety created with ID: {VarietyId} for Orchard Profile: {ProfileId}", varietyId, orchardProfileId);

            return CreatedAtAction(nameof(GetById), new { orchardProfileId, id = varietyId }, MapToDto(created));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating variety for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create or replace multiple varieties for the orchard profile
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> CreateMultiple(long orchardProfileId, [FromBody] VarietyCreateDto[] models)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var profile = await _profileService.GetByIdAsync(orchardProfileId);
            if (profile == null)
                return NotFound(new { message = "Orchard profile not found" });

            var varieties = models.Select(m => new OrchardVariety
            {
                VarietyName = m.VarietyName,
                NumberOfTrees = m.NumberOfTrees,
                RootstockUsed = m.RootstockUsed,
                PlantDensity = m.PlantDensity,
                TreesLessThan5Years = m.TreesLessThan5Years,
                TreesFiveToTenYears = m.TreesFiveToTenYears,
                TreesMoreThan10Years = m.TreesMoreThan10Years
            }).ToList();

            var result = await _varietyService.CreateMultipleAsync(orchardProfileId, varieties);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            _logger.LogInformation("Bulk created {Count} varieties for Orchard Profile: {ProfileId}", result.RecordsAffected, orchardProfileId);

            return Ok(new { success = true, message = result.Message, recordsAffected = result.RecordsAffected });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating varieties for orchard profile {ProfileId}", orchardProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update an existing variety
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long orchardProfileId, long id, [FromBody] VarietyUpdateDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var variety = await _varietyService.GetByIdAsync(id);
            if (variety == null || variety.OrchardProfileId != orchardProfileId)
                return NotFound(new { message = "Variety not found" });

            var updated = new OrchardVariety
            {
                VarietyName = model.VarietyName ?? variety.VarietyName,
                NumberOfTrees = model.NumberOfTrees ?? variety.NumberOfTrees,
                RootstockUsed = model.RootstockUsed ?? variety.RootstockUsed,
                PlantDensity = model.PlantDensity ?? variety.PlantDensity,
                TreesLessThan5Years = model.TreesLessThan5Years ?? variety.TreesLessThan5Years,
                TreesFiveToTenYears = model.TreesFiveToTenYears ?? variety.TreesFiveToTenYears,
                TreesMoreThan10Years = model.TreesMoreThan10Years ?? variety.TreesMoreThan10Years
            };

            var success = await _varietyService.UpdateAsync(id, updated);
            if (!success)
                return NotFound(new { message = "Variety not found" });

            var result = await _varietyService.GetByIdAsync(id);
            _logger.LogInformation("Variety updated: {VarietyId}", id);

            return Ok(new { success = true, data = MapToDto(result) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating variety {VarietyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a variety by ID
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long orchardProfileId, long id)
    {
        try
        {
            var variety = await _varietyService.GetByIdAsync(id);
            if (variety == null || variety.OrchardProfileId != orchardProfileId)
                return NotFound(new { message = "Variety not found" });

            var success = await _varietyService.DeleteAsync(id);
            if (!success)
                return NotFound(new { message = "Variety not found" });

            _logger.LogInformation("Variety deleted: {VarietyId}", id);
            return Ok(new { success = true, message = "Variety deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting variety {VarietyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    private VarietyDto MapToDto(OrchardVariety variety)
    {
        return new VarietyDto
        {
            Id = variety.Id,
            VarietyName = variety.VarietyName,
            NumberOfTrees = variety.NumberOfTrees,
            RootstockUsed = variety.RootstockUsed,
            PlantDensity = variety.PlantDensity,
            TreesLessThan5Years = variety.TreesLessThan5Years,
            TreesFiveToTenYears = variety.TreesFiveToTenYears,
            TreesMoreThan10Years = variety.TreesMoreThan10Years
        };
    }
}
