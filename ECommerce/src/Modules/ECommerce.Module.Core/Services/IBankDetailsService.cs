using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface IBankDetailsService
    {
        Task<IEnumerable<BankDetails>> GetAllAsync();
        Task<BankDetails> GetByIdAsync(long id);
        Task AddAsync(BankDetails entity);
        Task UpdateAsync(BankDetails entity);
        Task DeleteAsync(long id);
    }
}
