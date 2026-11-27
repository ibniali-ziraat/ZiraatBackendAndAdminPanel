using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [Authorize(AuthenticationSchemes = "Identity.Application", Roles = "admin, vendor")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeAdminController : Controller
    {
        private readonly IAntiforgery _antiforgery;

        public HomeAdminController(IAntiforgery antiforgery)
        {
            _antiforgery = antiforgery;
        }
        [HttpGet("admin")]
        public IActionResult Index()
        { 
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            HttpContext.Response.Cookies.Append("XSRF-TOKEN",
                tokens.RequestToken, new CookieOptions { HttpOnly = false, IsEssential = true }
            );

            return View();
        }
    }
}
