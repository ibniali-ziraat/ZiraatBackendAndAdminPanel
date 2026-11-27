using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.ShoppingCart.Models;

namespace ECommerce.Module.Orders.Events
{
    public class OrderCreatedClearCartHandler : INotificationHandler<OrderCreated>
    {
        private readonly IRepository<CartItem> _cartItemRepository;

        public OrderCreatedClearCartHandler(IRepository<CartItem> cartItemRepository)
        {
            _cartItemRepository = cartItemRepository;
        }

        public async Task Handle(OrderCreated notification, CancellationToken cancellationToken)
        {
            foreach (var orderItem in notification.Order.OrderItems)
            {
                var cartItem = await _cartItemRepository.Query().FirstOrDefaultAsync(x =>
                    x.ProductId == orderItem.ProductId
                    && x.CustomerId == notification.Order.CustomerId
                    && x.VendorId == orderItem.VendorId
                    && x.VendorProductId == orderItem.VendorProductId
                    && x.VendorProductVariationId == orderItem.VendorProductVariationId);
                if (cartItem != null)
                {
                    _cartItemRepository.Remove(cartItem);
                }
            }

            await _cartItemRepository.SaveChangesAsync();
        }
    }
}
