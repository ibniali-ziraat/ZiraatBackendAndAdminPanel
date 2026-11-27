using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Module.PaymentRazor.Areas.PaymentRazor.ViewModels
{
    public class RazorpayPaymentCallbackForm
    {
        [Required]
        [FromForm(Name = "razorpay_order_id")]
        public string RazorpayOrderId { get; set; }

        [Required]
        [FromForm(Name = "razorpay_payment_id")]
        public string RazorpayPaymentId { get; set; }

        [Required]
        [FromForm(Name = "razorpay_signature")]
        public string RazorpaySignature { get; set; }
    }
}
