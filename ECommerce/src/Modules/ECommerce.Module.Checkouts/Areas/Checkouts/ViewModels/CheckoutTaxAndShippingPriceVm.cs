using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Module.ShippingPrices.Services;
using ECommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels;

namespace ECommerce.Module.Checkouts.Areas.Checkouts.ViewModels
{
    public class CheckoutTaxAndShippingPriceVm
    {
        public bool IsProductPriceIncludedTax { get; set; }

        public IList<ShippingPrice> ShippingPrices { get; set; }

        public string SelectedShippingMethodName { get; set; }

        public CheckoutVm CheckoutVm { get; set; }
    }
}
