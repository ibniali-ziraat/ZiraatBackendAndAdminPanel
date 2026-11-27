using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface ICustomerReviewService
    {
        Task<IEnumerable<CustomerReview>> GetAllAsync();
        Task<CustomerReview> GetByIdAsync(long id);
        Task AddAsync(CustomerReview entity);
        Task UpdateAsync(CustomerReview entity);
        Task DeleteAsync(long id);
    }
}
