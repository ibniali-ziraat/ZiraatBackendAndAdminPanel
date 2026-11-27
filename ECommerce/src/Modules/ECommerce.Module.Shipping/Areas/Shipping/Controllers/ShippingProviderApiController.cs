using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Shipping.Models;

namespace ECommerce.Module.Shipping.Areas.Shipping.Controllers
{
    [Area("Shipping")]
    [Route("api/shipping-providers")]
    public class ShippingProviderApiController : ControllerBase
    {
        private readonly IRepositoryWithTypedId<ShippingProvider, string> _shippingProviderRepositor;

        public ShippingProviderApiController(IRepositoryWithTypedId<ShippingProvider, string> shippingProviderRepositor)
        {
            _shippingProviderRepositor = shippingProviderRepositor;
        }
        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var providers = await _shippingProviderRepositor.Query()
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.IsEnabled,
                    x.ConfigureUrl
                }).ToListAsync();

            return new JsonResult(providers);
        }
        [Authorize]
        [HttpGet("GetShippmentMethods")]
        public async Task<IActionResult> GetShippmentMethods()
        {
            var providers = await _shippingProviderRepositor.Query()
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.IsEnabled,
                })
                .Where(x => x.IsEnabled == true)
                .ToListAsync();

            return new JsonResult(providers);
        }

        [Authorize(Roles = "admin")]
        [HttpPost("{id}/enable")]
        public async Task<IActionResult> Enable(string id)
        {
            var provider = await _shippingProviderRepositor.Query().FirstOrDefaultAsync(x => x.Id == id);
            provider.IsEnabled = true;
            await _shippingProviderRepositor.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "admin")]
        [HttpPost("{id}/disable")]
        public async Task<IActionResult> Disable(string id)
        {
            var provider = await _shippingProviderRepositor.Query().FirstOrDefaultAsync(x => x.Id == id);
            provider.IsEnabled = false;
            await _shippingProviderRepositor.SaveChangesAsync();
            return NoContent();
        }
    }
}
