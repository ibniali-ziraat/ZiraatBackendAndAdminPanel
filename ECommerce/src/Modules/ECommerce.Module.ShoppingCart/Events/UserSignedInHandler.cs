using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.Module.Core.Events;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.ShoppingCart.Services;

namespace ECommerce.Module.ShoppingCart.Events
{
    public class UserSignedInHandler : INotificationHandler<UserSignedIn>
    {
        private readonly IWorkContext _workContext;
        private readonly ICartService _cartService;

        public UserSignedInHandler(IWorkContext workContext, ICartService cartService)
        {
            _workContext = workContext;
            _cartService = cartService;
        }

        public async Task Handle(UserSignedIn user, CancellationToken cancellationToken)
        {
            var guestUser = await _workContext.GetCurrentUser();
            if (guestUser == null || guestUser.Id == user.UserId) return;
            await _cartService.MigrateCart(guestUser.Id, user.UserId);
        }
    }
}
