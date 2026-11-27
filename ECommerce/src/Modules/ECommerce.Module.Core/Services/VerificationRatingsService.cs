using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public class VerificationRatingsService : IVerificationRatingsService
    {
        private readonly IRepository<VerificationRatings> _repo;
        public VerificationRatingsService(IRepository<VerificationRatings> repo) => _repo = repo;
        public async Task<IEnumerable<VerificationRatings>> GetAllAsync() => await _repo.Query().ToListAsync();
        public async Task<VerificationRatings> GetByIdAsync(long id) => await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);
        public async Task AddAsync(VerificationRatings entity) { _repo.Add(entity); await _repo.SaveChangesAsync(); }
        public async Task UpdateAsync(VerificationRatings entity) { await _repo.SaveChangesAsync(); }
        public async Task DeleteAsync(long id) { var e = await GetByIdAsync(id); if (e != null) { _repo.Remove(e); await _repo.SaveChangesAsync(); } }
    }
}
