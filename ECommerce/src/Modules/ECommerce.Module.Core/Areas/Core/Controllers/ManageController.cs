using System.Linq;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels.Manage;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [Authorize]
    public class ManageController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly ILogger _logger;
        private readonly SettingDefinitionProvider _settingDefinitionProvider;
        private readonly ISettingService _settingService;

        public ManageController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IEmailSender emailSender,
        ISmsSender smsSender,
        ILoggerFactory loggerFactory,
        SettingDefinitionProvider settingDefinitionProvider,
        ISettingService settingService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = loggerFactory.CreateLogger<ManageController>();
            _settingDefinitionProvider = settingDefinitionProvider;
            _settingService = settingService;
        }

        [HttpGet("user")]
        public async Task<IActionResult> Index(ManageMessageId? message = null)
        {
            if (User.IsInRole("admin") && Request.GetTypedHeaders().Accept?.Any(x =>
                string.Equals(x.MediaType.Value, "text/html", System.StringComparison.OrdinalIgnoreCase)) == true)
            {
                return LocalRedirect("~/admin#!/account");
            }

            var statusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }
            var model = new IndexViewModel
            {
                HasPassword = await _userManager.HasPasswordAsync(user),
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user),
                TwoFactor = await _userManager.GetTwoFactorEnabledAsync(user),
                Logins = await _userManager.GetLoginsAsync(user),
                BrowserRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user),
                FullName = user.FullName,
                Email = user.Email
            };
            return Ok(new { statusMessage, model, StatusCode = StatusCodes.Status200OK });
        }

        [HttpGet("user/info")]
        public async Task<IActionResult> UserInfo()
        {
            var user = await GetCurrentUserAsync();
            var model = new UserInfoVm { Email = user.Email, FullName = user.FullName };
            return Ok(model);
        }

        [HttpPost("user/info")]
        public async Task<IActionResult> UserInfo(UserInfoVm model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }
            user.FullName = model.FullName;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            return Ok(new { success = true , StatusCode = StatusCodes.Status200OK });
        }


        [HttpGet("user/settings")]
        public async Task<IActionResult> UserSettings()
        {
            var model = new UserSettingViewModel
            {
                SettingDefinitions = _settingDefinitionProvider.SettingDefinitions,
                UserSettings = await _settingService.GetAllSettingsAsync()
            };

            return Ok(model);
        }

        [HttpPost("user/settings")]
        public async Task<IActionResult> UserSettings(UserSettingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await GetCurrentUserAsync();
            foreach (var item in model.UserSettings)
            {
                _settingService.SetCustomSettingValueForUser(user, item.Key, item.Value);
            }

            await _userManager.UpdateAsync(user);

            return Ok(new { success = true , StatusCode = StatusCodes.Status200OK });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel account)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return NotFound();
            }
            var result = await _userManager.RemoveLoginAsync(user, account.LoginProvider, account.ProviderKey);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return Ok(new { success = true, StatusCode = StatusCodes.Status200OK });
            }
            return BadRequest(result.Errors);
        }

        [HttpGet("user/change-password")]
        public IActionResult ChangePassword()
        {
            return Ok();
        }

        [HttpPost("user/change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return NotFound();
            }
            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation(3, "User changed their password successfully.");
                return Ok(new { success = true, StatusCode = StatusCodes.Status200OK });
            }
            return BadRequest(result.Errors);
        }

        [HttpGet("user/set-password")]
        public IActionResult SetPassword()
        {
            return Ok();
        }

        [HttpPost("user/set-password")]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return NotFound();
            }
            var result = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return Ok(new { success = true, StatusCode = StatusCodes.Status200OK });
            }
            return BadRequest(result.Errors);
        }

        [HttpGet("user/manage-logins")]
        public async Task<IActionResult> ManageLogins(ManageMessageId? message = null)
        {
            var statusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.AddLoginSuccess ? "The external login was added."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return NotFound();
            }
            var userLogins = await _userManager.GetLoginsAsync(user);
            var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            var otherLogins = schemes.Where(auth => userLogins.All(ul => auth.Name != ul.LoginProvider)).ToList();
            var showRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return Ok(new
            {
                statusMessage,
                showRemoveButton,
                currentLogins = userLogins,
                otherLogins = otherLogins
            });
        }

        [HttpPost]
        public IActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Action("LinkLoginCallback", "Manage");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> LinkLoginCallback()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return NotFound();
            }
            var info = await _signInManager.GetExternalLoginInfoAsync(await _userManager.GetUserIdAsync(user));
            if (info == null)
            {
                return BadRequest(new { error = "External login info not found." });
            }
            var result = await _userManager.AddLoginAsync(user, info);
            if (result.Succeeded)
            {
                return Ok(new { success = true, StatusCode = StatusCodes.Status200OK });
            }
            return BadRequest(result.Errors);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            AddLoginSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

        private Task<User> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }
    }
}
