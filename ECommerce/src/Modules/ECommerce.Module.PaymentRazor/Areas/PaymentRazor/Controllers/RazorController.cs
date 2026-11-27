using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Helpers;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Checkouts.Services;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Orders.Models;
using ECommerce.Module.Orders.Services;
using ECommerce.Module.PaymentRazor.Areas.PaymentRazor.ViewModels;
using ECommerce.Module.PaymentRazor.Models;
using ECommerce.Module.PaymentRazor.Services;
using ECommerce.Module.Payments.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IOrderService = ECommerce.Module.Orders.Services.IOrderService;

namespace ECommerce.Module.PaymentRazor.Areas.PaymentRazor.Controllers
{
    [Area("PaymentRazor")]
    [ApiController]
    [Route("api/paymentRazor")]
    [Authorize]
    public class RazorController : ControllerBase
    {

        private readonly ICheckoutService _checkoutService;
        private readonly IOrderService _orderService;
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly ICurrencyService _currencyService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IRazorpayVendorPayoutService _vendorPayoutService;
        private readonly IRepository<Order> _orderRepository;
        private readonly IWorkContext _workContext;

        public RazorController(
            ICheckoutService checkoutService,
            IOrderService orderService,
            IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository,
            IRepository<Payment> paymentRepository,
            ICurrencyService currencyService,
            IHttpClientFactory httpClientFactory,
            IRazorpayVendorPayoutService vendorPayoutService,
            IRepository<Order> orderRepository,
            IWorkContext workContext)
        {
            _checkoutService = checkoutService;
            _orderService = orderService;
            _paymentProviderRepository = paymentProviderRepository;
            _paymentRepository = paymentRepository;
            _currencyService = currencyService;
            _httpClientFactory = httpClientFactory;
            _vendorPayoutService = vendorPayoutService;
            _orderRepository = orderRepository;
            _workContext = workContext;
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder(CreateRazorpayOrderForm model = null)
        {
            if (model.CheckoutId == Guid.Empty)
            {
                return BadRequest(new { error = "checkoutId is required" });
            }

            var settings = await GetSettings();
            var validationError = ValidateSettings(settings);
            if (validationError != null)
            {
                return BadRequest(validationError);
            }

            var checkout = await _checkoutService.GetCheckoutDetails(model.CheckoutId);
            if (checkout == null)
            {
                return NotFound();
            }

            if (!checkout.IsValid || checkout.OrderTotal <= 0)
            {
                return BadRequest("Checkout is invalid or order total is not greater than zero");
            }

            var amountInSubunits = ToCurrencySubunits(checkout.OrderTotal);
            var currency = GetCurrency(settings);
            var orderRequest = new
            {
                amount = amountInSubunits,
                currency,
                receipt = $"co_{model.CheckoutId:N}",
                payment_capture = 1
            };

            var razorpayResponse = await SendRazorpayRequest(settings, HttpMethod.Post, "/orders", orderRequest);
            if (!razorpayResponse.IsSuccessStatusCode)
            {
                var error = await razorpayResponse.Content.ReadAsStringAsync();
                return BadRequest(new { error = "Failed to create Razorpay order", details = error });
            }

            var content = await razorpayResponse.Content.ReadAsStringAsync();
            var razorpayOrder = JObject.Parse(content);

            return Ok(new CreateRazorpayOrderResponse
            {
                KeyId = settings.KeyId,
                RazorpayOrderId = razorpayOrder.Value<string>("id"),
                Amount = razorpayOrder.Value<long?>("amount") ?? amountInSubunits,
                Currency = razorpayOrder.Value<string>("currency") ?? currency,
                CheckoutId = model.CheckoutId,
                OrderTotal = checkout.OrderTotal,
                Items = checkout.Items,
                CallbackUrl = Url.Action(nameof(PaymentCallback), "Razor", new { area = "PaymentRazor", model.CheckoutId }, Request.Scheme)
            });
        }

        [HttpPost("verify-payment")]
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyRazorpayPaymentForm model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return await VerifyAndCreateOrder(model);
        }

        [HttpPost("payment-callback")]
        public async Task<IActionResult> PaymentCallback([FromQuery] Guid checkoutId, [FromForm] RazorpayPaymentCallbackForm model)
        {
            if (checkoutId == Guid.Empty)
            {
                return BadRequest(new { error = "checkoutId is required" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return await VerifyAndCreateOrder(new VerifyRazorpayPaymentForm
            {
                CheckoutId = checkoutId,
                RazorpayOrderId = model.RazorpayOrderId,
                RazorpayPaymentId = model.RazorpayPaymentId,
                RazorpaySignature = model.RazorpaySignature
            });
        }

        [HttpPost("orders/{orderId:long}/release-vendor-payment")]
        [Authorize(Roles = "admin, vendor")]
        public async Task<IActionResult> ReleaseVendorPayment(long orderId, [FromBody] ReleaseVendorPaymentForm model)
        {
            try
            {
                var permission = await EnsureCanAccessOrder(orderId);
                if (permission != null)
                {
                    return permission;
                }

                var release = await _vendorPayoutService.ReleaseForOrder(orderId, model?.Force ?? false);
                return Ok(MapRelease(release));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet("orders/{orderId:long}/vendor-payment-release")]
        [Authorize(Roles = "admin, vendor")]
        public async Task<IActionResult> GetVendorPaymentRelease(long orderId)
        {
            try
            {
                var permission = await EnsureCanAccessOrder(orderId);
                if (permission != null)
                    return permission;

                var release = await _vendorPayoutService.GetReleaseByOrderId(orderId);

                if (release == null)
                    return NotFound();

                return Ok(MapRelease(release));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }

        [HttpGet("orders/{orderId:long}/vendor-payment-release/status")]
        [Authorize(Roles = "admin, vendor")]
        public async Task<IActionResult> GetVendorPaymentReleaseStatus(long orderId)
        {
            try
            {
                var permission = await EnsureCanAccessOrder(orderId);
                if (permission != null)
                    return permission;

                var release = await _vendorPayoutService.RefreshPayoutStatusForOrder(orderId);

                if (release == null)
                    return NotFound();

                return Ok(MapRelease(release));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }

        private async Task<IActionResult> EnsureCanAccessOrder(long orderId)
        {
            var order = await _orderRepository.Query().FirstOrDefaultAsync(x => x.Id == orderId);
            if (order == null)
            {
                return NotFound();
            }

            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin") && order.VendorId != currentUser.VendorId)
            {
                return BadRequest(new { error = "You don't have permission to manage this order" });
            }

            return null;
        }

        private async Task<IActionResult> VerifyAndCreateOrder(VerifyRazorpayPaymentForm model)
        {
            var settings = await GetSettings();
            var validationError = ValidateSettings(settings);
            if (validationError != null)
            {
                return BadRequest(validationError);
            }

            if (!VerifySignature(model.RazorpayOrderId, model.RazorpayPaymentId, model.RazorpaySignature, settings.KeySecret))
            {
                return BadRequest("Invalid Razorpay payment signature");
            }

            var existingPayment = await _paymentRepository.Query()
                .Include(x => x.Order)
                .FirstOrDefaultAsync(x =>
                    x.PaymentMethod == PaymentProviderHelper.RazorpayPaymentMethod &&
                    x.GatewayTransactionId == model.RazorpayPaymentId &&
                    x.Status == PaymentStatus.Succeeded);

            if (existingPayment != null)
            {
                return Ok(new { success = true, orderId = existingPayment.OrderId });
            }

            var checkout = await _checkoutService.GetCheckoutDetails(model.CheckoutId);
            if (checkout == null)
            {
                return NotFound();
            }

            var paymentStatus = await EnsurePaymentCaptured(settings, model.RazorpayPaymentId, GetCurrency(settings));
            if (!paymentStatus.Success)
            {
                return BadRequest(paymentStatus.Error);
            }

            var expectedAmount = ToCurrencySubunits(checkout.OrderTotal);
            if (paymentStatus.Amount.HasValue && paymentStatus.Amount.Value != expectedAmount)
            {
                return BadRequest("Razorpay payment amount does not match checkout total");
            }

            var orderCreationResult = await _orderService.CreateOrder(model.CheckoutId, PaymentProviderHelper.RazorpayPaymentMethod, 0, OrderStatus.New);
            if (!orderCreationResult.Success)
            {
                return BadRequest(orderCreationResult.Error);
            }

            var order = orderCreationResult.Value;
            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.OrderTotal,
                PaymentMethod = PaymentProviderHelper.RazorpayPaymentMethod,
                GatewayTransactionId = model.RazorpayPaymentId,
                Status = PaymentStatus.Succeeded,
                CreatedOn = DateTimeOffset.UtcNow,
                LatestUpdatedOn = DateTimeOffset.UtcNow
            };

            _paymentRepository.Add(payment);
            await _paymentRepository.SaveChangesAsync();

            return Ok(orderCreationResult);
        }

        private static VendorPaymentReleaseVm MapRelease(VendorPaymentRelease release)
        {
            return new VendorPaymentReleaseVm
            {
                Id = release.Id,
                OrderId = release.OrderId,
                VendorId = release.VendorId,
                FinancialPaymentId = release.FinancialPaymentId,
                BankDetailId = release.BankDetailId,
                Amount = release.Amount,
                Currency = release.Currency,
                PaymentMethod = release.PaymentMethod,
                Status = release.Status.ToString(),
                RazorpayContactId = release.RazorpayContactId,
                RazorpayFundAccountId = release.RazorpayFundAccountId,
                RazorpayPayoutId = release.RazorpayPayoutId,
                RazorpayPayoutStatus = release.RazorpayPayoutStatus,
                ReferenceId = release.ReferenceId,
                FailureMessage = release.FailureMessage,
                CreatedOn = release.CreatedOn,
                LatestUpdatedOn = release.LatestUpdatedOn,
                ReleasedOn = release.ReleasedOn
            };
        }

        private async Task<RazorConfigForm> GetSettings()
        {
            var razorProvider = await _paymentProviderRepository.Query().FirstOrDefaultAsync(x => x.Id == PaymentProviderHelper.RazorpayProviderId);
            var settings = string.IsNullOrWhiteSpace(razorProvider?.AdditionalSettings)
                ? new RazorConfigForm()
                : JsonConvert.DeserializeObject<RazorConfigForm>(razorProvider.AdditionalSettings) ?? new RazorConfigForm();

            settings.KeyId = settings.KeyId;
            settings.KeySecret = settings.KeySecret;
            settings.WebhookSecret = settings.WebhookSecret;
            settings.Currency = settings.Currency;
            settings.PayoutUrl = settings.PayoutUrl;
            return settings;
        }

        private string ValidateSettings(RazorConfigForm settings)
        {
            if (string.IsNullOrWhiteSpace(settings.KeyId) || string.IsNullOrWhiteSpace(settings.KeySecret))
            {
                return "Razorpay KeyId and KeySecret are not configured";
            }

            return null;
        }

        private async Task<HttpResponseMessage> SendRazorpayRequest(RazorConfigForm settings, HttpMethod method, string path, object body = null, string idempotencyKey = null)
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(method, settings.PayoutUrl + path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.KeyId}:{settings.KeySecret}")));

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                request.Headers.Add("X-Idempotency-Key", idempotencyKey);
            }

            if (body != null)
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            }

            return await client.SendAsync(request);

        }


        private async Task<(bool Success, string Error, long? Amount)> EnsurePaymentCaptured(RazorConfigForm settings, string razorpayPaymentId, string currency)
        {
            var fetchResponse = await SendRazorpayRequest(settings, HttpMethod.Get, $"/payments/{razorpayPaymentId}");
            var fetchContent = await fetchResponse.Content.ReadAsStringAsync();
            if (!fetchResponse.IsSuccessStatusCode)
            {
                return (false, $"Unable to fetch Razorpay payment: {fetchContent}", null);
            }

            var payment = JObject.Parse(fetchContent);
            var status = payment.Value<string>("status");
            var amount = payment.Value<long?>("amount");

            if (status == "captured")
            {
                return (true, null, amount);
            }

            if (status == "authorized")
            {
                var captureResponse = await SendRazorpayRequest(settings, HttpMethod.Post, $"/payments/{razorpayPaymentId}/capture", new { amount, currency });
                var captureContent = await captureResponse.Content.ReadAsStringAsync();
                if (!captureResponse.IsSuccessStatusCode)
                {
                    return (false, $"Unable to capture Razorpay payment: {captureContent}", amount);
                }

                var capturedPayment = JObject.Parse(captureContent);
                return (capturedPayment.Value<string>("status") == "captured", "Razorpay payment was not captured", capturedPayment.Value<long?>("amount") ?? amount);
            }

            return (false, $"Unexpected Razorpay payment status: {status}", amount);
        }

        private bool VerifySignature(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature, string keySecret)
        {
            var payload = $"{razorpayOrderId}|{razorpayPaymentId}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(keySecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var expectedSignature = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();

            var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
            var actualBytes = Encoding.UTF8.GetBytes(razorpaySignature ?? string.Empty);

            return expectedBytes.Length == actualBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }

        private long ToCurrencySubunits(decimal amount)
        {
            var normalizedAmount = amount;
            if (!CurrencyHelper.IsZeroDecimalCurrencies(_currencyService.CurrencyCulture))
            {
                normalizedAmount *= 100;
            }

            return Convert.ToInt64(decimal.Round(normalizedAmount, 0, MidpointRounding.AwayFromZero));
        }

        private string GetCurrency(RazorConfigForm settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.Currency))
            {
                return settings.Currency.ToUpperInvariant();
            }

            var regionInfo = new RegionInfo(_currencyService.CurrencyCulture.LCID);
            return regionInfo.ISOCurrencySymbol;
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
