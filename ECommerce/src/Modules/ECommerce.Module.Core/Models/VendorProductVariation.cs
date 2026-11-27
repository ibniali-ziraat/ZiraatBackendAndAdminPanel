using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class VendorProductVariation : EntityBase
    {
        public long VendorProductId { get; set; }

        public VendorProduct VendorProduct { get; set; }

        [StringLength(450)]
        public string Name { get; set; }

        [StringLength(450)]
        public string NormalizedName { get; set; }

        [StringLength(450)]
        public string Sku { get; set; }

        [StringLength(450)]
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

        public bool IsActive { get; set; } = true;

        public int SortIndex { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }

        public IList<VendorProductVariationOption> OptionCombinations { get; protected set; } = new List<VendorProductVariationOption>();
    }
}
