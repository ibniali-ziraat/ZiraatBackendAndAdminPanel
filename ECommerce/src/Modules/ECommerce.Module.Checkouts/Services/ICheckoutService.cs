using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Checkouts.Areas.Checkouts.ViewModels;
using ECommerce.Module.Checkouts.Models;

namespace ECommerce.Module.Checkouts.Services
{
    public interface ICheckoutService
    {
        Task<CheckoutVm> GetCheckoutDetails(Guid checkoutId);
        Task<decimal> GetTax(Guid checkoutId, string countryId, long stateOrProvinceId, string zipCode);
        Task<CheckoutTaxAndShippingPriceVm> UpdateTaxAndShippingPrices(Guid checkoutId, TaxAndShippingPriceRequestVm model);
        Task<Checkout> Create(long customerId, long createdById, IList<CartItemToCheckoutVm> cartItems, string couponCode);
    }
}
