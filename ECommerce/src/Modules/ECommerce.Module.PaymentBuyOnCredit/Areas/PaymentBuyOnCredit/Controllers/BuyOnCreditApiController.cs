using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.PaymentBuyOnCredit.Models;
using ECommerce.Module.Payments.Models;
using System;

namespace ECommerce.Module.PaymentBuyOnCredit.Areas.PaymentBuyOnCredit.Controllers
{
    [Authorize(Roles = "admin")]
    [Area("PaymentBuyOnCredit")]
    [Route("api/buy-on-credit")]
    public class BuyOnCreditApiController : ControllerBase
    {
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;

        public BuyOnCreditApiController(IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository)
        {
            _paymentProviderRepository = paymentProviderRepository;
        }

        /// <summary>
        /// Get BuyOnCredit payment settings
        /// </summary>
        [HttpGet("config")]
        public async Task<IActionResult> Config()
        {
            try
            {
                var bocProvider = await _paymentProviderRepository.Query()
                    .FirstOrDefaultAsync(x => x.Id == PaymentProviderHelper.BuyOnCreditProviderId);

                if (bocProvider == null)
                {
                    return Ok(new BuyOnCreditSetting { PaymentFee = 0 });
                }

                if (string.IsNullOrEmpty(bocProvider.AdditionalSettings))
                {
                    return Ok(new BuyOnCreditSetting { PaymentFee = 0 });
                }

                var model = JsonConvert.DeserializeObject<BuyOnCreditSetting>(bocProvider.AdditionalSettings);
                return Ok(model);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update BuyOnCredit payment settings
        /// </summary>
        [HttpPut("config")]
        public async Task<IActionResult> Config([FromBody] BuyOnCreditSetting model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var bocProvider = await _paymentProviderRepository.Query()
                    .FirstOrDefaultAsync(x => x.Id == PaymentProviderHelper.BuyOnCreditProviderId);

                if (bocProvider == null)
                {
                    return NotFound(new { error = "BuyOnCredit payment provider not configured" });
                }

                bocProvider.AdditionalSettings = JsonConvert.SerializeObject(model);
                await _paymentProviderRepository.SaveChangesAsync();

                return Accepted(new { message = "BuyOnCredit settings updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
