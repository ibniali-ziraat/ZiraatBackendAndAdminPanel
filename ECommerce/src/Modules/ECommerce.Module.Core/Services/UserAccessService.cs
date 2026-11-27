using System.Security.Claims;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Core.Services
{
    public class UserAccessService : IUserAccessService
    {
        private readonly UserManager<User> _userManager;
        private readonly IRepository<Vendor> _vendorRepository;

        public UserAccessService(UserManager<User> userManager, IRepository<Vendor> vendorRepository)
        {
            _userManager = userManager;
            _vendorRepository = vendorRepository;
        }

        public async Task<bool> CanAccessAsync(User user)
        {
            if (user == null || user.IsDeleted)
            {
                return false;
            }

            // Consult the persisted vendor status even for accounts created before this check.
            return !user.VendorId.HasValue || await _vendorRepository.Query()
                .AnyAsync(x => x.Id == user.VendorId.Value && !x.IsDeleted);
        }

        public async Task<bool> CanAccessAsync(ClaimsPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            return await CanAccessAsync(await _userManager.GetUserAsync(principal));
        }
    }
}
