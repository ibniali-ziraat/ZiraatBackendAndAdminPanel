using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service interface for managing orchard optional fields data independently.
/// Handles CRUD operations for certifications and optional information.
/// </summary>
public interface IOrchardOptionalFieldsService
{
    /// <summary>
    /// Get optional fields data for a specific orchard profile.
    /// </summary>
    Task<OrchardOptionalFields> GetByOrchardProfileIdAsync(long orchardProfileId);

    /// <summary>
    /// Create or update optional fields data for an orchard profile.
    /// </summary>
    Task<SectionOperationResult> CreateOrUpdateAsync(long orchardProfileId, OrchardOptionalFields optionalFields);

    /// <summary>
    /// Delete optional fields data for an orchard profile.
    /// </summary>
    Task<bool> DeleteAsync(long orchardProfileId);
}
