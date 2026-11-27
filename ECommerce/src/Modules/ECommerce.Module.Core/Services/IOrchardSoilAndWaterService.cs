using System.Text.Json;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service interface for managing orchard soil and water data independently.
/// Handles CRUD operations for soil composition and water management.
/// </summary>
public interface IOrchardSoilAndWaterService
{
    /// <summary>
    /// Get soil and water data for a specific orchard profile.
    /// </summary>
    Task<OrchardSoilAndWater> GetByOrchardProfileIdAsync(long orchardProfileId);

    /// <summary>
    /// Create or update soil and water data for an orchard profile.
    /// </summary>
    Task<SectionOperationResult> CreateOrUpdateAsync(long orchardProfileId, OrchardSoilAndWater soilAndWater);

    /// <summary>
    /// Delete soil and water data for an orchard profile.
    /// </summary>
    Task<bool> DeleteAsync(long orchardProfileId);
}
