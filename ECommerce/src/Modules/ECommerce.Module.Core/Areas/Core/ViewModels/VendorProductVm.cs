using System;
using System.Collections.Generic;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class VendorProductVm
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public string Sku { get; set; }

        public string BatchNumber { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
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
        public DateTimeOffset CreatedOn { get; set; }
        public bool IsVariation { get; set; }
        public long? ParentProductId { get; set; }
        public IList<ProductVariationOptionInfoDto> VariationOptions { get; set; } =
            new List<ProductVariationOptionInfoDto>();
        public IList<ProductOptionInfoDto> AvailableOptions { get; set; } =
            new List<ProductOptionInfoDto>();
        public IList<VendorProductVariationVm> Variations { get; set; } =
            new List<VendorProductVariationVm>();
    }

    public class VendorProductVariationVm
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string NormalizedName { get; set; }
        public string Sku { get; set; }
        public string Gtin { get; set; }
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
        public int SortIndex { get; set; }
        public IList<VendorProductVariationOptionVm> OptionCombinations { get; set; } =
            new List<VendorProductVariationOptionVm>();
    }

    public class VendorProductVariationOptionVm
    {
        public long Id { get; set; }
        public long OptionId { get; set; }
        public string OptionName { get; set; }
        public string Value { get; set; }
        public int StockQuantity { get; set; }
        public int SortIndex { get; set; }
    }
}
