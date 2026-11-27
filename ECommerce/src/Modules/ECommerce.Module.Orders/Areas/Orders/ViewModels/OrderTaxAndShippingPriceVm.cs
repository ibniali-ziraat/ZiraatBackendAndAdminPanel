using System.Collections.Generic;
using ECommerce.Module.ShippingPrices.Services;
using ECommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels;

namespace ECommerce.Module.Orders.Areas.Orders.ViewModels
{
    public class OrderTaxAndShippingPriceVm
    {
        public bool IsProductPriceIncludedTax { get; set; }

        public IList<ShippingPrice> ShippingPrices { get; set; }

        public string SelectedShippingMethodName { get; set; }

        public CartVm Cart { get; set; }
    }
}
