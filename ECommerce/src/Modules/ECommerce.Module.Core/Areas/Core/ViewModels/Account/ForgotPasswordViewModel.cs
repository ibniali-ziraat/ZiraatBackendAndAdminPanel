using System.ComponentModel.DataAnnotations;

namespace ECommerce.Module.Core.Areas.Core.ViewModels.Account
{
    public class ForgotPasswordViewModel
    {
        [EmailAddress(ErrorMessage = "The Email field is not a valid e-mail address.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "The Phone Number is not valid.")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Email) || !string.IsNullOrWhiteSpace(PhoneNumber);
        }
    }
}
