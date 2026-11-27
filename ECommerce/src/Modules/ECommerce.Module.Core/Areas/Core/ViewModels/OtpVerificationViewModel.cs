using System;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class OtpVerificationViewModel
    {
        public OtpVerificationViewModel() { }

        // Use phone number as the identifier
        public string PhoneNumber { get; set; }

        public string Otp { get; set; }
    }
}
