using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Localization.Areas.Localization.Controllers
{
    [Area("Localization")]
    public class LocalizationController :ControllerBase
    {
        private readonly IRepositoryWithTypedId<User, long> _userRepository;
        private readonly IWorkContext _workContext;

        public LocalizationController(IRepositoryWithTypedId<User, long> userRepository, IWorkContext workContext)
        {
            _userRepository = userRepository;
            _workContext = workContext;
        }

        [HttpPost]
        public async Task<IActionResult> SetLanguage(string culture, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(culture)) return BadRequest();
            try
            {
                culture = CultureInfo.GetCultureInfo(culture).Name;
            }
            catch (CultureNotFoundException)
            {
                return BadRequest();
            }
            var currentUser = await _workContext.GetCurrentUser();

            if (currentUser != null)
            {
                currentUser.Culture = culture;
                _userRepository.SaveChanges();
            }

            Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                });

            return LocalRedirect(returnUrl);
        }
    }
}
