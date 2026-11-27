using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service interface for managing orchard production data independently.
/// Handles CRUD operations for yield, harvest, and marketing information.
/// </summary>
public interface IOrchardProductionService
{
    /// <summary>
    /// Get production data for a specific orchard profile.
    /// </summary>
    Task<OrchardProduction> GetByOrchardProfileIdAsync(long orchardProfileId);

    /// <summary>
    /// Create or update production data for an orchard profile.
    /// </summary>
    Task<SectionOperationResult> CreateOrUpdateAsync(long orchardProfileId, OrchardProduction production);

    /// <summary>
    /// Delete production data for an orchard profile.
    /// </summary>
    Task<bool> DeleteAsync(long orchardProfileId);
}
