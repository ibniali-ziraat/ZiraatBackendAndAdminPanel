using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Notifications.Models;
using ECommerce.Module.Orders.Events;
using ECommerce.Module.Orders.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotificationPublisher = ECommerce.Module.Notifications.Services.INotificationPublisher;

namespace ECommerce.Module.Notifications.Events
{
    public class OrderCreatedVendorNotificationHandler : INotificationHandler<OrderCreated>
    {
        private readonly NotificationPublisher _notificationPublisher;
        private readonly IRepository<User> _userRepository;

        public OrderCreatedVendorNotificationHandler(
            NotificationPublisher notificationPublisher,
            IRepository<User> userRepository)
        {
            _notificationPublisher = notificationPublisher;
            _userRepository = userRepository;
        }

        public async Task Handle(OrderCreated notification, CancellationToken cancellationToken)
        {
            var order = notification.Order;
            var vendorIds = order.VendorId.HasValue
                ? new[] { order.VendorId.Value }
                : order.OrderItems
                    .Where(x => x.VendorId.HasValue)
                    .Select(x => x.VendorId.Value)
                    .Distinct()
                    .ToArray();

            if (!vendorIds.Any())
            {
                return;
            }

            var vendorUserIds = await _userRepository.Query()
                .Where(x => x.VendorId.HasValue && vendorIds.Contains(x.VendorId.Value) && !x.IsDeleted)
                .Select(x => x.Id)
                .ToArrayAsync(cancellationToken);

            if (!vendorUserIds.Any())
            {
                return;
            }

            await _notificationPublisher.PublishAsync(
                NotificationDefinitions.Names.VendorOrderPlaced,
                new AlertNotificationData
                {
                    Title = "New order received",
                    Message = $"Order #{order.Id} has been placed.",
                    AlertType = "order-placed",
                    TargetApp = "vendor",
                    OrderId = order.Id,
                    VendorId = order.VendorId,
                    CustomerId = order.CustomerId,
                    Balance = order.OrderTotal
                },
                new EntityIdentifier(typeof(Order), order.Id),
                NotificationSeverity.Success,
                vendorUserIds);
        }
    }
}
