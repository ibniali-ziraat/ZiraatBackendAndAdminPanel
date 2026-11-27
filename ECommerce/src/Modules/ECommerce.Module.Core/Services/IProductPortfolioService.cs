using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface IProductPortfolioService
    {
        Task<IEnumerable<ProductPortfolio>> GetAllAsync();

        // Get portfolio by its primary key (Id)
        Task<ProductPortfolio> GetByIdAsync(long id);

        // New: get by supplier profile id (returns portfolio for a supplier)
        Task<ProductPortfolio> GetBySupplierProfileIdAsync(long supplierProfileId);

        Task AddAsync(ProductPortfolio entity);
        Task UpdateAsync(ProductPortfolio entity);
        Task DeleteAsync(long id);

        Task SaveWithDetailsAsync(ProductPortfolio portfolio, IEnumerable<ProductInputType> inputTypes, IEnumerable<ProductMajorBrand> majorBrands, IEnumerable<ProductCustomInput> customInputs);
    }
}
