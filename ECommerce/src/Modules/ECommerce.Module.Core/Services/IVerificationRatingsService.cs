using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface IVerificationRatingsService
    {
        Task<IEnumerable<VerificationRatings>> GetAllAsync();
        Task<VerificationRatings> GetByIdAsync(long id);
        Task AddAsync(VerificationRatings entity);
        Task UpdateAsync(VerificationRatings entity);
        Task DeleteAsync(long id);
    }
}
