using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Extensions
{
    public class WorkContext : IWorkContext
    {
        private User _currentUser;
        private ClaimsPrincipal _currentPrincipal;
        private readonly UserManager<User> _userManager;
        private readonly HttpContext _httpContext;

        public WorkContext(UserManager<User> userManager, IHttpContextAccessor contextAccessor)
        {
            _userManager = userManager;
            _httpContext = contextAccessor.HttpContext;
        }

        public string GetCurrentHostName() => _httpContext.Request.Host.Value;

        public async Task<User> GetCurrentUser()
        {
            var principal = _httpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            if (_currentUser != null && ReferenceEquals(principal, _currentPrincipal))
            {
                return _currentUser;
            }

            // Only authenticated identities resolve to users. Legacy guest cookies are not identity credentials.
            _currentUser = await _userManager.GetUserAsync(principal);
            _currentPrincipal = principal;
            return _currentUser;
        }
    }
}
