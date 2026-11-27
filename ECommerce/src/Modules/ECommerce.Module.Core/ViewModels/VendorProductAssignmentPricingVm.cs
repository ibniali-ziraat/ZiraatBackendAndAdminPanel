using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Core.ViewModels
{
    public class VendorProductAssignmentPricingVm
    {
        public long VendorId { get; set; }

        public long VendorProductId { get; set; }

        public long? VendorProductVariationId { get; set; }

        public decimal? MaximumRetailPrice { get; set; }

        public decimal? AssignedPrice { get; set; }

        public decimal? OriginalPrice { get; set; }

        public decimal? DiscountPrice { get; set; }

        public decimal? TaxRate { get; set; }

        public decimal? NetSalePriceForZiraat { get; set; }

        public decimal? CreditSaleHikePercentage { get; set; }

        public decimal? SalePriceForCreditor { get; set; }

        public bool IsAvailableForCreditBuyers { get; set; }

        public int StockQuantity { get; set; }

        public bool IsActive { get; set; }

        public decimal EffectivePrice { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal NetSalePrice { get; set; }

        public decimal CreditSaleHikeAmount { get; set; }

        public decimal CreditSalePrice { get; set; }

        public static VendorProductAssignmentPricingVm From(VendorProduct vendorProduct, decimal fallbackPrice = 0)
        {
            if (vendorProduct == null)
            {
                return null;
            }

            var effectivePrice = VendorProductPricing.GetEffectivePrice(vendorProduct, fallbackPrice);
            var netSalePrice = VendorProductPricing.GetNetSalePriceForZiraat(vendorProduct, fallbackPrice);
            var creditSalePrice = VendorProductPricing.GetSalePriceForCreditor(vendorProduct, fallbackPrice);

            return new VendorProductAssignmentPricingVm
            {
                VendorId = vendorProduct.VendorId,
                VendorProductId = vendorProduct.Id,
                MaximumRetailPrice = vendorProduct.MaximumRetailPrice,
                AssignedPrice = vendorProduct.AssignedPrice,
                OriginalPrice = vendorProduct.OriginalPrice,
                DiscountPrice = vendorProduct.DiscountPrice,
                TaxRate = vendorProduct.TaxRate,
                NetSalePriceForZiraat = vendorProduct.NetSalePriceForZiraat,
                CreditSaleHikePercentage = vendorProduct.CreditSaleHikePercentage,
                SalePriceForCreditor = vendorProduct.SalePriceForCreditor,
                IsAvailableForCreditBuyers = vendorProduct.IsAvailableForCreditBuyers,
                StockQuantity = vendorProduct.StockQuantity,
                IsActive = vendorProduct.IsActive,
                EffectivePrice = effectivePrice,
                TaxAmount = netSalePrice - effectivePrice,
                NetSalePrice = netSalePrice,
                CreditSaleHikeAmount = creditSalePrice - netSalePrice,
                CreditSalePrice = creditSalePrice
            };
        }

        public static VendorProductAssignmentPricingVm From(VendorProduct vendorProduct, VendorProductVariation variation, decimal fallbackPrice = 0)
        {
            if (vendorProduct == null)
            {
                return null;
            }

            if (variation == null)
            {
                return From(vendorProduct, fallbackPrice);
            }

            var effectivePrice = VendorProductPricing.GetEffectivePrice(variation, fallbackPrice);
            var netSalePrice = VendorProductPricing.GetNetSalePriceForZiraat(variation, fallbackPrice);
            var creditSalePrice = VendorProductPricing.GetSalePriceForCreditor(variation, fallbackPrice);

            return new VendorProductAssignmentPricingVm
            {
                VendorId = vendorProduct.VendorId,
                VendorProductId = vendorProduct.Id,
                VendorProductVariationId = variation.Id,
                MaximumRetailPrice = variation.MaximumRetailPrice,
                AssignedPrice = variation.AssignedPrice,
                OriginalPrice = variation.OriginalPrice,
                DiscountPrice = variation.DiscountPrice,
                TaxRate = variation.TaxRate,
                NetSalePriceForZiraat = variation.NetSalePriceForZiraat,
                CreditSaleHikePercentage = variation.CreditSaleHikePercentage,
                SalePriceForCreditor = variation.SalePriceForCreditor,
                IsAvailableForCreditBuyers = variation.IsAvailableForCreditBuyers,
                StockQuantity = variation.StockQuantity,
                IsActive = variation.IsActive,
                EffectivePrice = effectivePrice,
                TaxAmount = netSalePrice - effectivePrice,
                NetSalePrice = netSalePrice,
                CreditSaleHikeAmount = creditSalePrice - netSalePrice,
                CreditSalePrice = creditSalePrice
            };
        }
    }
}
