using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class VendorProductForm
    {
        [Required]
        public long ProductId { get; set; }

        [StringLength(50)]
        public string BatchNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ManufacturingDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Range(0, 999999.99)]
        public decimal? MaximumRetailPrice { get; set; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal AssignedPrice { get; set; }

        [Range(0, 999999.99)]
        public decimal? DiscountPrice { get; set; }

        [Range(0, 100)]
        public decimal? TaxRate { get; set; }

        [Range(0, 100)]
        public decimal? CreditSaleHikePercentage { get; set; }

        public bool IsAvailableForCreditBuyers { get; set; } = false;

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        public bool IsActive { get; set; } = true;
        public decimal? NetSalePriceForZiraat { get; set; }
        public decimal? SalePriceForCreditor { get; set; }

        public IList<VendorProductVariationForm> Variations { get; set; }
    }

    public class VendorProductVariationForm
    {
        public long Id { get; set; }

        [StringLength(450)]
        public string Name { get; set; }

        [StringLength(450)]
        public string NormalizedName { get; set; }

        [StringLength(450)]
        public string Sku { get; set; }

        [StringLength(450)]
        public string Gtin { get; set; }

        [Range(0, 999999.99)]
        public decimal? MaximumRetailPrice { get; set; }

        [Range(0, 999999.99)]
        public decimal? AssignedPrice { get; set; }

        [Range(0, 999999.99)]
        public decimal? DiscountPrice { get; set; }

        [Range(0, 100)]
        public decimal? TaxRate { get; set; }

        [Range(0, 100)]
        public decimal? CreditSaleHikePercentage { get; set; }

        public decimal? NetSalePriceForZiraat { get; set; }

        public decimal? SalePriceForCreditor { get; set; }

        public bool? IsAvailableForCreditBuyers { get; set; }

        [Range(0, int.MaxValue)]
        public int? StockQuantity { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortIndex { get; set; }

        public IList<VendorProductVariationOptionForm> OptionCombinations { get; set; } =
            new List<VendorProductVariationOptionForm>();
    }

    public class VendorProductVariationOptionForm
    {
        [Required]
        public long OptionId { get; set; }

        [StringLength(450)]
        public string OptionName { get; set; }

        [Required]
        [StringLength(450)]
        public string Value { get; set; }

        [Range(0, int.MaxValue)]
        public int? StockQuantity { get; set; }

        public int SortIndex { get; set; }
    }
}
