using System;
using System.Collections.Generic;

namespace ECommerce.Module.Payments.Areas.Payments.ViewModels
{
    public class CheckoutPaymentForm
    {
        public IList<PaymentProviderVm> PaymentProviders { get; set; } = new List<PaymentProviderVm>();

        public Guid CheckoutId { get; set; }
    }
}
