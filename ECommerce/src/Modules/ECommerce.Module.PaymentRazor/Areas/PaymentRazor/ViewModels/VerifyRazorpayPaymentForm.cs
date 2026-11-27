using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Module.PaymentRazor.Areas.PaymentRazor.ViewModels
{
    public class VerifyRazorpayPaymentForm
    {
        [Required]
        public Guid CheckoutId { get; set; }

        [Required]
        public string RazorpayOrderId { get; set; }

        [Required]
        public string RazorpayPaymentId { get; set; }

        [Required]
        public string RazorpaySignature { get; set; }
    }
}
