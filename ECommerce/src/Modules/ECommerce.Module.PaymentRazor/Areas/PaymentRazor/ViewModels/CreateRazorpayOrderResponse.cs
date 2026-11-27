using System;
using System.Collections.Generic;
using ECommerce.Module.Checkouts.Areas.Checkouts.ViewModels;

namespace ECommerce.Module.PaymentRazor.Areas.PaymentRazor.ViewModels
{
    public class CreateRazorpayOrderResponse
    {
        public string KeyId { get; set; }

        public string RazorpayOrderId { get; set; }

        public long Amount { get; set; }

        public string Currency { get; set; }

        public Guid CheckoutId { get; set; }

        public decimal OrderTotal { get; set; }

        public IList<CheckoutItemVm> Items { get; set; } = new List<CheckoutItemVm>();

        public string CallbackUrl { get; set; }
    }
}
