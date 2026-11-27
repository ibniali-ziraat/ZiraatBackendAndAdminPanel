using System.Threading.Tasks;
using ECommerce.Module.Pricing.Services;
using ECommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels;

namespace ECommerce.Module.ShoppingCart.Services
{
    public interface ICartService
    {
        Task<AddToCartResult> AddToCart(long customerId, long productId, int quantity, long? vendorProductId = null, long? vendorId = null, string variationName = null, long? vendorProductVariationId = null);

        Task<CartVm> GetCartDetails(long customerId);

        Task<CouponValidationResult> ApplyCoupon(long customerId, string couponCode);

        Task MigrateCart(long fromUserId, long toUserId);
      //  Task GetActiveCartDetails(long customerId, long id);
      //  Task GetActiveCartDetails(long id);
      //  Task AddToCart(long customerId, long id, long productId, int quantity);
    }
}
