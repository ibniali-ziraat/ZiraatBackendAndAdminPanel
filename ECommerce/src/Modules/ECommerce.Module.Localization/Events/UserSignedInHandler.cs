using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Events;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Localization.Events
{
    public class UserSignedInHandler : INotificationHandler<UserSignedIn>
    {
        private readonly IWorkContext _workContext;
        private readonly IRepositoryWithTypedId<User, long> _userRepository;

        public UserSignedInHandler(IWorkContext workContext, IRepositoryWithTypedId<User, long> userRepository)
        {
            _workContext = workContext;
            _userRepository = userRepository;
        }

        public async Task Handle(UserSignedIn user, CancellationToken cancellationToken)
        {
            var guestUser = await _workContext.GetCurrentUser();
            if (guestUser == null || guestUser.Id == user.UserId) return;
            var signedInUser = await _userRepository.Query().SingleAsync(u => u.Id == user.UserId);
            signedInUser.Culture = guestUser.Culture;
            await _userRepository.SaveChangesAsync();
        }
    }
}
