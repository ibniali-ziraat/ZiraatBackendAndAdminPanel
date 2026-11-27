using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public class BankDetailsService : IBankDetailsService
    {
        private readonly IRepository<BankDetails> _repo;
        public BankDetailsService(IRepository<BankDetails> repo) => _repo = repo;
        public async Task<IEnumerable<BankDetails>> GetAllAsync() => await _repo.Query().ToListAsync();
        public async Task<BankDetails> GetByIdAsync(long id) => await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);
        public async Task AddAsync(BankDetails entity) { _repo.Add(entity); await _repo.SaveChangesAsync(); }
        public async Task UpdateAsync(BankDetails entity) { await _repo.SaveChangesAsync(); }
        public async Task DeleteAsync(long id) { var e = await GetByIdAsync(id); if (e != null) { _repo.Remove(e); await _repo.SaveChangesAsync(); } }
    }
}
