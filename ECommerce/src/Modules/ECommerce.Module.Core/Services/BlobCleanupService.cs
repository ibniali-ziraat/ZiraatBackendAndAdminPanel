using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    /// <summary>
    /// Service for managing blob file cleanup when entities are deleted.
    /// Tracks blob files associated with entities and handles their deletion.
    /// </summary>
    public class BlobCleanupService : IBlobCleanupService
    {
        private readonly IRepository<BlobFileReference> _blobRefRepo;
        private readonly IAzureBlobService _blobService;

        public BlobCleanupService(
            IRepository<BlobFileReference> blobRefRepo,
            IAzureBlobService blobService)
        {
            _blobRefRepo = blobRefRepo;
            _blobService = blobService;
        }

        /// <summary>
        /// Register a blob file reference for an entity.
        /// Call this when uploading files associated with profiles/entities.
        /// </summary>
        public async Task<long> RegisterBlobReferenceAsync(string blobName, string blobUrl, string entityType, long entityId)
        {
            if (string.IsNullOrWhiteSpace(blobName) || string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("BlobName and EntityType are required.");

            var reference = new BlobFileReference
            {
                BlobName = blobName,
                BlobUrl = blobUrl,
                EntityType = entityType,
                EntityId = entityId,
                CreatedOn = DateTime.UtcNow
            };

            _blobRefRepo.Add(reference);
            await _blobRefRepo.SaveChangesAsync().ConfigureAwait(false);

            return reference.Id;
        }

        /// <summary>
        /// Delete all blob files associated with an entity.
        /// Returns the count of successfully deleted files.
        /// </summary>
        public async Task<int> DeleteBlobsByEntityAsync(string entityType, long entityId)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                return 0;

            try
            {
                var blobReferences = await _blobRefRepo.Query()
                    .Where(b => b.EntityType == entityType && b.EntityId == entityId)
                    .ToListAsync()
                    .ConfigureAwait(false);

                if (blobReferences.Count == 0)
                    return 0;

                var blobNames = blobReferences.Select(b => b.BlobName).ToArray();

                // Delete from Azure Blob Storage
                var deletedCount = await _blobService.DeleteFilesAsync(blobNames);

                // Remove references from database
                foreach (var reference in blobReferences)
                    _blobRefRepo.Remove(reference);

                await _blobRefRepo.SaveChangesAsync().ConfigureAwait(false);

                return deletedCount;
            }
            catch (Exception ex)
            {
                // Log exception but don't throw to prevent cascading failures
                return 0;
            }
        }

        /// <summary>
        /// Delete specific blob files.
        /// Returns the count of successfully deleted files.
        /// </summary>
        public async Task<int> DeleteBlobsAsync(params string[] blobNames)
        {
            if (blobNames == null || blobNames.Length == 0)
                return 0;

            try
            {
                // Delete from Azure Blob Storage
                var deletedCount = await _blobService.DeleteFilesAsync(blobNames);

                // Remove references from database
                var references = await _blobRefRepo.Query()
                    .Where(b => blobNames.Contains(b.BlobName))
                    .ToListAsync()
                    .ConfigureAwait(false);

                foreach (var reference in references)
                    _blobRefRepo.Remove(reference);

                if (references.Count > 0)
                    await _blobRefRepo.SaveChangesAsync().ConfigureAwait(false);

                return deletedCount;
            }
            catch (Exception ex)
            {
                // Log exception but don't throw
                return 0;
            }
        }

        /// <summary>
        /// Get all blob references for an entity.
        /// </summary>
        public async Task<IEnumerable<string>> GetBlobNamesForEntityAsync(string entityType, long entityId)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                return Enumerable.Empty<string>();

            return await _blobRefRepo.Query()
                .Where(b => b.EntityType == entityType && b.EntityId == entityId)
                .Select(b => b.BlobName)
                .ToListAsync()
                .ConfigureAwait(false);
        }
    }
}
