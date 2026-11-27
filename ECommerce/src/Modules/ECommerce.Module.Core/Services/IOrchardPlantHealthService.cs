using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service interface for managing orchard plant health data independently.
/// Handles CRUD operations for disease, pest, and health management.
/// </summary>
public interface IOrchardPlantHealthService
{
    /// <summary>
    /// Get plant health data for a specific orchard profile.
    /// </summary>
    Task<OrchardPlantHealth> GetByOrchardProfileIdAsync(long orchardProfileId);

    /// <summary>
    /// Create or update plant health data for an orchard profile.
    /// </summary>
    Task<SectionOperationResult> CreateOrUpdateAsync(long orchardProfileId, OrchardPlantHealth plantHealth);

    /// <summary>
    /// Delete plant health data for an orchard profile.
    /// </summary>
    Task<bool> DeleteAsync(long orchardProfileId);
}
