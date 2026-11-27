using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface IProductCustomInputService
    {
        Task<IEnumerable<ProductCustomInput>> GetAllAsync();
        Task<ProductCustomInput> GetByIdAsync(long id);
        Task AddAsync(ProductCustomInput entity);
        Task UpdateAsync(ProductCustomInput entity);
        Task DeleteAsync(long id);
    }
}
