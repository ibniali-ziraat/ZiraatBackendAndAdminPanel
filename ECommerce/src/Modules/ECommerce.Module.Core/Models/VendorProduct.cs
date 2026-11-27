using System;
using System.Collections.Generic;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class VendorProduct : EntityBase
    {
        public long VendorId { get; set; }

        public long ProductId { get; set; }

        /// <summary>
        /// Cached product name to avoid circular reference with Catalog module
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Cached product SKU to avoid circular reference with Catalog module
        /// </summary>
        public string ProductSku { get; set; }

        /// <summary>
        /// Batch number for inventory tracking
        /// </summary>
        public string BatchNumber { get; set; }

        /// <summary>
        /// Manufacturing date of the product
        /// </summary>
        public DateTime? ManufacturingDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Maximum Retail Price (as per label)
        /// </summary>
        public decimal? MaximumRetailPrice { get; set; }

        public decimal? AssignedPrice { get; set; }

        public decimal? OriginalPrice { get; set; }

        public decimal? DiscountPrice { get; set; }

        /// <summary>
        /// Tax rate percentage (e.g., 5, 12, 18 for 5%, 12%, 18% GST)
        /// </summary>
        public decimal? TaxRate { get; set; }

        /// <summary>
        /// Net sale price after tax deduction (auto-calculated)
        /// Formula: AssignedPrice - (AssignedPrice × TaxRate / 100)
        /// </summary>
        public decimal? NetSalePriceForZiraat { get; set; }

        /// <summary>
        /// Credit sale markup percentage for credit buyers
        /// </summary>
        public decimal? CreditSaleHikePercentage { get; set; }

        /// <summary>
        /// Sale price for credit buyers (auto-calculated)
        /// Formula: NetSalePriceForZiraat × (1 + CreditSaleHikePercentage / 100)
        /// </summary>
        public decimal? SalePriceForCreditor { get; set; }

        /// <summary>
        /// Whether this product is available for credit buyers
        /// </summary>
        public bool IsAvailableForCreditBuyers { get; set; } = false;

        public int StockQuantity { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }

        public long? CreatedById { get; set; }

        public long? LatestUpdatedById { get; set; }

        public virtual Vendor Vendor { get; set; }

        public virtual User CreatedBy { get; set; }

        public virtual User LatestUpdatedBy { get; set; }

        public IList<VendorProductVariation> Variations { get; protected set; } =
            new List<VendorProductVariation>();
    }
}
