using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface IProductInputTypeService
    {
        Task<IEnumerable<ProductInputType>> GetAllAsync();
        Task<ProductInputType> GetByIdAsync(long id);
        Task AddAsync(ProductInputType entity);
        Task UpdateAsync(ProductInputType entity);
        Task DeleteAsync(long id);
    }
}
