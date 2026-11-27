using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public class ServiceAreaService : IServiceAreaService
    {
        private readonly IRepository<ServiceArea> _repo;
        public ServiceAreaService(IRepository<ServiceArea> repo) => _repo = repo;
        public async Task<IEnumerable<ServiceArea>> GetAllAsync() => await _repo.Query().ToListAsync();
        public async Task<ServiceArea> GetByIdAsync(long id) => await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);
        public async Task AddAsync(ServiceArea entity) { _repo.Add(entity); await _repo.SaveChangesAsync(); }
        public async Task UpdateAsync(ServiceArea entity) { await _repo.SaveChangesAsync(); }
        public async Task DeleteAsync(long id) { var e = await GetByIdAsync(id); if (e != null) { _repo.Remove(e); await _repo.SaveChangesAsync(); } }
    }
}
