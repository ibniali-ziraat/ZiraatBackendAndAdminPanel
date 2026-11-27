using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service interface for managing orchard profile data with grouped entities.
/// Supports independent CRUD operations for each section without single transaction.
/// </summary>
public interface IOrchardProfileService
{
    #region Profile Management

    /// <summary>
    /// Get orchard profile by database ID with all related entities.
    /// </summary>
    Task<OrchardProfile> GetByIdAsync(long id);

    /// <summary>
    /// Get orchard profile by Orchard ID string with all related entities.
    /// </summary>
    Task<OrchardProfile> GetByOrchardIdAsync(string orchardId);

    /// <summary>
    /// Get all orchard profiles with all related entities, ordered by created date descending.
    /// </summary>
    Task<IEnumerable<OrchardProfile>> GetAllAsync();

    /// <summary>
    /// Get all basic orchard profiles for a specific user.
    /// </summary>
    Task<IEnumerable<OrchardProfile>> GetBasicOrchardsByUserIdAsync(long userId);


    /// <summary>
    /// Get all orchard profiles for a specific user with all related entities.
    /// </summary>
    Task<IEnumerable<OrchardProfile>> GetByUserIdAsync(long userId);

    /// <summary>
    /// Create a new orchard profile (main entity only).
    /// </summary>
    /// <returns>The ID of the newly created orchard profile.</returns>
    Task<long> CreateAsync(OrchardProfile model);

    /// <summary>
    /// Update an existing orchard profile (main entity only).
    /// </summary>
    Task UpdateAsync(long id, OrchardProfile model);

    /// <summary>
    /// Delete an orchard profile by ID.
    /// </summary>
    Task<DeleteResult> DeleteAsync(long id);

    /// <summary>
    /// Preview what will be deleted before actual deletion.
    /// </summary>
    Task<DeletePreview> GetDeletePreviewAsync(long id);

    #endregion

    #region Independent Section Operations

    /// <summary>
    /// Create or update locations for an orchard profile independently.
    /// Replaces existing locations with new ones.
    /// </summary>
    Task<SectionOperationResult> SaveLocationsAsync(long orchardProfileId, IEnumerable<OrchardLocation> locations);

    /// <summary>
    /// Create or update varieties for an orchard profile independently.
    /// Replaces existing varieties with new ones.
    /// </summary>
    Task<SectionOperationResult> SaveVarietiesAsync(long orchardProfileId, IEnumerable<OrchardVariety> varieties);

    /// <summary>
    /// Create or update soil and water data for an orchard profile independently.
    /// </summary>
    Task<SectionOperationResult> SaveSoilAndWaterAsync(long orchardProfileId, OrchardSoilAndWater soilAndWater);

    /// <summary>
    /// Create or update crop nutrition data for an orchard profile independently.
    /// </summary>
    Task<SectionOperationResult> SaveCropNutritionAsync(long orchardProfileId, OrchardCropNutrition cropNutrition);

    /// <summary>
    /// Create or update plant health data for an orchard profile independently.
    /// </summary>
    Task<SectionOperationResult> SavePlantHealthAsync(long orchardProfileId, OrchardPlantHealth plantHealth);

    /// <summary>
    /// Create or update infrastructure data for an orchard profile independently.
    /// </summary>
    Task<SectionOperationResult> SaveInfrastructureAsync(long orchardProfileId, OrchardInfrastructure infrastructure);

    /// <summary>
    /// Create or update production data for an orchard profile independently.
    /// </summary>
    Task<SectionOperationResult> SaveProductionAsync(long orchardProfileId, OrchardProduction production);

    /// <summary>
    /// Create or update input cost data for an orchard profile independently.
    /// </summary>
    Task<SectionOperationResult> SaveInputCostAsync(long orchardProfileId, OrchardInputCost inputCost);

    /// <summary>
    /// Create or update optional fields data for an orchard profile independently.
    /// </summary>
    Task<SectionOperationResult> SaveOptionalFieldsAsync(long orchardProfileId, OrchardOptionalFields optionalFields);

    #endregion

    #region Bulk Operations (Legacy Support)

    /// <summary>
    /// Legacy method - kept for backward compatibility.
    /// Create orchard profile with all related detail entities in a single transaction.
    /// Only creates if the profile doesn't already exist (checked by OrchardId).
    /// </summary>
    Task<CreateOrchardProfileResult> CreateWithAllDetailsAsync(long Id,
        OrchardProfile profile,
        IEnumerable<OrchardVariety> varieties,
        OrchardSoilAndWater soilAndWater,
        OrchardCropNutrition cropNutrition,
        OrchardPlantHealth plantHealth,
        OrchardInfrastructure infrastructure,
        OrchardProduction production,
        OrchardInputCost inputCost,
        OrchardOptionalFields optionalFields
    );

    /// <summary>
    /// Legacy method - kept for backward compatibility.
    /// Update existing orchard profile with all related detail entities in a single transaction.
    /// Replaces child collections (locations, varieties) and detail entities.
    /// </summary>
    Task<UpdateOrchardProfileResult> UpdateWithAllDetailsAsync(
        long id,
        OrchardProfile profile,
        IEnumerable<OrchardVariety> varieties,
        OrchardSoilAndWater soilAndWater,
        OrchardCropNutrition cropNutrition,
        OrchardPlantHealth plantHealth,
        OrchardInfrastructure infrastructure,
        OrchardProduction production,
        OrchardInputCost inputCost,
        OrchardOptionalFields optionalFields
    );

    #endregion

    /// <summary>
    /// Partially update an orchard profile based on section visibility.
    /// Only updates sections that are provided (non-null) in the patch model.
    /// </summary>
    Task<UpdateOrchardProfileResult> PartialUpdateAsync(long id, OrchardProfilePatchViewModel patchModel);
}

/// <summary>
/// Result of a section operation (create/update).
/// </summary>
public class SectionOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int RecordsAffected { get; set; }
}

/// <summary>
/// Result of creating an orchard profile with all details.
/// </summary>
public class CreateOrchardProfileResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public long ProfileId { get; set; }
    public bool IsNew { get; set; }
}

/// <summary>
/// Result of updating an orchard profile with all details.
/// </summary>
public class UpdateOrchardProfileResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
}
