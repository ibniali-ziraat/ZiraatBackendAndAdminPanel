using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public class SupportServicesService : ISupportServicesService
    {
        private readonly IRepository<SupportServices> _repo;
        public SupportServicesService(IRepository<SupportServices> repo) => _repo = repo;
        public async Task<IEnumerable<SupportServices>> GetAllAsync() => await _repo.Query().ToListAsync();
        public async Task<SupportServices> GetByIdAsync(long id) => await _repo.Query().FirstOrDefaultAsync(x => x.SupplierProfileId == id);
        public async Task AddAsync(SupportServices entity) { _repo.Add(entity); await _repo.SaveChangesAsync(); }
        public async Task UpdateAsync(SupportServices entity) { await _repo.SaveChangesAsync(); }
        public async Task DeleteAsync(long id) { var e = await GetByIdAsync(id); if (e != null) { _repo.Remove(e); await _repo.SaveChangesAsync(); } }
    }
}
