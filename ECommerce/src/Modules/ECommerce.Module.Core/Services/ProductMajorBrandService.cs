using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public class ProductMajorBrandService : IProductMajorBrandService
    {
        private readonly IRepository<ProductMajorBrand> _repo;
        public ProductMajorBrandService(IRepository<ProductMajorBrand> repo) => _repo = repo;
        public async Task<IEnumerable<ProductMajorBrand>> GetAllAsync() => await _repo.Query().ToListAsync();
        public async Task<ProductMajorBrand> GetByIdAsync(long id) => await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);
        public async Task AddAsync(ProductMajorBrand entity) { _repo.Add(entity); await _repo.SaveChangesAsync(); }
        public async Task UpdateAsync(ProductMajorBrand entity) { await _repo.SaveChangesAsync(); }
        public async Task DeleteAsync(long id) { var e = await GetByIdAsync(id); if (e != null) { _repo.Remove(e); await _repo.SaveChangesAsync(); } }
    }
}
