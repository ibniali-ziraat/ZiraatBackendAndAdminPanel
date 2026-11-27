using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Helpers;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Orders.Models;
using ECommerce.Module.PaymentRazor.Areas.PaymentRazor.ViewModels;
using ECommerce.Module.PaymentRazor.Models;
using ECommerce.Module.Payments.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ECommerce.Module.PaymentRazor.Services
{
    public class RazorpayVendorPayoutService : IRazorpayVendorPayoutService
    {

        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRepository<VendorPaymentRelease> _releaseRepository;
        private readonly IRepository<BusinessInformation> _businessInformationRepository;
        private readonly IRepository<FinancialPayment> _financialPaymentRepository;
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICurrencyService _currencyService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RazorpayVendorPayoutService> _logger;

        public RazorpayVendorPayoutService(
            IRepository<Order> orderRepository,
            IRepository<Payment> paymentRepository,
            IRepository<VendorPaymentRelease> releaseRepository,
            IRepository<BusinessInformation> businessInformationRepository,
            IRepository<FinancialPayment> financialPaymentRepository,
            IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository,
            IHttpClientFactory httpClientFactory,
            ICurrencyService currencyService,
            IConfiguration configuration,
            ILogger<RazorpayVendorPayoutService> logger)
        {
            _orderRepository = orderRepository;
            _paymentRepository = paymentRepository;
            _releaseRepository = releaseRepository;
            _businessInformationRepository = businessInformationRepository;
            _financialPaymentRepository = financialPaymentRepository;
            _paymentProviderRepository = paymentProviderRepository;
            _httpClientFactory = httpClientFactory;
            _currencyService = currencyService;
            _configuration = configuration;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private async Task ValidateExistingRazorpayIds(RazorConfigForm settings, VendorPaymentRelease release)
        {
            // If contact id is present, GET it to ensure it belongs to current key; if not found, clear
            if (!string.IsNullOrWhiteSpace(release.RazorpayContactId))
            {
                var contactResp = await SendRazorpayRequest(settings, HttpMethod.Get, $"contacts/{release.RazorpayContactId}");
                var contactContent = await contactResp.Content.ReadAsStringAsync();
                if (!contactResp.IsSuccessStatusCode)
                {
                    // Clear stored ids so they can be recreated with current keys
                    release.RazorpayContactId = null;
                    release.RazorpayFundAccountId = null;
                    release.ResponsePayload = (release.ResponsePayload ?? string.Empty) + "\nVALIDATE_CONTACT_RESPONSE:\n" + contactContent;
                    release.LatestUpdatedOn = DateTimeOffset.UtcNow;
                    await _releaseRepository.SaveChangesAsync();
                }
            }

            if (!string.IsNullOrWhiteSpace(release.RazorpayFundAccountId))
            {
                var fundResp = await SendRazorpayRequest(settings, HttpMethod.Get, $"fund_accounts/{release.RazorpayFundAccountId}");
                var fundContent = await fundResp.Content.ReadAsStringAsync();
                if (!fundResp.IsSuccessStatusCode)
                {
                    release.RazorpayFundAccountId = null;
                    release.ResponsePayload = (release.ResponsePayload ?? string.Empty) + "\nVALIDATE_FUND_RESPONSE:\n" + fundContent;
                    release.LatestUpdatedOn = DateTimeOffset.UtcNow;
                    await _releaseRepository.SaveChangesAsync();
                }
            }
        }

        public async Task<VendorPaymentRelease> GetReleaseByOrderId(long orderId)
        {
            return await _releaseRepository.Query()
                .Include(x => x.BankDetail)
                .Include(x => x.FinancialPayment)
                .FirstOrDefaultAsync(x => x.OrderId == orderId);
        }

        public async Task<VendorPaymentRelease> RefreshPayoutStatusForOrder(long orderId)
        {
            var release = await GetReleaseByOrderId(orderId);
            if (release == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(release.RazorpayPayoutId))
            {
                throw new InvalidOperationException("Razorpay payout id is not available for this payment release.");
            }

            var settings = await GetSettings();
            ValidateSettings(settings);

            var payoutResponse = await SendRazorpayRequest(settings, HttpMethod.Get, $"payouts/{release.RazorpayPayoutId}");
            var payoutContent = await payoutResponse.Content.ReadAsStringAsync();

            if (!payoutResponse.IsSuccessStatusCode)
            {
                release.FailureMessage = BuildPayoutFailureMessage(payoutContent);
                release.LatestUpdatedOn = DateTimeOffset.UtcNow;
                await _releaseRepository.SaveChangesAsync();
                throw new InvalidOperationException(release.FailureMessage);
            }

            var payout = JObject.Parse(payoutContent);
            release.ResponsePayload = payoutContent;
            release.RazorpayPayoutStatus = payout.Value<string>("status");
            ApplyPayoutStatus(release, payout);
            release.LatestUpdatedOn = DateTimeOffset.UtcNow;

            await _releaseRepository.SaveChangesAsync();
            return release;
        }

        public async Task<VendorPaymentRelease> ReleaseForOrder(long orderId, bool force = false)
        {
            var order = await _orderRepository.Query()
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order == null)
            {
                throw new InvalidOperationException("Order not found.");
            }

            if (order.OrderStatus != OrderStatus.Complete)
            {
                throw new InvalidOperationException("Vendor payment can be released only after order status is Complete.");
            }

            if (!order.VendorId.HasValue)
            {
                throw new InvalidOperationException("Order is not associated with a vendor.");
            }

            var existingRelease = await _releaseRepository.Query().FirstOrDefaultAsync(x => x.OrderId == order.Id);
            if (existingRelease != null && existingRelease.Status == VendorPaymentReleaseStatus.Released && !force)
            {
                return existingRelease;
            }

            if (existingRelease != null && existingRelease.Status == VendorPaymentReleaseStatus.Processing && !force)
            {
                return existingRelease;
            }

            var payment = await _paymentRepository.Query()
                .FirstOrDefaultAsync(x =>
                    x.OrderId == order.Id &&
                    x.PaymentMethod == PaymentProviderHelper.RazorpayPaymentMethod &&
                    x.Status == PaymentStatus.Succeeded);


            if (payment == null)
            {
                return await MarkSkipped(existingRelease, order, "No successful Razorpay payment found for this order.");
            }

            var settings = await GetSettings();
            ValidateSettings(settings);

            var businessInformation = await _businessInformationRepository.Query()
                .FirstOrDefaultAsync(x => x.VendorId == order.VendorId.Value);

            var financialPayment = await ResolveFinancialPayment(order.VendorId.Value, businessInformation?.UserId);
            var activeBank = financialPayment?.BankDetails?.FirstOrDefault(x => x.IsActive);
            if (financialPayment == null || activeBank == null)
            {
                throw new InvalidOperationException("Vendor active bank account is not configured in FinancialPayment.");
            }

            var release = existingRelease ?? new VendorPaymentRelease
            {
                OrderId = order.Id,
                VendorId = order.VendorId.Value,
                CreatedOn = DateTimeOffset.UtcNow
            };

            release.FinancialPaymentId = financialPayment.Id;
            release.BankDetailId = activeBank.Id;
            release.Amount = order.OrderTotal;
            release.Currency = GetCurrency(settings);
            release.PaymentMethod = payment.PaymentMethod;
            release.Status = VendorPaymentReleaseStatus.Processing;
            release.ReferenceId = release.ReferenceId ?? $"order_{order.Id}";
            release.FailureMessage = null;
            release.LatestUpdatedOn = DateTimeOffset.UtcNow;

            if (existingRelease == null)
            {
                _releaseRepository.Add(release);
            }

            await _releaseRepository.SaveChangesAsync();

            try
            {
                // Use RazorpayX Payouts API: POST /payouts
                // Build payout payload with vendor bank details and create a payout
                // Ensure we have contact and fund_account ids for the vendor
                // If stored contact/fund ids exist, validate they belong to this account/keys; clear them if not
                if (!string.IsNullOrWhiteSpace(release.RazorpayContactId) || !string.IsNullOrWhiteSpace(release.RazorpayFundAccountId))
                {
                    await ValidateExistingRazorpayIds(settings, release);
                }

                if (string.IsNullOrWhiteSpace(release.RazorpayContactId) || string.IsNullOrWhiteSpace(release.RazorpayFundAccountId))
                {
                    await EnsureContactAndFundAccount(settings, release, order, activeBank, businessInformation);
                }

                var payoutPayload = BuildPayoutPayload(settings, release, order, activeBank);
                release.RequestPayload = JsonConvert.SerializeObject(payoutPayload);

                var payoutResponse = await SendRazorpayRequest(settings, HttpMethod.Post, "payouts", payoutPayload, release.ReferenceId);
                var payoutContent = await payoutResponse.Content.ReadAsStringAsync();
                release.ResponsePayload = payoutContent;

                if (!payoutResponse.IsSuccessStatusCode)
                {
                    release.Status = VendorPaymentReleaseStatus.Failed;
                    release.FailureMessage = BuildPayoutFailureMessage(payoutContent);
                    release.LatestUpdatedOn = DateTimeOffset.UtcNow;
                    await _releaseRepository.SaveChangesAsync();
                    return release;
                }

                var payout = JObject.Parse(payoutContent);
                release.RazorpayPayoutId = payout.Value<string>("id");
                release.RazorpayPayoutStatus = payout.Value<string>("status");
                release.Status = VendorPaymentReleaseStatus.Released;
                release.ReleasedOn = DateTimeOffset.UtcNow;
                release.LatestUpdatedOn = DateTimeOffset.UtcNow;
                await _releaseRepository.SaveChangesAsync();

                return release;
            }
            catch (Exception ex)
            {
                release.Status = VendorPaymentReleaseStatus.Failed;
                release.FailureMessage = ex.Message;
                release.LatestUpdatedOn = DateTimeOffset.UtcNow;
                await _releaseRepository.SaveChangesAsync();
                return release;
            }
        }

        private async Task<VendorPaymentRelease> MarkSkipped(VendorPaymentRelease release, Order order, string reason)
        {
            release ??= new VendorPaymentRelease
            {
                OrderId = order.Id,
                VendorId = order.VendorId ?? 0,
                CreatedOn = DateTimeOffset.UtcNow
            };

            release.Amount = order.OrderTotal;
            release.Currency = _configuration["Payments:Razorpay:Currency"] ?? "INR";
            release.PaymentMethod = order.PaymentMethod;
            release.Status = VendorPaymentReleaseStatus.Skipped;
            release.FailureMessage = reason;
            release.LatestUpdatedOn = DateTimeOffset.UtcNow;

            if (release.Id == 0)
            {
                _releaseRepository.Add(release);
            }

            await _releaseRepository.SaveChangesAsync();
            return release;
        }

        private async Task<FinancialPayment> ResolveFinancialPayment(long vendorId, long? supplierProfileId)
        {
            var query = _financialPaymentRepository.Query().Include(x => x.BankDetails);

            if (supplierProfileId.HasValue)
            {
                var byBusinessProfile = await query.FirstOrDefaultAsync(x => x.SupplierProfileId == supplierProfileId.Value);
                if (byBusinessProfile != null)
                {
                    return byBusinessProfile;
                }
            }

            return await query.FirstOrDefaultAsync(x => x.SupplierProfileId == vendorId);
        }

        private object BuildPayoutPayload(RazorConfigForm settings, VendorPaymentRelease release, Order order, BankDetails activeBank)
        {
            // Prefer using fund_account_id when available (recommended by RazorpayX)
            if (!string.IsNullOrWhiteSpace(release.RazorpayFundAccountId))
            {
                return new
                {
                    account_number = settings.PayoutAccountNumber,
                    fund_account_id = release.RazorpayFundAccountId,
                    amount = ToCurrencySubunits(release.Amount),
                    currency = release.Currency,
                    mode = "IMPS",
                    purpose = "payout",
                    narration = $"Payout for order {order.Id}",
                    reference_id = release.ReferenceId,
                    queue_if_low_balance = true,
                    notes = new
                    {
                        orderId = order.Id.ToString(CultureInfo.InvariantCulture),
                        vendorId = order.VendorId?.ToString(CultureInfo.InvariantCulture)
                    }
                };
            }

            // Fallback: provide bank details directly (may be rejected depending on account configuration)
            return new
            {
                account_number = activeBank.AccountNumber,
                amount = ToCurrencySubunits(release.Amount),
                currency = release.Currency,
                mode = "IMPS",
                purpose = "payout",
                beneficiary_name = activeBank.AccountHolderName,
                beneficiary_account = activeBank.AccountNumber,
                beneficiary_ifsc = activeBank.IFSCCode,
                narration = $"Payout for order {order.Id}",
                reference_id = release.ReferenceId,
                queue_if_low_balance = true,
                notes = new
                {
                    orderId = order.Id.ToString(CultureInfo.InvariantCulture),
                    vendorId = order.VendorId?.ToString(CultureInfo.InvariantCulture)
                }
            };
        }

        private async Task EnsureContactAndFundAccount(RazorConfigForm settings, VendorPaymentRelease release, Order order, BankDetails activeBank, BusinessInformation businessInformation)
        {
            // Create contact if missing
            if (string.IsNullOrWhiteSpace(release.RazorpayContactId))
            {
                var contactPayload = new
                {
                    name = activeBank.AccountHolderName,
                    email = businessInformation?.Email,
                    contact = businessInformation?.ContactNumberPrimary,
                    type = "vendor",
                    reference_id = release.ReferenceId,
                    notes = new { orderId = order.Id.ToString(CultureInfo.InvariantCulture) }
                };

                var contactResponse = await SendRazorpayRequest(settings, HttpMethod.Post, "contacts", contactPayload, release.ReferenceId);
                var contactContent = await contactResponse.Content.ReadAsStringAsync();
                // Persist request/response for debugging
                release.RequestPayload = release.RequestPayload ?? string.Empty + "\nCONTACT_REQUEST:\n" + JsonConvert.SerializeObject(contactPayload);
                release.ResponsePayload = release.ResponsePayload ?? string.Empty + "\nCONTACT_RESPONSE:\n" + contactContent;

                if (!contactResponse.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(BuildPayoutFailureMessage(contactContent));
                }

                var contactJson = JObject.Parse(contactContent);
                release.RazorpayContactId = contactJson.Value<string>("id");
                release.LatestUpdatedOn = DateTimeOffset.UtcNow;
                await _releaseRepository.SaveChangesAsync();
            }

            // Create fund account if missing
            if (string.IsNullOrWhiteSpace(release.RazorpayFundAccountId))
            {
                var fundAccountPayload = new
                {
                    contact_id = release.RazorpayContactId,
                    account_type = "bank_account",
                    bank_account = new
                    {
                        name = activeBank.AccountHolderName,
                        ifsc = activeBank.IFSCCode,
                        account_number = activeBank.AccountNumber
                    }
                };

                var fundResponse = await SendRazorpayRequest(settings, HttpMethod.Post, "fund_accounts", fundAccountPayload, release.ReferenceId);
                var fundContent = await fundResponse.Content.ReadAsStringAsync();
                release.RequestPayload = release.RequestPayload ?? string.Empty + "\nFUND_ACCOUNT_REQUEST:\n" + JsonConvert.SerializeObject(fundAccountPayload);
                release.ResponsePayload = release.ResponsePayload ?? string.Empty + "\nFUND_ACCOUNT_RESPONSE:\n" + fundContent;

                if (!fundResponse.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(BuildPayoutFailureMessage(fundContent));
                }

                var fundJson = JObject.Parse(fundContent);
                release.RazorpayFundAccountId = fundJson.Value<string>("id");
                release.LatestUpdatedOn = DateTimeOffset.UtcNow;
                await _releaseRepository.SaveChangesAsync();
            }
        }

        private static string BuildPayoutFailureMessage(string payoutContent)
        {
            try
            {
                var errorJson = JObject.Parse(payoutContent);
                var error = errorJson["error"]?.ToString();
                return $"Razorpay payout failed: {error}";
            }
            catch
            {
                return $"Razorpay payout failed: {payoutContent}";
            }
        }

        private async Task<RazorConfigForm> GetSettings()
        {
            var razorProvider = await _paymentProviderRepository.Query().FirstOrDefaultAsync(x => x.Id == PaymentProviderHelper.RazorpayProviderId);
            var settings = string.IsNullOrWhiteSpace(razorProvider?.AdditionalSettings)
                ? new RazorConfigForm()
                : JsonConvert.DeserializeObject<RazorConfigForm>(razorProvider.AdditionalSettings) ?? new RazorConfigForm();


            // Optionally force use of configuration keys for debugging
            var forceUseConfig = string.Equals(_configuration["Payments:Razorpay:ForceUseConfigKeys"], "true", StringComparison.OrdinalIgnoreCase);
            var configKeyId = _configuration["Payments:Razorpay:KeyId"];
            var configKeySecret = _configuration["Payments:Razorpay:KeySecret"];


            if (forceUseConfig)
            {
                settings.KeyId = FirstNonEmpty(configKeyId, settings.KeyId);
                settings.KeySecret = FirstNonEmpty(configKeySecret, settings.KeySecret);
            }
            else
            {
                settings.KeyId = FirstNonEmpty(settings.KeyId, configKeyId);
                settings.KeySecret = FirstNonEmpty(settings.KeySecret, configKeySecret);

            }

            settings.WebhookSecret = FirstNonEmpty(settings.WebhookSecret, _configuration["Payments:Razorpay:WebhookSecret"]);
            settings.Currency = FirstNonEmpty(settings.Currency, _configuration["Payments:Razorpay:Currency"], "INR");
            settings.PayoutUrl = NormalizeBaseUrl(FirstNonEmpty(settings.PayoutUrl, _configuration["Payments:Razorpay:PayoutUrl"], "https://api.razorpay.com/v1/"));
            // Log which KeyId is in use (mask secret)
            try
            {
                var maskedSecret = string.IsNullOrWhiteSpace(settings.KeySecret) ? "(none)" : "****" + settings.KeySecret.Substring(Math.Max(0, settings.KeySecret.Length - 4));
                _logger.LogDebug("Razorpay settings loaded. KeyId={KeyId}, KeySecretMasked={Masked}", settings.KeyId, maskedSecret);
            }
            catch { }

            return settings;
        }

        private static void ValidateSettings(RazorConfigForm settings)
        {
            if (string.IsNullOrWhiteSpace(settings.KeyId) || string.IsNullOrWhiteSpace(settings.KeySecret))
            {
                throw new InvalidOperationException("Razorpay KeyId and KeySecret are not configured.");
            }

            if (string.IsNullOrWhiteSpace(settings.PayoutUrl))
            {
                throw new InvalidOperationException("Razorpay payout API URL is not configured.");
            }
        }

        private static void ApplyPayoutStatus(VendorPaymentRelease release, JObject payout)
        {
            var payoutStatus = release.RazorpayPayoutStatus?.ToLowerInvariant();
            switch (payoutStatus)
            {
                case "processed":
                    release.Status = VendorPaymentReleaseStatus.Released;
                    release.FailureMessage = null;
                    release.ReleasedOn ??= DateTimeOffset.UtcNow;
                    break;

                case "queued":
                case "pending":
                case "processing":
                    release.Status = VendorPaymentReleaseStatus.Processing;
                    release.FailureMessage = null;
                    break;

                case "rejected":
                case "cancelled":
                case "reversed":
                case "failed":
                    release.Status = VendorPaymentReleaseStatus.Failed;
                    release.FailureMessage = BuildPayoutStatusMessage(payout) ?? $"Razorpay payout status: {release.RazorpayPayoutStatus}";
                    break;
            }
        }

        private static string BuildPayoutStatusMessage(JObject payout)
        {
            var statusDetails = payout["status_details"] as JObject;
            if (statusDetails == null)
            {
                return null;
            }

            var description = statusDetails.Value<string>("description");
            var reason = statusDetails.Value<string>("reason");
            var source = statusDetails.Value<string>("source");

            return FirstNonEmpty(description, reason, source);
        }

        private static string NormalizeBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl) || baseUrl.EndsWith("/", StringComparison.Ordinal))
            {
                return baseUrl;
            }

            return baseUrl + "/";
        }

        private async Task<HttpResponseMessage> SendRazorpayRequest(RazorConfigForm settings, HttpMethod method, string path, object body = null, string idempotencyKey = null)
        {
            var client = _httpClientFactory.CreateClient("razorpay");
            client.BaseAddress = new Uri(settings.PayoutUrl);

            var request = new HttpRequestMessage(method, path);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.KeyId}:{settings.KeySecret}")));

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                request.Headers.Add("X-Idempotency-Key", idempotencyKey);
            }

            string jsonBody = null;
            if (body != null)
            {
                jsonBody = JsonConvert.SerializeObject(body);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            try
            {
                // Log request details for debugging (mask secret)
                try
                {
                    _logger.LogDebug("RazorpayRequest {Method} {RequestUri} KeyId:{KeyId} Idempotency:{Idempotency} Body:{Body}", method, request.RequestUri, settings.KeyId, idempotencyKey, jsonBody);
                }
                catch { /* ignore logging errors */ }

                var response = await client.SendAsync(request);

                // Read response content for logging but replace it so callers can still read
                var respContent = await response.Content.ReadAsStringAsync();
                try
                {
                    _logger.LogDebug("RazorpayResponse {StatusCode} {ReasonPhrase} Path:{Path} Content:{Content}", (int)response.StatusCode, response.ReasonPhrase, path, respContent);
                }
                catch { /* ignore logging errors */ }

                // Replace response content so it can be read again by caller
                response.Content = new StringContent(respContent, Encoding.UTF8, "application/json");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RazorpayRequestError {Method} {Url}", method, settings.PayoutUrl + path);
                throw;
            }
        }

        private static string BuildTransferFailureMessage(string transferContent)
        {
            try
            {
                var errorJson = JObject.Parse(transferContent);
                var errorCode = errorJson["error"]?["code"]?.ToString();
                var description = errorJson["error"]?["description"]?.ToString();
                return $"Razorpay transfer failed ({errorCode}): {description}";
            }
            catch
            {
                return $"Razorpay transfer failed: {transferContent}";
            }
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
