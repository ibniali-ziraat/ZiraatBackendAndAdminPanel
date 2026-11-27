using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.ActivityLog.Models;
using ECommerce.Module.Core.Events;
using ECommerce.Module.Core.Extensions;

namespace ECommerce.Module.ActivityLog.Events
{
    public class EntityViewedHandler : INotificationHandler<EntityViewed>
    {
        private readonly IRepository<Activity> _activityRepository;
        private readonly IWorkContext _workContext;
        private const long EntityViewedActivityTypeId = 1;

        public EntityViewedHandler(IRepository<Activity> activityRepository, IWorkContext workcontext)
        {
            _activityRepository = activityRepository;
            _workContext = workcontext;
        }

        public async Task Handle(EntityViewed notification, CancellationToken cancellationToken)
        {
            var user = await _workContext.GetCurrentUser();
            if (user == null) return;
            var activity = new Activity
            {
                ActivityTypeId = EntityViewedActivityTypeId,
                EntityId = notification.EntityId,
                EntityTypeId = notification.EntityTypeId,
                UserId = user.Id,
                CreatedOn = DateTimeOffset.Now
            };

            _activityRepository.Add(activity);
        }
    }
}
