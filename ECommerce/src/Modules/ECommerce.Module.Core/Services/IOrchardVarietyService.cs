using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service interface for managing orchard variety data independently.
/// Handles CRUD operations for fruit varieties planted in orchards.
/// </summary>
public interface IOrchardVarietyService
{
    /// <summary>
    /// Get all varieties for a specific orchard profile.
    /// </summary>
    Task<IEnumerable<OrchardVariety>> GetByOrchardProfileIdAsync(long orchardProfileId);

    /// <summary>
    /// Get a single variety by ID.
    /// </summary>
    Task<OrchardVariety> GetByIdAsync(long varietyId);

    /// <summary>
    /// Create a new variety for an orchard profile.
    /// </summary>
    Task<long> CreateAsync(long orchardProfileId, OrchardVariety variety);

    /// <summary>
    /// Create multiple varieties for an orchard profile.
    /// Replaces existing varieties.
    /// </summary>
    Task<SectionOperationResult> CreateMultipleAsync(long orchardProfileId, IEnumerable<OrchardVariety> varieties);

    /// <summary>
    /// Update an existing variety.
    /// </summary>
    Task<bool> UpdateAsync(long varietyId, OrchardVariety variety);

    /// <summary>
    /// Delete a variety by ID.
    /// </summary>
    Task<bool> DeleteAsync(long varietyId);

    /// <summary>
    /// Delete all varieties for an orchard profile.
    /// </summary>
    Task<SectionOperationResult> DeleteAllByOrchardProfileIdAsync(long orchardProfileId);
}
