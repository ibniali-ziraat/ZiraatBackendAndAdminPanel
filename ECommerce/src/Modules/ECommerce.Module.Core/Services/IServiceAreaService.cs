using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface IServiceAreaService
    {
        Task<IEnumerable<ServiceArea>> GetAllAsync();
        Task<ServiceArea> GetByIdAsync(long id);
        Task AddAsync(ServiceArea entity);
        Task UpdateAsync(ServiceArea entity);
        Task DeleteAsync(long id);
    }
}
