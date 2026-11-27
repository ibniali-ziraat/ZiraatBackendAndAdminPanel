using System.Threading;
using System.Threading.Tasks;
using ECommerce.Module.Orders.Events;
using ECommerce.Module.Orders.Models;
using ECommerce.Module.PaymentRazor.Services;
using MediatR;

namespace ECommerce.Module.PaymentRazor.Events
{
    public class RazorpayOrderCompletedHandler : INotificationHandler<OrderChanged>
    {
        private readonly IRazorpayVendorPayoutService _vendorPayoutService;

        public RazorpayOrderCompletedHandler(IRazorpayVendorPayoutService vendorPayoutService)
        {
            _vendorPayoutService = vendorPayoutService;
        }

        public async Task Handle(OrderChanged notification, CancellationToken cancellationToken)
        {
            if (notification.NewStatus != OrderStatus.Complete || notification.OldStatus == OrderStatus.Complete)
            {
                return;
            }

            try
            {
                await _vendorPayoutService.ReleaseForOrder(notification.OrderId);
            }
            catch
            {
                // Order completion should not be rolled back by payout configuration/API issues.
                // The manual release endpoint can be used to retry and surface the exact error.
            }
        }
    }
}
