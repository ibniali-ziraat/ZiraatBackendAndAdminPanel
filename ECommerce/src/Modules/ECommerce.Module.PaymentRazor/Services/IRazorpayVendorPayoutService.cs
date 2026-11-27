using System.Threading.Tasks;
using ECommerce.Module.PaymentRazor.Models;

namespace ECommerce.Module.PaymentRazor.Services
{
    public interface IRazorpayVendorPayoutService
    {
        Task<VendorPaymentRelease> ReleaseForOrder(long orderId, bool force = false);

        Task<VendorPaymentRelease> GetReleaseByOrderId(long orderId);

        Task<VendorPaymentRelease> RefreshPayoutStatusForOrder(long orderId);
    }
}
