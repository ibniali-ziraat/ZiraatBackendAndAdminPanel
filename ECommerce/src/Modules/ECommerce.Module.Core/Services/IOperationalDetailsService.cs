using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface IOperationalDetailsService
    {
        Task<IEnumerable<OperationalDetails>> GetAllAsync();
        Task<OperationalDetails> GetByIdAsync(long id);
        Task AddAsync(OperationalDetails entity);
        Task UpdateAsync(OperationalDetails entity);
        Task DeleteAsync(long id);
        Task SaveWithDetailsAsync(OperationalDetails details, IEnumerable<ServiceArea> serviceAreas, IEnumerable<CompanyTieup> companyTieups);
    }
}
