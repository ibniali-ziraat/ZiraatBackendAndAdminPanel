using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Module.Core.Services
{
    /// <summary>
    /// Service for managing blob file cleanup when entities are deleted.
    /// Tracks blob files associated with entities and handles their deletion.
    /// </summary>
    public interface IBlobCleanupService
    {
        /// <summary>
        /// Register a blob file reference for an entity.
        /// Call this when uploading files associated with profiles/entities.
        /// </summary>
        Task<long> RegisterBlobReferenceAsync(string blobName, string blobUrl, string entityType, long entityId);

        /// <summary>
        /// Delete all blob files associated with an entity.
        /// Returns the count of successfully deleted files.
        /// </summary>
        Task<int> DeleteBlobsByEntityAsync(string entityType, long entityId);

        /// <summary>
        /// Delete specific blob files.
        /// Returns the count of successfully deleted files.
        /// </summary>
        Task<int> DeleteBlobsAsync(params string[] blobNames);

        /// <summary>
        /// Get all blob references for an entity.
        /// </summary>
        Task<IEnumerable<string>> GetBlobNamesForEntityAsync(string entityType, long entityId);
    }
}
