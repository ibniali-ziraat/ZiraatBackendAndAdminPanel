using System.ComponentModel.DataAnnotations;

namespace ECommerce.Module.Core.Areas.Core.ViewModels.Account
{
    public class LoginViewModel
    {
        // Keep Email optional (not used for mobile login)
        [EmailAddress(ErrorMessage = "The Email field is not a valid e-mail address.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        // Use MobileNumber as the required login identifier
        [Required(ErrorMessage = "The {0} field is required.")]
        [Phone(ErrorMessage = "The Mobile Number is not valid.")]
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
