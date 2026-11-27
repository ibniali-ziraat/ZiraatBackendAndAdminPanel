using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Module.Core.Areas.Core.ViewModels;


public class UserProfile
{
    public long ProfileId { get; set; }

    public string Fullname { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; }
    public string Profilepic { get; set; }
    public string Password { get; set; }
    public string Confirmpassword { get; set; }
}

public class ExistingEmailPhoneNumberViewModel
{
    
    public string Email { get; set; }
    public string Phone { get; set; }
    
}

/// <summary>
/// Model for changing user password
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Current password (required for verification)
    /// </summary>
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; }

    /// <summary>
    /// New password (minimum 8 characters)
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(450, MinimumLength = 8, ErrorMessage = "New password must be between 8 and 450 characters")]
    public string NewPassword { get; set; }

    /// <summary>
    /// Confirm new password (must match NewPassword)
    /// </summary>
    [Required(ErrorMessage = "Confirm password is required")]
    [Compare("NewPassword", ErrorMessage = "New password and confirm password do not match")]
    public string ConfirmPassword { get; set; }
}
