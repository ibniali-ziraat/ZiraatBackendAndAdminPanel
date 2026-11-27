using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public static class VendorProductPricing
    {
        public static decimal GetEffectivePrice(VendorProduct vendorProduct, decimal fallbackPrice = 0)
        {
            if (vendorProduct == null)
            {
                return fallbackPrice;
            }

            if (vendorProduct.DiscountPrice.HasValue && vendorProduct.DiscountPrice.Value > 0)
            {
                return vendorProduct.DiscountPrice.Value;
            }

            return vendorProduct.AssignedPrice ?? fallbackPrice;
        }

        public static decimal GetEffectivePrice(VendorProductVariation variation, decimal fallbackPrice = 0)
        {
            if (variation == null)
            {
                return fallbackPrice;
            }

            if (variation.DiscountPrice.HasValue && variation.DiscountPrice.Value > 0)
            {
                return variation.DiscountPrice.Value;
            }

            return variation.AssignedPrice ?? fallbackPrice;
        }

        public static decimal GetNetSalePriceForZiraat(VendorProduct vendorProduct, decimal fallbackPrice = 0)
        {
            if (vendorProduct?.NetSalePriceForZiraat > 0)
            {
                return vendorProduct.NetSalePriceForZiraat.Value;
            }

            var effectivePrice = GetEffectivePrice(vendorProduct, fallbackPrice);
            if (vendorProduct?.TaxRate > 0)
            {
                return effectivePrice + (effectivePrice * vendorProduct.TaxRate.Value / 100);
            }

            return effectivePrice;
        }

        public static decimal GetNetSalePriceForZiraat(VendorProductVariation variation, decimal fallbackPrice = 0)
        {
            if (variation?.NetSalePriceForZiraat > 0)
            {
                return variation.NetSalePriceForZiraat.Value;
            }

            var effectivePrice = GetEffectivePrice(variation, fallbackPrice);
            if (variation?.TaxRate > 0)
            {
                return effectivePrice + (effectivePrice * variation.TaxRate.Value / 100);
            }

            return effectivePrice;
        }

        public static decimal GetSalePriceForCreditor(VendorProduct vendorProduct, decimal fallbackPrice = 0)
        {
            if (vendorProduct?.IsAvailableForCreditBuyers == true && vendorProduct.SalePriceForCreditor > 0)
            {
                return vendorProduct.SalePriceForCreditor.Value;
            }

            var netSalePrice = GetNetSalePriceForZiraat(vendorProduct, fallbackPrice);
            if (vendorProduct?.IsAvailableForCreditBuyers == true && vendorProduct.CreditSaleHikePercentage > 0)
            {
                return netSalePrice + (netSalePrice * vendorProduct.CreditSaleHikePercentage.Value / 100);
            }

            return netSalePrice;
        }

        public static decimal GetSalePriceForCreditor(VendorProductVariation variation, decimal fallbackPrice = 0)
        {
            if (variation?.IsAvailableForCreditBuyers == true && variation.SalePriceForCreditor > 0)
            {
                return variation.SalePriceForCreditor.Value;
            }

            var netSalePrice = GetNetSalePriceForZiraat(variation, fallbackPrice);
            if (variation?.IsAvailableForCreditBuyers == true && variation.CreditSaleHikePercentage > 0)
            {
                return netSalePrice + (netSalePrice * variation.CreditSaleHikePercentage.Value / 100);
            }

            return netSalePrice;
        }

        public static decimal GetTaxAmount(VendorProduct vendorProduct, decimal fallbackPrice = 0)
        {
            if (vendorProduct == null)
            {
                return 0;
            }

            return GetNetSalePriceForZiraat(vendorProduct, fallbackPrice) - GetEffectivePrice(vendorProduct, fallbackPrice);
        }

        public static decimal GetTaxAmount(VendorProductVariation variation, decimal fallbackPrice = 0)
        {
            if (variation == null)
            {
                return 0;
            }

            return GetNetSalePriceForZiraat(variation, fallbackPrice) - GetEffectivePrice(variation, fallbackPrice);
        }

        public static decimal? GetOldPrice(VendorProduct vendorProduct)
        {
            if (vendorProduct == null || !vendorProduct.DiscountPrice.HasValue || vendorProduct.DiscountPrice.Value <= 0)
            {
                return null;
            }

            return  vendorProduct.OriginalPrice;
        }

        public static decimal? GetOldPrice(VendorProductVariation variation)
        {
            if (variation == null || !variation.DiscountPrice.HasValue || variation.DiscountPrice.Value <= 0)
            {
                return null;
            }

            return variation.OriginalPrice;
        }

        public static decimal? GetVendorOldPrice(VendorProduct vendorProduct)
        {
            if (vendorProduct == null || !vendorProduct.DiscountPrice.HasValue || vendorProduct.DiscountPrice.Value <= 0)
            {
                return null;
            }

            return vendorProduct.MaximumRetailPrice;
        }

        public static decimal? GetVendorOldPrice(VendorProductVariation variation)
        {
            if (variation == null || !variation.DiscountPrice.HasValue || variation.DiscountPrice.Value <= 0)
            {
                return null;
            }

            return variation.MaximumRetailPrice;
        }
    }
}
