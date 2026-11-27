using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Checkouts.Models;
using ECommerce.Module.Checkouts.Areas.Checkouts.ViewModels;
using ECommerce.Module.Checkouts.Services;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Orders.Services;
using ECommerce.Module.PaymentCoD.Models;
using ECommerce.Module.Payments.Models;
using ECommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels;
using ECommerce.Module.ShoppingCart.Services;

namespace ECommerce.Module.PaymentCoD.Areas.PaymentCoD.Controllers
{
    [Authorize]
    [Area("PaymentCoD")]
    [Route("api/CoD")]

    public class CoDController :ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly ICheckoutService _checkoutService;
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        private readonly IRepositoryWithTypedId<Checkout, Guid> _checkoutRepository;
        private Lazy<CoDSetting> _setting;

        public CoDController(
            ICheckoutService checkoutService,
            IOrderService orderService,
            IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository,
            IRepositoryWithTypedId<Checkout, Guid> checkoutRepository,
            IWorkContext workContext)
        {
            _paymentProviderRepository = paymentProviderRepository;
            _checkoutRepository = checkoutRepository;
            _checkoutService = checkoutService;
            _orderService = orderService;
            _workContext = workContext;
            _setting = new Lazy<CoDSetting>(GetSetting());
        }

        [HttpPost("CoDCheckout")]
        public async Task<IActionResult> CoDCheckout(Guid checkoutId)
        {
            var currentUser = await _workContext.GetCurrentUser();
            var checkout = await _checkoutRepository.Query().FirstOrDefaultAsync(x => x.Id == checkoutId);
            if (checkout == null)
            {
                return NotFound();
            }

            if (checkout.CustomerId != currentUser.Id && checkout.CreatedById != currentUser.Id)
            {
                return Forbid();
            }

            var checkoutVm = await _checkoutService.GetCheckoutDetails(checkoutId);
            if(checkoutVm == null)
            {
                return NotFound();
            }

           
            if (!ValidateCoD(checkoutVm))
            {
               return BadRequest("Payment Method is not eligible for this order.");
            }

            var calculatedFee = CalculateFee(checkoutVm);           
            var orderCreateResult = await _orderService.CreateOrder(checkoutVm.Id, "CashOnDelivery", calculatedFee);

            if (!orderCreateResult.Success)
            {
             return BadRequest(orderCreateResult);
            }
            return Ok(orderCreateResult);
        }

        private CoDSetting GetSetting()
        {
            var coDProvider = _paymentProviderRepository.Query().FirstOrDefault(x => x.Id == PaymentProviderHelper.CODProviderId);
            if (string.IsNullOrEmpty(coDProvider.AdditionalSettings))
            {
                return new CoDSetting();
            }

            var coDSetting = JsonConvert.DeserializeObject<CoDSetting>(coDProvider.AdditionalSettings);
            return coDSetting;
        }

        private bool ValidateCoD(CheckoutVm checkoutVm)
        {
            if (_setting.Value.MinOrderValue.HasValue && _setting.Value.MinOrderValue.Value > checkoutVm.OrderTotal)
            {
                return false;
            }

            if (_setting.Value.MaxOrderValue.HasValue && _setting.Value.MaxOrderValue.Value < checkoutVm.OrderTotal)
            {
                return false;
            }

            return true;
        }

        private decimal CalculateFee(CheckoutVm chekoutVm)
        {
            var percent = _setting.Value.PaymentFee;
            return (chekoutVm.OrderTotal / 100) * percent;
        }
    }
}
