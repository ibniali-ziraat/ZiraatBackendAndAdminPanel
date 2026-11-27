using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.PaymentRazor.Areas.PaymentRazor.ViewModels;
using ECommerce.Module.PaymentRazor.Models;
using ECommerce.Module.Payments.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ECommerce.Module.PaymentRazor.Areas.PaymentRazor.Controllers
{
    [Area("PaymentRazor")]
    [Authorize(Roles = "admin")]
    [Route("api/razor")]
    public class RazorApiController : ControllerBase
    {
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        private readonly IConfiguration _configuration;

        public RazorApiController(
            IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository,
            IConfiguration configuration)
        {
            _paymentProviderRepository = paymentProviderRepository;
            _configuration = configuration;
        }

        [HttpGet("config")]
        public async Task<IActionResult> Config()
        {
            var razorProvider = await _paymentProviderRepository.Query().FirstOrDefaultAsync(x => x.Id == PaymentProviderHelper.RazorpayProviderId);
            var model = DeserializeSettings(razorProvider?.AdditionalSettings);
            ApplyAppSettingsFallback(model);
            return Ok(model);
        }

        [HttpPut("config")]
        public async Task<IActionResult> Config([FromBody] RazorConfigForm model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var razorProvider = await _paymentProviderRepository.Query().FirstOrDefaultAsync(x => x.Id == PaymentProviderHelper.RazorpayProviderId);
            if (razorProvider == null)
            {
                return NotFound("Razorpay payment provider is not registered");
            }

            razorProvider.AdditionalSettings = JsonConvert.SerializeObject(model);
            await _paymentProviderRepository.SaveChangesAsync();
            return Accepted();
        }

        private RazorConfigForm DeserializeSettings(string settings)
        {
            if (string.IsNullOrWhiteSpace(settings))
            {
                return new RazorConfigForm();
            }

            return JsonConvert.DeserializeObject<RazorConfigForm>(settings) ?? new RazorConfigForm();
        }

        private void ApplyAppSettingsFallback(RazorConfigForm model)
        {
            model.KeyId = FirstNonEmpty(model.KeyId, _configuration["Payments:Razorpay:KeyId"]);
            model.KeySecret = FirstNonEmpty(model.KeySecret, _configuration["Payments:Razorpay:KeySecret"]);
            model.WebhookSecret = FirstNonEmpty(model.WebhookSecret, _configuration["Payments:Razorpay:WebhookSecret"]);
            model.Currency = FirstNonEmpty(model.Currency, _configuration["Payments:Razorpay:Currency"], "INR");
            model.PayoutAccountNumber = FirstNonEmpty(model.PayoutAccountNumber, _configuration["Payments:Razorpay:PayoutAccountNumber"]);
            model.PayoutMode = FirstNonEmpty(model.PayoutMode, _configuration["Payments:Razorpay:PayoutMode"], "IMPS");
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }
    }
}
