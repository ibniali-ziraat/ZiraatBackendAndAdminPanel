using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels.Account;
using ECommerce.Module.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AdminAccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger _logger;

        public AdminAccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILoggerFactory loggerFactory)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = loggerFactory.CreateLogger<AdminAccountController>();
        }

        [HttpGet("admin/login")]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = string.IsNullOrWhiteSpace(returnUrl) ? "/admin#!/dashboard" : returnUrl;
            return View(new AdminLoginViewModel());
        }

        [HttpPost("admin/login")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = string.IsNullOrWhiteSpace(returnUrl) ? "/admin#!/dashboard" : returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName ?? user.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                if (!await _userManager.IsInRoleAsync(user, "admin") && !await _userManager.IsInRoleAsync(user, "vendor"))
                {
                    await _signInManager.SignOutAsync();
                    ModelState.AddModelError(string.Empty, "This account does not have admin access.");
                    return View(model);
                }

                _logger.LogInformation("Admin user logged in.");
                if (string.IsNullOrWhiteSpace(returnUrl) && await _userManager.IsInRoleAsync(user, "vendor"))
                {
                    return Redirect("/admin");
                }

                return RedirectToLocal(string.IsNullOrWhiteSpace(returnUrl) ? "/admin#!/dashboard" : returnUrl);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "This account is locked out.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpPost("admin/logoff")]
        [Authorize(AuthenticationSchemes = "Identity.Application")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet("admin/access-denied")]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect("/admin");
        }
    }
}
