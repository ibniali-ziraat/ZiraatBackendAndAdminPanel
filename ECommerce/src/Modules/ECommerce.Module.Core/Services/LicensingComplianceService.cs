using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Core.Services
{
    public class LicensingComplianceService : ILicensingComplianceService
    {
        private readonly IRepository<LicensingCompliance> _complianceRepo;
        private readonly IRepository<Models.License> _licenseRepo;
        private readonly IRepository<LicenseDocument> _licenseDocRepo;
        private readonly IRepository<AdditionalDocument> _additionalDocRepo;

        public LicensingComplianceService(
            IRepository<LicensingCompliance> complianceRepo,
            IRepository<Models.License> licenseRepo,
            IRepository<LicenseDocument> licenseDocRepo,
            IRepository<AdditionalDocument> additionalDocRepo)
        {
            _complianceRepo = complianceRepo;
            _licenseRepo = licenseRepo;
            _licenseDocRepo = licenseDocRepo;
            _additionalDocRepo = additionalDocRepo;
        }

        public async Task<IEnumerable<LicensingCompliance>> GetAllAsync()
        {
            return await _complianceRepo.Query()
                .Include(x => x.Licenses)
                    .ThenInclude(l => l.LicenseDocument)
                .Include(x => x.Licenses)
                    .ThenInclude(l => l.AdditionalDocuments)
                .ToListAsync();
        }

        public async Task<LicensingCompliance> GetByIdAsync(long id)
        {
            return await _complianceRepo.Query()
                .Include(x => x.Licenses)
                    .ThenInclude(l => l.LicenseDocument)
                .Include(x => x.Licenses)
                    .ThenInclude(l => l.AdditionalDocuments)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<LicensingCompliance> GetBySupplierProfileIdAsync(long supplierProfileId)
        {
            return await _complianceRepo.Query()
                .Include(x => x.Licenses)
                    .ThenInclude(l => l.LicenseDocument)
                .Include(x => x.Licenses)
                    .ThenInclude(l => l.AdditionalDocuments)
                .FirstOrDefaultAsync(x => x.SupplierProfileId == supplierProfileId);
        }

        public async Task<Models.License> GetLicenseByIdAsync(long licenseId)
        {
            return await _licenseRepo.Query()
                .Include(l => l.LicenseDocument)
                .Include(l => l.AdditionalDocuments)
                .FirstOrDefaultAsync(x => x.Id == licenseId);
        }

        public async Task<IEnumerable<Models.License>> GetLicensesByComplianceIdAsync(long complianceId)
        {
            return await _licenseRepo.Query()
                .Where(l => l.LicensingComplianceId == complianceId)
                .Include(l => l.LicenseDocument)
                .Include(l => l.AdditionalDocuments)
                .ToListAsync();
        }

        public async Task AddAsync(LicensingCompliance entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "LicensingCompliance entity cannot be null");
            }

            if (entity.SupplierProfileId <= 0)
            {
                throw new ArgumentException("SupplierProfileId must be greater than 0", nameof(entity.SupplierProfileId));
            }

            try
            {
                _complianceRepo.Add(entity);
                await _complianceRepo.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                throw new InvalidOperationException($"Database error while adding licensing compliance record: {dbEx.InnerException?.Message}", dbEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error adding licensing compliance record and related data", ex);
            }
        }

        public async Task UpdateAsync(LicensingCompliance entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "LicensingCompliance entity cannot be null");
            }

            if (entity.Id <= 0)
            {
                throw new ArgumentException("Id must be greater than 0", nameof(entity.Id));
            }

            try
            {
                await _complianceRepo.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                throw new InvalidOperationException($"Database error while updating licensing compliance record: {dbEx.InnerException?.Message}", dbEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error updating licensing compliance record", ex);
            }
        }

        public async Task UpdateLicenseAsync(Models.License entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "License entity cannot be null");
            }

            if (entity.Id <= 0)
            {
                throw new ArgumentException("Id must be greater than 0", nameof(entity.Id));
            }

            try
            {
                await _licenseRepo.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                throw new InvalidOperationException($"Database error while updating license: {dbEx.InnerException?.Message}", dbEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error updating license", ex);
            }
        }

        public async Task UpdateLicenseDocumentAsync(LicenseDocument entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "LicenseDocument entity cannot be null");
            }

            if (entity.Id <= 0)
            {
                throw new ArgumentException("Id must be greater than 0", nameof(entity.Id));
            }

            try
            {
                await _licenseDocRepo.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                throw new InvalidOperationException($"Database error while updating license document: {dbEx.InnerException?.Message}", dbEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error updating license document", ex);
            }
        }

        // Stage deletion so the document changes are saved together by UpdateLicenseAsync.
        public void RemoveAdditionalDocument(AdditionalDocument entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _additionalDocRepo.Remove(entity);
        }

        public async Task UpdateAdditionalDocumentAsync(AdditionalDocument entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "AdditionalDocument entity cannot be null");
            }

            if (entity.Id <= 0)
            {
                throw new ArgumentException("Id must be greater than 0", nameof(entity.Id));
            }

            try
            {
                await _additionalDocRepo.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                throw new InvalidOperationException($"Database error while updating additional document: {dbEx.InnerException?.Message}", dbEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error updating additional document", ex);
            }
        }

        /// <summary>
        /// Adds a new license to an existing licensing compliance record
        /// </summary>
        public async Task<Models.License> AddLicenseAsync(long complianceId, Models.License license)
        {
            if (license == null)
            {
                throw new ArgumentNullException(nameof(license), "License entity cannot be null");
            }

            if (complianceId <= 0)
            {
                throw new ArgumentException("Compliance Id must be greater than 0", nameof(complianceId));
            }

            if (string.IsNullOrWhiteSpace(license.LicenseType))
            {
                throw new ArgumentException("License type is required", nameof(license.LicenseType));
            }

            if (string.IsNullOrWhiteSpace(license.LicenseNumber))
            {
                throw new ArgumentException("License number is required", nameof(license.LicenseNumber));
            }

            try
            {
                var compliance = await GetByIdAsync(complianceId);
                if (compliance == null)
                {
                    throw new InvalidOperationException($"Licensing compliance record with Id {complianceId} not found");
                }

                license.LicensingComplianceId = complianceId;
                _licenseRepo.Add(license);
                await _licenseRepo.SaveChangesAsync();

                return await GetLicenseByIdAsync(license.Id);
            }
            catch (DbUpdateException dbEx)
            {
                throw new InvalidOperationException($"Database error while adding license: {dbEx.InnerException?.Message}", dbEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error adding license", ex);
            }
        }

        public async Task<bool> DeleteAsync(long id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Id must be greater than 0", nameof(id));
            }

            try
            {
                var entity = await GetByIdAsync(id);
                if (entity == null)
                {
                    return false;
                }

                // Get all licenses for this compliance record
                var licenses = await _licenseRepo.Query()
                    .Where(l => l.LicensingComplianceId == id)
                    .Include(l => l.LicenseDocument)
                    .Include(l => l.AdditionalDocuments)
                    .ToListAsync();

                // Delete all additional documents for all licenses
                foreach (var license in licenses)
                {
                    if (license.AdditionalDocuments != null && license.AdditionalDocuments.Count > 0)
                    {
                        foreach (var doc in license.AdditionalDocuments)
                        {
                            _additionalDocRepo.Remove(doc);
                        }
                    }

                    // Delete license document if exists
                    if (license.LicenseDocument != null)
                    {
                        _licenseDocRepo.Remove(license.LicenseDocument);
                    }

                    // Delete license
                    _licenseRepo.Remove(license);
                }

                // Save all document deletions
                await _additionalDocRepo.SaveChangesAsync();
                await _licenseDocRepo.SaveChangesAsync();
                await _licenseRepo.SaveChangesAsync();

                // Finally delete the compliance record
                _complianceRepo.Remove(entity);
                await _complianceRepo.SaveChangesAsync();

                return true;
            }
            catch (DbUpdateException dbEx)
            {
                throw new InvalidOperationException($"Database error while deleting licensing compliance record: {dbEx.InnerException?.Message}", dbEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error deleting licensing compliance record", ex);
            }
        }

        public async Task<bool> DeleteLicenseAsync(long licenseId)
        {
            if (licenseId <= 0)
            {
                throw new ArgumentException("License Id must be greater than 0", nameof(licenseId));
            }

            try
            {
                var license = await GetLicenseByIdAsync(licenseId);
                if (license == null)
                {
                    return false;
                }

                // Delete all additional documents first
                var additionalDocuments = await _additionalDocRepo.Query()
                    .Where(x => x.LicenseId == licenseId)
                    .ToListAsync();

                foreach (var doc in additionalDocuments)
                {
                    _additionalDocRepo.Remove(doc);
                }

                // Delete license document if exists
                if (license.LicenseDocument != null)
                {
                    _licenseDocRepo.Remove(license.LicenseDocument);
                }

                // Save document deletions first
                if (additionalDocuments.Count > 0)
                {
                    await _additionalDocRepo.SaveChangesAsync();
                }

                if (license.LicenseDocument != null)
                {
                    await _licenseDocRepo.SaveChangesAsync();
                }

                // Finally delete the license
                _licenseRepo.Remove(license);
                await _licenseRepo.SaveChangesAsync();

                return true;
            }
            catch (DbUpdateException dbEx)
            {
                throw new InvalidOperationException($"Database error while deleting license: {dbEx.InnerException?.Message}", dbEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error deleting license", ex);
            }
        }

        public async Task<bool> DeleteAdditionalDocumentAsync(long documentId)
        {
            if (documentId <= 0)
            {
                throw new ArgumentException("Document Id must be greater than 0", nameof(documentId));
            }

            try
            {
                var document = await _additionalDocRepo.Query()
                    .FirstOrDefaultAsync(x => x.Id == documentId);

                if (document == null)
                {
                    return false;
                }

                _additionalDocRepo.Remove(document);
                await _additionalDocRepo.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException dbEx)
            {
                throw new InvalidOperationException($"Database error while deleting additional document: {dbEx.InnerException?.Message}", dbEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error deleting additional document", ex);
            }
        }
    }
}
