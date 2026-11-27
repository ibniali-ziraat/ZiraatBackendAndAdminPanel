using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service interface for managing orchard infrastructure data independently.
/// Handles CRUD operations for equipment, labor, and storage facilities.
/// </summary>
public interface IOrchardInfrastructureService
{
    /// <summary>
    /// Get infrastructure data for a specific orchard profile.
    /// </summary>
    Task<OrchardInfrastructure> GetByOrchardProfileIdAsync(long orchardProfileId);

    /// <summary>
    /// Create or update infrastructure data for an orchard profile.
    /// </summary>
    Task<SectionOperationResult> CreateOrUpdateAsync(long orchardProfileId, OrchardInfrastructure infrastructure);

    /// <summary>
    /// Delete infrastructure data for an orchard profile.
    /// </summary>
    Task<bool> DeleteAsync(long orchardProfileId);
}
