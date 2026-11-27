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

/// <summary>
/// Controller for managing basic orchard profile information.
/// Handles CRUD operations for the main profile entity only.
/// Detail sections (varieties, soil/water, etc.) are managed by separate section controllers.
/// </summary>
[Area("Core")]
[Route("api/core/orchardProfile")]
[ApiController]
[Authorize, OrchardAccess(profileRoutes: true)]
public class OrchardProfileController : ControllerBase
{
    private readonly IOrchardProfileService _orchardService;
    private readonly ILogger<OrchardProfileController> _logger;

    public OrchardProfileController(
        IOrchardProfileService orchardService,
        ILogger<OrchardProfileController> logger)
    {
        _orchardService = orchardService;
        _logger = logger;
    }

    /// <summary>
    /// Get all orchard profiles for the authenticated user (basic info only)
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userId, out var userIdLong))
            {
                return Unauthorized(new { message = "User authentication failed" });
            }

            var orchards = await _orchardService.GetBasicOrchardsByUserIdAsync(userIdLong);
            var responses = orchards.Select(MapToBasicDto).ToList();
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orchard profiles for user");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get single orchard profile by database ID (with all related sections)
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        try
        {
            var orchard = await _orchardService.GetByIdAsync(id);
            if (orchard == null)
                return NotFound(new { message = "Orchard profile not found" });

            return Ok(MapToResponseDto(orchard));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orchard profile {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get single orchard profile by Orchard ID (with all related sections)
    /// </summary>
    [HttpGet("byOrchardId/{orchardId}")]
    public async Task<IActionResult> GetByOrchardId(string orchardId)
    {
        try
        {
            var orchard = await _orchardService.GetByOrchardIdAsync(orchardId);
            if (orchard == null)
                return NotFound(new { message = "Orchard profile not found" });

            return Ok(MapToResponseDto(orchard));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orchard profile {OrchardId}", orchardId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all orchard profiles for a specific user (with all related sections)
    /// </summary>
    [HttpGet("user/{userId:long}")]
    [Authorize]
    public async Task<IActionResult> GetByUserId(long userId)
    {
        try
        {
            var orchards = await _orchardService.GetByUserIdAsync(userId);
            var responses = orchards.Select(MapToResponseDto).ToList();
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orchard profiles for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create new basic orchard profile.
    /// For a complete profile, use this endpoint first, then use section-specific endpoints to add details.
    /// Note: Legacy support - accepts full OrchardProfileViewModel but detail sections should be created via section APIs.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] OrchardProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userId, out var userIdLong))
            {
                return Unauthorized(new { message = "User authentication failed" });
            }

            var profile = MapViewModelToEntity(model);
            profile.UserId = userIdLong;

            // For legacy support, if detail sections are provided, use bulk operation
            if (HasDetailSections(model))
            {
                var varieties = MapVarietiesFromViewModel(model.Varieties);
                var result = await _orchardService.CreateWithAllDetailsAsync(
                    model.Id,
                    profile,
                    varieties,
                    model.SoilAndWater != null ? MapSoilAndWaterFromViewModel(model.SoilAndWater) : null,
                    model.CropNutrition != null ? MapCropNutritionFromViewModel(model.CropNutrition) : null,
                    model.PlantHealth != null ? MapPlantHealthFromViewModel(model.PlantHealth) : null,
                    model.Infrastructure != null ? MapInfrastructureFromViewModel(model.Infrastructure) : null,
                    model.Production != null ? MapProductionFromViewModel(model.Production) : null,
                    model.InputCost != null ? MapInputCostFromViewModel(model.InputCost) : null,
                    model.OptionalFields != null ? MapOptionalFieldsFromViewModel(model.OptionalFields) : null
                );

                if (!result.Success)
                {
                    if (!result.IsNew)
                    {
                        _logger.LogWarning("Attempted to create duplicate orchard profile. OrchardId: {OrchardId}", model.OrchardId);
                        return Conflict(new
                        {
                            success = false,
                            message = result.Message,
                            existingProfileId = result.ProfileId,
                            statusCode = StatusCodes.Status409Conflict
                        });
                    }

                    _logger.LogError("Error creating orchard profile: {Message}", result.Message);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = result.Message });
                }

                var saved = await _orchardService.GetByIdAsync(result.ProfileId);
                _logger.LogInformation("Orchard profile created with ID: {ProfileId} for User: {UserId}", result.ProfileId, userIdLong);

                return Created($"/api/core/orchardProfile/{result.ProfileId}", new
                {
                    success = true,
                    id = result.ProfileId,
                    data = MapToResponseDto(saved),
                    statusCode = StatusCodes.Status201Created
                });
            }

            // New way: Create basic profile only
            var profileId = await _orchardService.CreateAsync(profile);
            var created = await _orchardService.GetByIdAsync(profileId);

            _logger.LogInformation("Orchard profile created with ID: {ProfileId} for User: {UserId}", profileId, userIdLong);

            return Created($"/api/core/orchardProfile/{profileId}", new
            {
                success = true,
                id = profileId,
                data = MapToBasicDto(created),
                statusCode = StatusCodes.Status201Created,
                message = "Profile created successfully. Use section-specific endpoints to add details."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating orchard profile");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update existing orchard profile (basic info only).
    /// Use section-specific endpoints to update detail sections.
    /// </summary>
    [HttpPut("{id:long}")]
    [Authorize]
    public async Task<IActionResult> Update(long id, [FromBody] OrchardProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var existing = await _orchardService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = "Orchard profile not found" });

            var profile = MapViewModelToEntity(model);
            profile.UserId = existing.UserId;

            // For legacy support, if detail sections are provided, use bulk operation
            if (HasDetailSections(model))
            {
                var varieties = MapVarietiesFromViewModel(model.Varieties);
                var result = await _orchardService.UpdateWithAllDetailsAsync(
                    id,
                    profile,
                    varieties,
                    model.SoilAndWater != null ? MapSoilAndWaterFromViewModel(model.SoilAndWater) : null,
                    model.CropNutrition != null ? MapCropNutritionFromViewModel(model.CropNutrition) : null,
                    model.PlantHealth != null ? MapPlantHealthFromViewModel(model.PlantHealth) : null,
                    model.Infrastructure != null ? MapInfrastructureFromViewModel(model.Infrastructure) : null,
                    model.Production != null ? MapProductionFromViewModel(model.Production) : null,
                    model.InputCost != null ? MapInputCostFromViewModel(model.InputCost) : null,
                    model.OptionalFields != null ? MapOptionalFieldsFromViewModel(model.OptionalFields) : null
                );

                if (!result.Success)
                {
                    _logger.LogError("Error updating orchard profile {ProfileId}: {Message}", id, result.Message);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = result.Message });
                }

                var updated = await _orchardService.GetByIdAsync(id);
                _logger.LogInformation("Orchard profile updated: {ProfileId}", id);

                return Ok(new
                {
                    success = true,
                    data = MapToResponseDto(updated),
                    statusCode = StatusCodes.Status200OK
                });
            }

            // New way: Update basic profile only
            await _orchardService.UpdateAsync(id, profile);
            var updated2 = await _orchardService.GetByIdAsync(id);

            _logger.LogInformation("Orchard profile updated: {ProfileId}", id);

            return Ok(new
            {
                success = true,
                data = MapToBasicDto(updated2),
                statusCode = StatusCodes.Status200OK,
                message = "Profile updated successfully. Use section-specific endpoints to update details."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating orchard profile {ProfileId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete orchard profile and all related data
    /// </summary>
    [HttpDelete("{id:long}")]
    [Authorize]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var existing = await _orchardService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = "Orchard profile not found" });

            var result = await _orchardService.DeleteAsync(id);
            if (!result.Success)
            {
                _logger.LogError("Error deleting orchard profile {ProfileId}: {Message}", id, result.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = result.Message });
            }

            _logger.LogInformation("Orchard profile deleted: {ProfileId}", id);
            return Ok(new
            {
                success = true,
                message = result.Message,
                recordsDeleted = result.RecordsDeleted,
                statusCode = StatusCodes.Status200OK
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting orchard profile {ProfileId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error" });
        }
    }

    #region Mapping Methods

    /// <summary>
    /// Maps basic profile information to entity (excludes detail sections)
    /// </summary>
    private OrchardProfile MapViewModelToEntity(OrchardProfileViewModel model)
    {
        return new OrchardProfile
        {
            OrchardId = model.OrchardId,
            OrchardName = model.OrchardName,
            Kanals = model.Kanals,
            Marlas = model.Marlas,
            EstablishmentYear = model.EstablishmentYear,
            PlantationPattern = model.PlantationPattern,
            OwnershipType = model.OwnershipType,
            District = model.District,
            Tehsil = model.Tehsil,
            Zone = model.Zone,
            PinCode = model.PinCode,
            Latitude = model.Latitude,
            Longitude = model.Longitude
        };
    }

    /// <summary>
    /// Maps basic profile to DTO response (excludes detail sections)
    /// </summary>
    private dynamic MapToBasicDto(OrchardProfile orchard)
    {
        return new
        {
            orchard.Id,
            orchard.OrchardId,
            orchard.OrchardName,
            orchard.Kanals,
            orchard.Marlas,
            orchard.EstablishmentYear,
            orchard.PlantationPattern,
            orchard.OwnershipType,
            orchard.District,
            orchard.Tehsil,
            orchard.Zone,
            orchard.PinCode,
            orchard.Latitude,
            orchard.Longitude,
            orchard.CreatedOn,
            orchard.UpdatedOn
        };
    }

    /// <summary>
    /// Maps full profile to response DTO (includes all detail sections for backward compatibility)
    /// </summary>
    private OrchardProfileResponseDto MapToResponseDto(OrchardProfile orchard)
    {
        return new OrchardProfileResponseDto
        {
            Id = orchard.Id,
            OrchardId = orchard.OrchardId,
            OrchardName = orchard.OrchardName,
            Kanals = orchard.Kanals,
            Marlas = orchard.Marlas,
            EstablishmentYear = orchard.EstablishmentYear,
            PlantationPattern = orchard.PlantationPattern,
            OwnershipType = orchard.OwnershipType,
            District = orchard.District,
            Tehsil = orchard.Tehsil,
            Zone = orchard.Zone,
            PinCode = orchard.PinCode,
            Latitude = orchard.Latitude,
            Longitude = orchard.Longitude,
            Varieties = orchard.Varieties?.Select(v => new VarietyDto
            {
                Id = v.Id,
                VarietyName = v.VarietyName,
                NumberOfTrees = v.NumberOfTrees,
                RootstockUsed = v.RootstockUsed,
                PlantDensity = v.PlantDensity,
                TreesLessThan5Years = v.TreesLessThan5Years,
                TreesFiveToTenYears = v.TreesFiveToTenYears,
                TreesMoreThan10Years = v.TreesMoreThan10Years
            }).ToList() ?? new(),
            CreatedOn = orchard.CreatedOn,
            UpdatedOn = orchard.UpdatedOn
        };
    }

    /// <summary>
    /// Checks if detail sections are provided in the view model (for legacy support)
    /// </summary>
    private bool HasDetailSections(OrchardProfileViewModel model)
    {
        return model.Varieties?.Count > 0 ||
               model.SoilAndWater != null ||
               model.CropNutrition != null ||
               model.PlantHealth != null ||
               model.Infrastructure != null ||
               model.Production != null ||
               model.InputCost != null ||
               model.OptionalFields != null;
    }

    #region Legacy Mapping Methods (for backward compatibility with bulk operations)

    private System.Collections.Generic.IEnumerable<OrchardVariety> MapVarietiesFromViewModel(System.Collections.Generic.List<VarietyDto> varieties)
    {
        return (varieties ?? new System.Collections.Generic.List<VarietyDto>())
            .Select(v => new OrchardVariety
            {
                VarietyName = v.VarietyName,
                NumberOfTrees = v.NumberOfTrees,
                RootstockUsed = v.RootstockUsed,
                PlantDensity = v.PlantDensity,
                TreesLessThan5Years = v.TreesLessThan5Years,
                TreesFiveToTenYears = v.TreesFiveToTenYears,
                TreesMoreThan10Years = v.TreesMoreThan10Years
            })
            .ToList();
    }

    private OrchardSoilAndWater MapSoilAndWaterFromViewModel(SoilAndWaterDto dto)
    {
        return new OrchardSoilAndWater
        {
            SoilType = dto.SoilType,
            SoilPh = dto.SoilPh,
            SoilNitrogen = dto.SoilNitrogen,
            SoilPhosphorus = dto.SoilPhosphorus,
            SoilPotassium = dto.SoilPotassium,
            SoilOrganicCarbon = dto.SoilOrganicCarbon,
            SoilMicronutrients = dto.SoilMicronutrients,
            SoilTestReportUrls = DeserializeArray(dto.SoilTestReportUrls),
            SoilHealthStatus = System.Text.Json.JsonSerializer.Serialize(dto.SoilHealthStatus ?? new string[] { }),
            LastSoilTestDate = dto.LastSoilTestDate,
            WaterSource = dto.WaterSource,
            IrrigationMethod = dto.IrrigationMethod,
            WaterAvailability = dto.WaterAvailability
        };
    }

    private OrchardCropNutrition MapCropNutritionFromViewModel(CropNutritionDto dto)
    {
        return new OrchardCropNutrition
        {
            FertilizerPractices = dto.FertilizerPractices,
            HasFertigationFacility = dto.HasFertigation,
            NutritionPlan = dto.NutritionPlan,
            IsMulchingApplied = dto.IsMulchingApplied,
            LastFertilizerApplicationDate = dto.LastFertilizerApplicationDate,
            FertilizerApplicationMode = dto.FertilizerApplicationMode
        };
    }

    private OrchardPlantHealth MapPlantHealthFromViewModel(PlantHealthDto dto)
    {
        return new OrchardPlantHealth
        {
            DiseaseHistory = System.Text.Json.JsonSerializer.Serialize(dto.DiseaseHistory ?? new string[] { }),
            PestHistory = System.Text.Json.JsonSerializer.Serialize(dto.PestHistory ?? new string[] { }),
            SpraySchedule = System.Text.Json.JsonSerializer.Serialize(dto.SpraySchedule ?? new string[] { }),
            IsCropInsuranceCovered = dto.IsCropInsuranceCovered
        };
    }

    private OrchardInfrastructure MapInfrastructureFromViewModel(InfrastructureDto dto)
    {
        return new OrchardInfrastructure
        {
            EquipmentOwned = System.Text.Json.JsonSerializer.Serialize(dto.EquipmentOwned ?? new string[] { }),
            StorageFacilities = System.Text.Json.JsonSerializer.Serialize(dto.StorageFacilities ?? new string[] { }),
            OtherTools = System.Text.Json.JsonSerializer.Serialize(dto.OtherTools ?? new string[] { }),
            PermanentLabor = dto.PermanentLabor,
            SeasonalLabor = dto.SeasonalLabor,
            ContractBasedLabor = dto.ContractBasedLabor
        };
    }

    private OrchardProduction MapProductionFromViewModel(ProductionDto dto)
    {
        return new OrchardProduction
        {
            AverageAnnualYield = dto.AverageAnnualYield,
            YieldUnit = dto.YieldUnit,
            PeakHarvestFromMonth = dto.PeakHarvestFromMonth,
            PeakHarvestToMonth = dto.PeakHarvestToMonth,
            SellingPreference = dto.SellingPreference,
            BuyerName = dto.BuyerName,
            BuyerContact = dto.BuyerContact
        };
    }

    private OrchardInputCost MapInputCostFromViewModel(InputCostDto dto)
    {
        return new OrchardInputCost
        {
            PesticideSupplierLocation = dto.PesticideSupplierLocation,
            PaymentType = dto.PaymentType,
            CreditPeriod = dto.CreditPeriod
        };
    }

    private OrchardOptionalFields MapOptionalFieldsFromViewModel(OptionalFieldsDto dto)
    {
        return new OrchardOptionalFields
        {
            GovernmentSchemesAvailed = System.Text.Json.JsonSerializer.Serialize(dto.GovernmentSchemesAvailed ?? new string[] { }),
            IsFpoOrCooperativeMember = dto.IsFpoOrCooperativeMember,
            Certifications = System.Text.Json.JsonSerializer.Serialize(dto.Certifications ?? new string[] { }),
            DigitalToolsUsed = System.Text.Json.JsonSerializer.Serialize(dto.DigitalToolsUsed ?? new string[] { })
        };
    }

    #endregion

    #endregion

    /// <summary>
    /// Converts a string array to a JSON string, or returns an empty JSON array if null.
    /// </summary>
    private string DeserializeArray(string[] array)
    {
        return System.Text.Json.JsonSerializer.Serialize(array ?? new string[] { });
    }
}
