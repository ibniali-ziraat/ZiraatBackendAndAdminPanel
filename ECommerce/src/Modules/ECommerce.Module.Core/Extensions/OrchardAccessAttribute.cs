using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Core.Extensions
{
    // Runs before model binding/actions, including controller catch blocks and bulk writes.
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class OrchardAccessAttribute : TypeFilterAttribute
    {
        public OrchardAccessAttribute(bool profileRoutes = false) : base(typeof(OrchardAccessFilter))
        {
            Arguments = new object[] { profileRoutes };
        }
    }

    public sealed class OrchardAccessFilter : IAsyncAuthorizationFilter
    {
        private readonly IRepository<OrchardProfile> _orchards;
        private readonly bool _profileRoutes;

        public OrchardAccessFilter(IRepository<OrchardProfile> orchards, bool profileRoutes)
        {
            _orchards = orchards;
            _profileRoutes = profileRoutes;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var principal = context.HttpContext.User;
            var idClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
            if (principal.Identity?.IsAuthenticated != true || !long.TryParse(idClaim, out var userId) || userId <= 0)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var route = context.RouteData.Values;
            var admin = principal.IsInRole("admin");
            var query = _orchards.Query().AsNoTracking();
            if (_profileRoutes && route.TryGetValue("userId", out var requestedUser))
            {
                if (!TryId(requestedUser, out var requestedUserId) || (!admin && requestedUserId != userId))
                    context.Result = new NotFoundResult();
                return;
            }

            if (_profileRoutes && route.TryGetValue("orchardId", out var reference))
            {
                var orchardId = Convert.ToString(reference, CultureInfo.InvariantCulture);
                // Reject ambiguous external references even when one matching row belongs to the caller.
                var owners = await query.Where(x => x.OrchardId == orchardId)
                    .Select(x => x.UserId).Take(2).ToListAsync(context.HttpContext.RequestAborted);
                if (string.IsNullOrWhiteSpace(orchardId) || owners.Count != 1 || (!admin && owners[0] != userId))
                    context.Result = new NotFoundResult();
                return;
            }

            if (route.TryGetValue(_profileRoutes ? "id" : "orchardProfileId", out var requestedOrchard))
            {
                if (!TryId(requestedOrchard, out var orchardId) ||
                    !await query.AnyAsync(x => x.Id == orchardId && (admin || x.UserId == userId),
                        context.HttpContext.RequestAborted))
                    context.Result = new NotFoundResult();
                return;
            }

            // Only the profile collection has owner-derived GET/POST actions without a resource ID.
            if (!_profileRoutes || !(HttpMethods.IsGet(context.HttpContext.Request.Method) ||
                HttpMethods.IsPost(context.HttpContext.Request.Method)))
                context.Result = new NotFoundResult();
        }

        private static bool TryId(object value, out long id) =>
            long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out id) && id > 0;
    }
}
