using System.ComponentModel.DataAnnotations;

namespace ECommerce.Module.PaymentRazor.Areas.PaymentRazor.ViewModels
{
    public class RazorConfigForm
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        public string KeyId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string KeySecret { get; set; }

        public string WebhookSecret { get; set; }

        public string Currency { get; set; }

        public string PayoutAccountNumber { get; set; }

        public string PayoutMode { get; set; }
        public string PayoutUrl { get; set; }
    }
}
