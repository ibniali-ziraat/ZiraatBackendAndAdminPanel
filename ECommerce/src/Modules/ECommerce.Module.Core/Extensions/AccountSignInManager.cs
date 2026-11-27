using System.Threading.Tasks;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Module.Core.Extensions
{
    public class AccountSignInManager : SignInManager<User>
    {
        private readonly IUserAccessService _userAccess;

        public AccountSignInManager(UserManager<User> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<User> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<User>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<User> confirmation,
            IUserAccessService userAccess)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
            _userAccess = userAccess;
        }

        public override async Task<bool> CanSignInAsync(User user)
        {
            return await _userAccess.CanAccessAsync(user) && await base.CanSignInAsync(user);
        }
    }
}
