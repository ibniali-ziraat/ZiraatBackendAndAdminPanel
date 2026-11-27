using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Core.ViewModels;

namespace ECommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels
{
    public class AddToCartResultVm
    {
        private readonly ICurrencyService _currencyService;

        public AddToCartResultVm(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public string ProductName { get; set; }

        public string ProductImage { get; set; }

        public CalculatedProductPrice CalculatedProductPrice { get; set; }

        public decimal ProductPrice { get; set; }

        public long? VendorId { get; set; }

        public long? VendorProductId { get; set; }

        public long? VendorProductVariationId { get; set; }

        public VendorProductAssignmentPricingVm VendorAssignment { get; set; }

        public string VariationName { get; set; }

        public int Quantity { get; set; }

        public int CartItemCount { get; set; }

        public decimal CartAmount { get; set; }

        public string ProductPriceString => _currencyService.FormatCurrency(ProductPrice);

        public string CartAmountString => _currencyService.FormatCurrency(CartAmount);
    }
}
