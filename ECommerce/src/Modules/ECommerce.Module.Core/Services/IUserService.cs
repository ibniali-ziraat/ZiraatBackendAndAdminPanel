using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface IUserService
    {
        Task<User> GetCurrentUserAsync();
        Task UpdateUserAsync(User user);
        Task<User> GetUserByIdAsync(long id);
    }
}
