using System;

namespace ECommerce.Module.PaymentMomo.ViewModels
{
    public class MomoCheckoutForm
    {
        public decimal PaymentFee { get; set; }

        public Guid CheckoutId { get; set; }
    }
}
