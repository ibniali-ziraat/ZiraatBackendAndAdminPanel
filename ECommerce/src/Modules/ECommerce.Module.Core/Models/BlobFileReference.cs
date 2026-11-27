using System;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    /// <summary>
    /// Represents a reference to a blob file stored in Azure Blob Storage.
    /// Used to track which blob files are associated with entities for cleanup on deletion.
    /// </summary>
    public class BlobFileReference:EntityBase
    {
        public long Id { get; set; }

        /// <summary>
        /// The blob name/path in Azure Blob Storage
        /// </summary>
        public string BlobName { get; set; }

        /// <summary>
        /// Type of entity this blob belongs to (e.g., "OrchardProfile", "UserProfile")
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// ID of the entity this blob belongs to
        /// </summary>
        public long EntityId { get; set; }

        /// <summary>
        /// The actual URL of the blob (for quick access)
        /// </summary>
        public string BlobUrl { get; set; }

        /// <summary>
        /// When the blob was uploaded
        /// </summary>
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
