using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Web;
using ECommerce.Module.Core.Areas.Core.ViewModels.Account;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IStringLocalizer _localizer;
        private readonly IEmailSender _emailSender;
        private readonly IOtpVerificationService _otpVerificationService;
        private readonly ILogger _logger;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IStringLocalizerFactory stringLocalizerFactory,
            IEmailSender emailSender,
            IOtpVerificationService otpVerificationService,
            ILoggerFactory loggerFactory)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _localizer = stringLocalizerFactory.Create(null);
            _emailSender = emailSender;
            _otpVerificationService = otpVerificationService;
            _logger = loggerFactory.CreateLogger<AccountController>();
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        [Route("login")]
        public IActionResult Login(string returnUrl = null)
        {
            return Ok(new { returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                _logger.LogInformation(1, "User logged in.");
                return Ok(new { success = true, returnUrl });
            }
            if (result.RequiresTwoFactor)
            {
                return Ok(new { requiresTwoFactor = true, returnUrl });
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning(2, "User account locked out.");
                return BadRequest(new { error = "LockedOut" });
            }

            return BadRequest(new { error = "Invalid login attempt." });
        }

        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        [Route("register")]
        public IActionResult Register(string returnUrl = null)
        {
            return Ok(new { returnUrl });
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new User { UserName = model.Email, Email = model.Email, FullName = model.FullName };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "customer");
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation(3, "User created a new account with password.");
                return Ok(new { success = true, returnUrl });
            }

            AddErrors(result);
            return BadRequest(ModelState);
        }

        // POST: /Account/LogOff
        [HttpPost]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation(4, "User logged out.");
            return Ok(new { success = true });
        }

        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        // GET: /Account/ExternalLoginCallback
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                return BadRequest(new { error = $"Error from external provider: {remoteError}" });
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return BadRequest(new { error = "External login info not found." });
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                _logger.LogInformation(5, "User logged in with {Name} provider.", info.LoginProvider);
                return Ok(new { success = true, returnUrl });
            }
            if (result.RequiresTwoFactor)
            {
                return Ok(new { requiresTwoFactor = true, returnUrl });
            }
            if (result.IsLockedOut)
            {
                return BadRequest(new { error = "LockedOut" });
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);
            return Ok(new ExternalLoginConfirmationViewModel { Email = email, FullName = name });
        }

        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginConfirmation([FromBody] ExternalLoginConfirmationViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return BadRequest(new { error = "External login info not found." });
            }

            var user = new User { UserName = model.Email, Email = model.Email, FullName = model.FullName };
            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "customer");
                result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation(6, "User created an account using {Name} provider.", info.LoginProvider);
                    return Ok(new { success = true, returnUrl });
                }
            }

            AddErrors(result);
            return BadRequest(ModelState);
        }

        // GET: /Account/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return BadRequest(new { error = "Invalid parameters." });
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                return Ok(new { success = true });
            }
            return BadRequest(new { success = false });
        }

        /// <summary>
        /// Request password reset via OTP sent to phone number
        /// </summary>
        /// <param name="model">ForgotPasswordViewModel containing phone number</param>
        /// <returns>Success response indicating OTP was sent</returns>
        [HttpPost]
        [AllowAnonymous]
        [Route("ForgotPassword")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                return BadRequest(new { error = "Phone number is required for OTP-based password reset." });
            }

            // Find user by phone number
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return BadRequest(new { success = true, message = "Unregistered phone number." });
            }

            // Send OTP via OtpVerificationService
            var otpResult = await _otpVerificationService.SendOtpAsync(model.PhoneNumber);
            if (string.IsNullOrWhiteSpace(otpResult))
            {
                _logger.LogError($"Failed to send OTP to {model.PhoneNumber}");
                return Ok(new { success = true, message = "Failed to send OTP to this number" });
            }

            _logger.LogInformation($"OTP sent successfully to {model.PhoneNumber} for password reset");

            return Ok(new
            {
                success = true,
                message = "An OTP has been sent to your registered phone number. Please use it to reset your password.",
                phoneNumber = MaskPhoneNumber(model.PhoneNumber)
            });
        }

        /// <summary>
        /// Verify OTP and reset password
        /// </summary>
        /// <param name="model">VerifyOtpAndResetPasswordViewModel containing phone, OTP, and new password</param>
        /// <returns>Success response if password reset successful</returns>
        [HttpPost]
        [AllowAnonymous]
        [Route("VerifyOtpAndResetPassword")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyOtpAndResetPassword([FromBody] VerifyOtpAndResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(model.PhoneNumber) || string.IsNullOrWhiteSpace(model.Otp) || string.IsNullOrWhiteSpace(model.NewPassword))
            {
                return BadRequest(new { error = "Phone number, OTP, and new password are required." });
            }

            // Verify OTP
            var otpVerificationResult = _otpVerificationService.VerifyOtp(model.PhoneNumber, model.Otp);
            if (otpVerificationResult != "OTP verified successfully")
            {
                return BadRequest(new { error = otpVerificationResult });
            }

            // Find user by phone number
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);
            if (user == null)
            {
                return BadRequest(new { error = "User not found." });
            }

            // Generate password reset token
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Reset password
            var result = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation($"Password reset successfully for user {user.Email}");
                return Ok(new { success = true, message = "Your password has been reset successfully. Please log in with your new password." });
            }

            AddErrors(result);
            return BadRequest(new { error = "Failed to reset password.", errors = ModelState });
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                return BadRequest(new { error = "Code is required." });
            }
            return Ok(new { code });
        }

        // POST: /Account/ResetPassword (Legacy email-based)
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var user = await _userManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                return Ok(new { success = true });
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return Ok(new { success = true });
            }
            AddErrors(result);
            return BadRequest(ModelState);
        }

        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return Ok();
        }

        // GET: /Account/SendCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl = null, bool rememberMe = false)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return BadRequest(new { error = "User not found." });
            }
            var userFactors = await _userManager.GetValidTwoFactorProvidersAsync(user);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return Ok(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SendCode([FromBody] SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return BadRequest(new { error = "User not found." });
            }

            var code = await _userManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { error = "Failed to generate code." });
            }

            var message = "Your security code is: " + code;
            if (model.SelectedProvider == "Email")
            {
                await _emailSender.SendEmailAsync(await _userManager.GetEmailAsync(user), "Security Code", message);
            }
            else if (model.SelectedProvider == "Phone")
            {
                await _otpVerificationService.SendOtpAsync(await _userManager.GetPhoneNumberAsync(user));
            }

            return Ok(new { success = true });
        }

        // GET: /Account/VerifyCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string provider, bool rememberMe, string returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return BadRequest(new { error = "User not found." });
            }
            return Ok(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
            if (result.Succeeded)
            {
                return Ok(new { success = true, returnUrl = model.ReturnUrl });
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning(7, "User account locked out.");
                return BadRequest(new { error = "LockedOut" });
            }

            return BadRequest(new { error = "Invalid code." });
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return Forbid();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private Task<User> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }

        private string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
                return phoneNumber;

            return phoneNumber.Substring(0, phoneNumber.Length - 4).PadRight(phoneNumber.Length, '*');
        }

        #endregion
    }
}
