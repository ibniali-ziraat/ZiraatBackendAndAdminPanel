using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface ISupportServicesService
    {
        Task<IEnumerable<SupportServices>> GetAllAsync();
        Task<SupportServices> GetByIdAsync(long id);
        Task AddAsync(SupportServices entity);
        Task UpdateAsync(SupportServices entity);
        Task DeleteAsync(long id);
    }
}
