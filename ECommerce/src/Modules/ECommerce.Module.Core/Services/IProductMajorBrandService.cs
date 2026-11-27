using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface IProductMajorBrandService
    {
        Task<IEnumerable<ProductMajorBrand>> GetAllAsync();
        Task<ProductMajorBrand> GetByIdAsync(long id);
        Task AddAsync(ProductMajorBrand entity);
        Task UpdateAsync(ProductMajorBrand entity);
        Task DeleteAsync(long id);
    }
}
