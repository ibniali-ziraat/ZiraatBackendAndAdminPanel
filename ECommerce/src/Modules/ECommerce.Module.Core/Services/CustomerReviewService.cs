using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public class CustomerReviewService : ICustomerReviewService
    {
        private readonly IRepository<CustomerReview> _repo;
        public CustomerReviewService(IRepository<CustomerReview> repo) => _repo = repo;
        public async Task<IEnumerable<CustomerReview>> GetAllAsync() => await _repo.Query().ToListAsync();
        public async Task<CustomerReview> GetByIdAsync(long id) => await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);
        public async Task AddAsync(CustomerReview entity) { _repo.Add(entity); await _repo.SaveChangesAsync(); }
        public async Task UpdateAsync(CustomerReview entity) { await _repo.SaveChangesAsync(); }
        public async Task DeleteAsync(long id) { var e = await GetByIdAsync(id); if (e != null) { _repo.Remove(e); await _repo.SaveChangesAsync(); } }
    }
}
