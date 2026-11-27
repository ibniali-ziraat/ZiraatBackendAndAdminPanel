using System.Security.Claims;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface IUserAccessService
    {
        Task<bool> CanAccessAsync(User user);

        Task<bool> CanAccessAsync(ClaimsPrincipal principal);
    }
}
