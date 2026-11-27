using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRepository<User> _userRepository;

        public UserService(IHttpContextAccessor httpContextAccessor, IRepository<User> userRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
        }

        public async Task<User> GetCurrentUserAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                return null;
            }

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub") ?? user.FindFirst("id");
            if (idClaim == null)
            {
                return null;
            }

            if (!long.TryParse(idClaim.Value, out var userId))
            {
                return null;
            }

            return await _userRepository.Query().FirstOrDefaultAsync(x => x.Id == userId);
        }

        public async Task<User> GetUserByIdAsync(long id)
        {
            return await _userRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task UpdateUserAsync(User user)
        {
            _userRepository.SaveChanges();
            await Task.CompletedTask;
        }
    }
}
