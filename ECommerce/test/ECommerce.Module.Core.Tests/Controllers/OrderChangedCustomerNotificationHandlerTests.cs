using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Module.Notifications;
using ECommerce.Module.Notifications.Events;
using ECommerce.Module.Notifications.Models;
using ECommerce.Module.Notifications.Services;
using ECommerce.Module.Orders.Events;
using ECommerce.Module.Orders.Models;
using Moq;
using Xunit;

namespace ECommerce.Module.Core.Tests.Controllers
{
    public class OrderChangedCustomerNotificationHandlerTests
    {
        public static IEnumerable<object[]> Statuses =>
            Enum.GetValues<OrderStatus>().Select(status => new object[] { status });

        [Theory]
        [MemberData(nameof(Statuses))]
        public async Task PublishesCustomerNotificationForEveryChangedStatus(OrderStatus newStatus)
        {
            var publisher = new Mock<INotificationPublisher>();
            var handler = new OrderChangedCustomerNotificationHandler(publisher.Object);
            var order = new Order { CustomerId = 42, VendorId = 10014 };
            var oldStatus = newStatus == OrderStatus.New ? OrderStatus.OnHold : OrderStatus.New;

            await handler.Handle(
                new OrderChanged
                {
                    OrderId = order.Id,
                    Order = order,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    UserId = 7,
                },
                CancellationToken.None
            );

            publisher.Verify(
                x =>
                    x.PublishAsync(
                        NotificationDefinitions.Names.CustomerOrderStatusChanged,
                        It.Is<AlertNotificationData>(data =>
                            data.Title == "Order update"
                            && data.Message.Contains(newStatus.ToString())
                            && data.AlertType
                                == "order-status-" + newStatus.ToString().ToLowerInvariant()
                            && data.CustomerId == order.CustomerId
                            && data.VendorId == order.VendorId
                        ),
                        It.Is<EntityIdentifier>(entity =>
                            entity.Type == typeof(Order) && (long)entity.Id == order.Id
                        ),
                        NotificationSeverity.Success,
                        It.Is<long[]>(userIds => userIds.SequenceEqual(new[] { order.CustomerId })),
                        null
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task DoesNotPublishWhenStatusDidNotChange()
        {
            var publisher = new Mock<INotificationPublisher>();
            var handler = new OrderChangedCustomerNotificationHandler(publisher.Object);
            var order = new Order { CustomerId = 42, VendorId = 10014 };

            await handler.Handle(
                new OrderChanged
                {
                    OrderId = order.Id,
                    Order = order,
                    OldStatus = OrderStatus.Shipping,
                    NewStatus = OrderStatus.Shipping,
                },
                CancellationToken.None
            );

            publisher.Verify(
                x =>
                    x.PublishAsync(
                        It.IsAny<string>(),
                        It.IsAny<NotificationData>(),
                        It.IsAny<EntityIdentifier>(),
                        It.IsAny<NotificationSeverity>(),
                        It.IsAny<long[]>(),
                        It.IsAny<long[]>()
                    ),
                Times.Never
            );
        }
    }
}
