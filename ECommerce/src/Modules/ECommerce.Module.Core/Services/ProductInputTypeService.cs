using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public class ProductInputTypeService : IProductInputTypeService
    {
        private readonly IRepository<ProductInputType> _repo;
        public ProductInputTypeService(IRepository<ProductInputType> repo) => _repo = repo;
        public async Task<IEnumerable<ProductInputType>> GetAllAsync() => await _repo.Query().ToListAsync();
        public async Task<ProductInputType> GetByIdAsync(long id) => await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);
        public async Task AddAsync(ProductInputType entity) { _repo.Add(entity); await _repo.SaveChangesAsync(); }
        public async Task UpdateAsync(ProductInputType entity) { await _repo.SaveChangesAsync(); }
        public async Task DeleteAsync(long id) { var e = await GetByIdAsync(id); if (e != null) { _repo.Remove(e); await _repo.SaveChangesAsync(); } }
    }
}
