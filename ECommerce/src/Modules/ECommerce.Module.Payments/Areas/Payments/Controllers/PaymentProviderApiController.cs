using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Payments.Models;

namespace ECommerce.Module.Payments.Areas.Payments.Controllers
{
    [Area("Payments")]
    [Route("api/payments-providers")]
    public class PaymentProviderApiController : ControllerBase
    {
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;

        public PaymentProviderApiController(IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepositor)
        {
            _paymentProviderRepository = paymentProviderRepositor;
        }
        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var providers = await _paymentProviderRepository.Query()
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
        [HttpGet("GetPaymentProviders")]
        public async Task<IActionResult> GetPaymentProviders()
        {
            var providers = await _paymentProviderRepository.Query()
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.IsEnabled,
                }).Where(x => x.IsEnabled == true)
                .ToListAsync();

            return new JsonResult(providers);
        }
        [Authorize(Roles = "admin")]
        [HttpPost("{id}/enable")]
        public async Task<IActionResult> Enable(string id)
        {
            var provider = await _paymentProviderRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            provider.IsEnabled = true;
            await _paymentProviderRepository.SaveChangesAsync();
            return NoContent();
        }
        [Authorize(Roles = "admin")]
        [HttpPost("{id}/disable")]
        public async Task<IActionResult> Disable(string id)
        {
            var provider = await _paymentProviderRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            provider.IsEnabled = false;
            await _paymentProviderRepository.SaveChangesAsync();
            return NoContent();
        }
    }
}
