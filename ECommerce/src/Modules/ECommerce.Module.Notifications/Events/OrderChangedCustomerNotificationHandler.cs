using System.Threading;
using System.Threading.Tasks;
using ECommerce.Module.Notifications.Models;
using ECommerce.Module.Orders.Events;
using ECommerce.Module.Orders.Models;
using MediatR;
using NotificationPublisher = ECommerce.Module.Notifications.Services.INotificationPublisher;

namespace ECommerce.Module.Notifications.Events
{
    public class OrderChangedCustomerNotificationHandler : INotificationHandler<OrderChanged>
    {
        private readonly NotificationPublisher _notificationPublisher;

        public OrderChangedCustomerNotificationHandler(NotificationPublisher notificationPublisher)
        {
            _notificationPublisher = notificationPublisher;
        }

        public async Task Handle(OrderChanged notification, CancellationToken cancellationToken)
        {
            if (notification.Order == null || notification.OldStatus == notification.NewStatus)
            {
                return;
            }

            var statusText = notification.NewStatus.ToString();
            await _notificationPublisher.PublishAsync(
                NotificationDefinitions.Names.CustomerOrderStatusChanged,
                new AlertNotificationData
                {
                    Title = "Order update",
                    Message = $"Your order #{notification.Order.Id} status is now {statusText}.",
                    AlertType = $"order-status-{statusText.ToLowerInvariant()}",
                    TargetApp = "customer",
                    OrderId = notification.Order.Id,
                    VendorId = notification.Order.VendorId,
                    CustomerId = notification.Order.CustomerId,
                    //Balance = notification.Order.OrderTotal
                },
                new EntityIdentifier(typeof(Order), notification.Order.Id),
                NotificationSeverity.Success,
                new[] { notification.Order.CustomerId }
            );
        }
    }
}
