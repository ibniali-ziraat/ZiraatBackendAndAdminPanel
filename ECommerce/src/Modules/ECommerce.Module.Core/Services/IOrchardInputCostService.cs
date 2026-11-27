using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service interface for managing orchard input cost data independently.
/// Handles CRUD operations for pesticide suppliers and payment information.
/// </summary>
public interface IOrchardInputCostService
{
    /// <summary>
    /// Get input cost data for a specific orchard profile.
    /// </summary>
    Task<OrchardInputCost> GetByOrchardProfileIdAsync(long orchardProfileId);

    /// <summary>
    /// Create or update input cost data for an orchard profile.
    /// </summary>
    Task<SectionOperationResult> CreateOrUpdateAsync(long orchardProfileId, OrchardInputCost inputCost);

    /// <summary>
    /// Delete input cost data for an orchard profile.
    /// </summary>
    Task<bool> DeleteAsync(long orchardProfileId);
}
