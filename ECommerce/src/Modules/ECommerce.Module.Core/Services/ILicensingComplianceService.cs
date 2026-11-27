using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface ILicensingComplianceService
    {
        Task<IEnumerable<LicensingCompliance>> GetAllAsync();
        Task<LicensingCompliance> GetByIdAsync(long id);
        Task<LicensingCompliance> GetBySupplierProfileIdAsync(long supplierProfileId);
        Task<License> GetLicenseByIdAsync(long licenseId);
        Task<IEnumerable<License>> GetLicensesByComplianceIdAsync(long complianceId);
        Task AddAsync(LicensingCompliance entity);
        Task UpdateAsync(LicensingCompliance entity);
        Task UpdateLicenseAsync(License entity);
        Task UpdateLicenseDocumentAsync(LicenseDocument entity);
        Task UpdateAdditionalDocumentAsync(AdditionalDocument entity);
        void RemoveAdditionalDocument(AdditionalDocument entity);
        Task<License> AddLicenseAsync(long complianceId, License license);
        Task<bool> DeleteAsync(long id);
        Task<bool> DeleteLicenseAsync(long licenseId);
        Task<bool> DeleteAdditionalDocumentAsync(long documentId);
    }
}
