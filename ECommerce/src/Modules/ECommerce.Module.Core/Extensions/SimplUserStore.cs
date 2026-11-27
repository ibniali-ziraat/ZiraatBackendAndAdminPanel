using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ECommerce.Module.Core.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Extensions
{
    public class SimplUserStore : UserStore<User, Role, SimplDbContext, long, IdentityUserClaim<long>, UserRole,
        IdentityUserLogin<long>,IdentityUserToken<long>, IdentityRoleClaim<long>>
    {
        public SimplUserStore(SimplDbContext context, IdentityErrorDescriber describer) : base(context, describer)
        {
        }
    }
}
