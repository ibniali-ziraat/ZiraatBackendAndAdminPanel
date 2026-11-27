using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service interface for managing orchard crop nutrition data independently.
/// Handles CRUD operations for fertilization and nutrition management.
/// </summary>
public interface IOrchardCropNutritionService
{
    /// <summary>
    /// Get crop nutrition data for a specific orchard profile.
    /// </summary>
    Task<OrchardCropNutrition> GetByOrchardProfileIdAsync(long orchardProfileId);

    /// <summary>
    /// Create or update crop nutrition data for an orchard profile.
    /// </summary>
    Task<SectionOperationResult> CreateOrUpdateAsync(long orchardProfileId, OrchardCropNutrition cropNutrition);

    /// <summary>
    /// Delete crop nutrition data for an orchard profile.
    /// </summary>
    Task<bool> DeleteAsync(long orchardProfileId);
}
