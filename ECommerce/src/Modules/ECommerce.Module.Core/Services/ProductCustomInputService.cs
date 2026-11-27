using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public class ProductCustomInputService : IProductCustomInputService
    {
        private readonly IRepository<ProductCustomInput> _repo;
        public ProductCustomInputService(IRepository<ProductCustomInput> repo) => _repo = repo;
        public async Task<IEnumerable<ProductCustomInput>> GetAllAsync() => await _repo.Query().ToListAsync();
        public async Task<ProductCustomInput> GetByIdAsync(long id) => await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);
        public async Task AddAsync(ProductCustomInput entity) { _repo.Add(entity); await _repo.SaveChangesAsync(); }
        public async Task UpdateAsync(ProductCustomInput entity) { await _repo.SaveChangesAsync(); }
        public async Task DeleteAsync(long id) { var e = await GetByIdAsync(id); if (e != null) { _repo.Remove(e); await _repo.SaveChangesAsync(); } }
    }
}
