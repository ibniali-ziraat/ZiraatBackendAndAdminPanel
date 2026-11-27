using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Checkouts.Models;
using ECommerce.Module.Checkouts.Areas.Checkouts.ViewModels;
using ECommerce.Module.Checkouts.Services;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Orders.Services;
using ECommerce.Module.PaymentBuyOnCredit.Models;
using ECommerce.Module.Payments.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.PaymentBuyOnCredit.Areas.PaymentBuyOnCredit.Controllers
{
    [Authorize]
    [Area("PaymentBuyOnCredit")]
    [Route("api/BuyOnCredit")]
    public class BuyOnCreditController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly ICheckoutService _checkoutService;
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        private readonly IRepositoryWithTypedId<Checkout, Guid> _checkoutRepository;
        private readonly IRepository<VendorCustomerAssociation> _vendorCustomerAssociationRepository;
        private Lazy<BuyOnCreditSetting> _setting;

        public BuyOnCreditController(
            ICheckoutService checkoutService,
            IOrderService orderService,
            IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository,
            IRepositoryWithTypedId<Checkout, Guid> checkoutRepository,
            IRepository<VendorCustomerAssociation> vendorCustomerAssociationRepository,
            IWorkContext workContext)
        {
            _paymentProviderRepository = paymentProviderRepository;
            _checkoutRepository = checkoutRepository;
            _checkoutService = checkoutService;
            _orderService = orderService;
            _vendorCustomerAssociationRepository = vendorCustomerAssociationRepository;
            _workContext = workContext;
            _setting = new Lazy<BuyOnCreditSetting>(GetSetting());
        }

        /// <summary>
        /// Place order using BuyOnCredit payment method
        /// </summary>
        [HttpPost("BuyOnCreditCheckout")]
        public async Task<IActionResult> BuyOnCreditCheckout(Guid checkoutId, long? vendorId = null)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                var checkout = await _checkoutRepository.Query().FirstOrDefaultAsync(x => x.Id == checkoutId);
                if (checkout == null)
                {
                    return NotFound(new { error = "Checkout not found" });
                }

                if (checkout.CustomerId != currentUser.Id && checkout.CreatedById != currentUser.Id)
                {
                    return Forbid();
                }

                var checkoutVm = await _checkoutService.GetCheckoutDetails(checkoutId);

                if (checkoutVm == null)
                {
                    return NotFound(new { error = "Checkout not found" });
                }

                var validationResult = await ValidateBuyOnCredit(checkoutVm, vendorId, currentUser.Id);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { error = validationResult.ErrorMessage });
                }

                var calculatedFee = CalculateFee(checkoutVm);
                var orderCreateResult = await _orderService.CreateOrder(checkoutVm.Id, "BuyOnCredit", calculatedFee);

                if (!orderCreateResult.Success)
                {
                    return BadRequest(new { error = orderCreateResult.Error });
                }

                return Ok(orderCreateResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Validate if customer is eligible for BuyOnCredit payment
        /// </summary>
        [HttpPost("validate")]
        public async Task<IActionResult> ValidatePaymentMethod(Guid checkoutId, long? vendorId = null)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                var checkout = await _checkoutRepository.Query().FirstOrDefaultAsync(x => x.Id == checkoutId);
                if (checkout == null)
                {
                    return NotFound(new { error = "Checkout not found", isValid = false });
                }

                if (checkout.CustomerId != currentUser.Id && checkout.CreatedById != currentUser.Id)
                {
                    return Forbid();
                }

                var checkoutVm = await _checkoutService.GetCheckoutDetails(checkoutId);

                if (checkoutVm == null)
                {
                    return NotFound(new { error = "Checkout not found", isValid = false });
                }

                var validationResult = await ValidateBuyOnCredit(checkoutVm, vendorId, currentUser.Id);

                if (validationResult.IsValid)
                {
                    return Ok(new
                    {
                        isValid = true,
                        message = "Customer is eligible for BuyOnCredit payment",
                        paymentFee = CalculateFee(checkoutVm)
                    });
                }
                else
                {
                    return Ok(new
                    {
                        isValid = false,
                        message = validationResult.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, isValid = false });
            }
        }

        private BuyOnCreditSetting GetSetting()
        {
            var bocProvider = _paymentProviderRepository.Query()
                .FirstOrDefault(x => x.Id == PaymentProviderHelper.BuyOnCreditProviderId);

            if (bocProvider == null || string.IsNullOrEmpty(bocProvider.AdditionalSettings))
            {
                return new BuyOnCreditSetting { PaymentFee = 0 };
            }

            var bocSetting = JsonConvert.DeserializeObject<BuyOnCreditSetting>(bocProvider.AdditionalSettings);
            return bocSetting;
        }

        private async Task<ValidationResult> ValidateBuyOnCredit(CheckoutVm checkoutVm, long? vendorId, long customerId)
        {
            // Validate min order value
            var creditOrderTotal = GetCreditOrderTotal(checkoutVm);

            if (_setting.Value.MinOrderValue.HasValue && _setting.Value.MinOrderValue.Value > creditOrderTotal)
            {
                return ValidationResult.Fail($"Order total is below minimum required (₹{_setting.Value.MinOrderValue}) for BuyOnCredit");
            }

            // Validate max order value
            if (_setting.Value.MaxOrderValue.HasValue && _setting.Value.MaxOrderValue.Value < creditOrderTotal)
            {
                return ValidationResult.Fail($"Order total exceeds maximum allowed (₹{_setting.Value.MaxOrderValue}) for BuyOnCredit");
            }

            var checkoutItems = checkoutVm.Items.AsEnumerable();
            if (vendorId.HasValue)
            {
                checkoutItems = checkoutItems.Where(x => x.VendorId == vendorId.Value);
            }

            var vendorGroups = checkoutItems
                .Where(x => x.VendorId.HasValue)
                .GroupBy(x => x.VendorId.Value)
                .ToList();

            if (!vendorGroups.Any() || vendorGroups.Sum(x => x.Count()) != checkoutVm.Items.Count)
            {
                return ValidationResult.Fail("BuyOnCredit requires every checkout item to be linked to an associated vendor");
            }

            foreach (var vendorGroup in vendorGroups)
            {
                var association = await _vendorCustomerAssociationRepository.Query()
                    .FirstOrDefaultAsync(x => x.VendorId == vendorGroup.Key && x.CustomerId == customerId && !x.IsDeleted);

                if (association == null)
                {
                    return ValidationResult.Fail("You are not authorized to buy on credit from this vendor");
                }

                if (!association.AllowCredit)
                {
                    return ValidationResult.Fail("Credit is not allowed for your account with this vendor");
                }

                if (!association.IsActive)
                {
                    return ValidationResult.Fail("Your credit account with this vendor is currently inactive");
                }

                var vendorOrderTotal = vendorGroup.Sum(x => x.Quantity * GetCreditLinePrice(x));
                var availableCredit = association.MaxCreditAmount - association.CurrentBalance;
                if (availableCredit < vendorOrderTotal)
                {
                    return ValidationResult.Fail(
                        $"Insufficient credit available. Available: ₹{availableCredit}, Required: ₹{vendorOrderTotal}");
                }
            }

            return ValidationResult.Success();
        }

        private decimal CalculateFee(CheckoutVm checkoutVm)
        {
            if (_setting.Value.PaymentFee <= 0)
            {
                return 0;
            }

            return (GetCreditOrderTotal(checkoutVm) / 100) * _setting.Value.PaymentFee;
        }

        private decimal GetCreditOrderTotal(CheckoutVm checkoutVm)
        {
            return checkoutVm.Items.Sum(x => x.Quantity * GetCreditLinePrice(x))
                + (checkoutVm.ShippingAmount ?? 0);
        }

        private decimal GetCreditLinePrice(CheckoutItemVm checkoutItem)
        {
            return checkoutItem.VendorAssignment?.SalePriceForCreditor > 0
                ? checkoutItem.VendorAssignment.SalePriceForCreditor.Value
                : checkoutItem.ProductPrice;
        }

        private class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }

            public static ValidationResult Success() => new ValidationResult { IsValid = true };
            public static ValidationResult Fail(string message) => new ValidationResult { IsValid = false, ErrorMessage = message };
        }
    }
}
