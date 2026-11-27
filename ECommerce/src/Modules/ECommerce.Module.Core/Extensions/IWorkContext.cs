using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Extensions
{
    public interface IWorkContext
    {
        string GetCurrentHostName();

        /// <summary>
        /// Returns the authenticated database user, or null for anonymous/unresolved identities.
        /// This lookup never creates an account.
        /// </summary>
        Task<User> GetCurrentUser();
    }
}
