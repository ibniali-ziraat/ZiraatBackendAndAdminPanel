using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.Module.Core.Events;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.ProductComparison.Services;

namespace ECommerce.Module.ProductComparison.Events
{
    public class UserSignedInHandler : INotificationHandler<UserSignedIn>
    {
        private readonly IWorkContext _workContext;
        private readonly IComparingProductService _comparingService;

        public UserSignedInHandler(IWorkContext workContext, IComparingProductService comparingService)
        {
            _workContext = workContext;
            _comparingService = comparingService;
        }

        public async Task Handle(UserSignedIn user, CancellationToken cancellationToken)
        {
            var guestUser = await _workContext.GetCurrentUser();
            if (guestUser == null || guestUser.Id == user.UserId) return;
            _comparingService.MigrateComparingProduct(guestUser.Id, user.UserId);
        }
    }
}
