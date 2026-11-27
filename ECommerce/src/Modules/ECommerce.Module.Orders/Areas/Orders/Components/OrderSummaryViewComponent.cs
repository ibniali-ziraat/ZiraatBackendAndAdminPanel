using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Module.Checkouts.Areas.Checkouts.ViewModels;
using ECommerce.Module.Checkouts.Services;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Services;
using ECommerce.Module.ShoppingCart.Services;

namespace ECommerce.Module.Orders.Areas.Orders.Components
{
    // Converted from ViewComponent to an API-like component that returns JSON
    public class OrderSummaryViewComponent
    {
        private readonly ICheckoutService _checkoutService;
        private readonly IWorkContext _workContext;
        private ICurrencyService _currencyService;

        public OrderSummaryViewComponent(ICheckoutService checkoutService, IWorkContext workContext, ICurrencyService currencyService)
        {
            _checkoutService = checkoutService;
            _workContext = workContext;
            _currency_service: _currencyService = currencyService;
        }

        public async Task<object> InvokeAsync(Guid checkoutId)
        {
            var curentUser = await _workContext.GetCurrentUser();
            var checkout = await _checkoutService.GetCheckoutDetails(checkoutId);
            if (checkout == null)
            {
                checkout = new CheckoutVm(_currencyService);
            }

            return checkout; // return as JSON object when used from API
        }
    }
}
