using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface IBusinessInformationService
    {
        Task<IEnumerable<BusinessInformation>> GetAllAsync();
        Task<BusinessInformation> GetByIdAsync(long id);
        Task<BusinessInformation> GetBusinessInformationByUserId(long userId);
        Task AddAsync(BusinessInformation entity);
        Task UpdateAsync(BusinessInformation entity);
        Task DeleteAsync(long id);
    }
}
