using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    /// <summary>
    /// Service for managing vendor product assignments in a multi-vendor marketplace
    /// Handles stock, pricing, and discount management per vendor
    /// </summary>
    public interface IVendorProductService
    {
        /// <summary>
        /// Assigns a product to a vendor with custom pricing and stock
        /// </summary>
        Task<VendorProduct> AssignProductToVendor(
            long vendorId,
            long productId,
            decimal? assignedPrice,
            decimal? discountPrice,
            int stockQuantity,
            bool isActive,
            string batchNumber = null,
            DateTime? manufacturingDate = null,
            decimal? maximumRetailPrice = null,
            decimal? taxRate = null,
            decimal? creditSaleHikePercentage = null,
            bool isAvailableForCreditBuyers = false,
            decimal? NetSalePriceForZiraat = null,
            decimal? SalePriceForCreditor = null,
            IList<VendorProductVariationForm> variations = null,
            bool syncToMainProduct = false);

        /// <summary>
        /// Updates vendor's product assignment with new pricing and stock
        /// </summary>
        Task<VendorProduct> UpdateVendorProduct(
            long vendorProductId,
            decimal? assignedPrice,
            decimal? discountPrice,
            int stockQuantity,
            bool isActive,
            string batchNumber = null,
            DateTime? manufacturingDate = null,
            decimal? maximumRetailPrice = null,
            decimal? taxRate = null,
            decimal? creditSaleHikePercentage = null,
            bool isAvailableForCreditBuyers = false,
            decimal? NetSalePriceForZiraat = null,
            decimal? SalePriceForCreditor = null, 
            IList<VendorProductVariationForm> variations = null,
            bool syncToMainProduct = false);

        /// <summary>
        /// Deletes vendor product assignment
        /// </summary>
        Task DeleteVendorProduct(long vendorProductId);

        /// <summary>
        /// Gets active vendor assignments for a product
        /// Useful for determining if product has multiple vendors
        /// </summary>
        Task<List<VendorProduct>> GetActiveVendorAssignmentsForProduct(long productId);

        /// <summary>
        /// Calculates aggregated pricing and stock data for a product across all vendors
        /// </summary>
        Task<VendorProductAggregateData> GetAggregateProductData(long productId);
    }

    /// <summary>
    /// Aggregated data across all active vendor assignments for a product
    /// </summary>
    public class VendorProductAggregateData
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }

        /// <summary>Total stock available across all active vendors</summary>
        public int TotalAggregatedStock { get; set; }

        /// <summary>Lowest price offered by any vendor</summary>
        public decimal LowestPrice { get; set; }

        /// <summary>Highest discount available</summary>
        public decimal HighestDiscount { get; set; }

        /// <summary>Number of vendors offering this product</summary>
        public int VendorCount { get; set; }

        /// <summary>Vendor assignments with lowest price and highest discount</summary>
        public List<VendorProductBrief> VendorOptions { get; set; } = new();
    }

    /// <summary>Brief vendor product info for display</summary>
    public class VendorProductBrief
    {
        public long VendorProductId { get; set; }
        public long VendorId { get; set; }
        public string VendorName { get; set; }
        public decimal? AssignedPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
    }
}
