using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    /// <summary>
    /// Handles pricing and stock strategy for products sold by multiple vendors.
    /// Determines best pricing, aggregates stock, and manages inventory across vendors.
    /// </summary>
    public interface IMultiVendorProductStrategy
    {
        /// <summary>
        /// Gets the best price option from all active vendors for a product
        /// </summary>
        Task<VendorPricingOption> GetBestPriceOptionAsync(long productId);

        /// <summary>
        /// Gets all available pricing options from active vendors
        /// </summary>
        Task<List<VendorPricingOption>> GetAllVendorPricingOptionsAsync(long productId);

        /// <summary>
        /// Gets total aggregated stock from all active vendors
        /// </summary>
        Task<int> GetAggregatedStockAsync(long productId);

        /// <summary>
        /// Determines if a product has multiple active vendors
        /// </summary>
        Task<bool> HasMultipleActiveVendorsAsync(long productId);

        /// <summary>
        /// Gets vendor count for a product
        /// </summary>
        Task<int> GetVendorCountAsync(long productId);

        /// <summary>
        /// Allocates quantity across vendors (for inventory management)
        /// </summary>
        Task<List<VendorAllocation>> AllocateQuantityAsync(long productId, int requestedQuantity);
    }

    /// <summary>Represents a vendor's pricing option for a product</summary>
    public class VendorPricingOption
    {
        public long VendorProductId { get; set; }
        public long VendorId { get; set; }
        public string VendorName { get; set; }
        public decimal AssignedPrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset LatestUpdatedOn { get; set; }

        /// <summary>Final price after applying discount</summary>
        public decimal FinalPrice => DiscountPrice > 0 ? DiscountPrice : AssignedPrice;

        /// <summary>Discount percentage if applicable</summary>
        public decimal DiscountPercentage => DiscountPrice > 0
            ? Math.Round(((AssignedPrice - DiscountPrice) / AssignedPrice) * 100, 2)
            : 0;

        /// <summary>Is stock available</summary>
        public bool IsAvailable => StockQuantity > 0 && IsActive;
    }

    /// <summary>Represents how much quantity can be fulfilled by a vendor</summary>
    public class VendorAllocation
    {
        public long VendorId { get; set; }
        public long VendorProductId { get; set; }
        public int AllocatedQuantity { get; set; }
        public decimal Price { get; set; }
        public int AvailableStock { get; set; }
        public bool CanFulfillFull { get; set; }
    }

    /// <summary>
    /// Implementation of multi-vendor product strategy
    /// </summary>
    public class MultiVendorProductStrategy : IMultiVendorProductStrategy
    {
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IRepository<Vendor> _vendorRepository;

        public MultiVendorProductStrategy(
            IRepository<VendorProduct> vendorProductRepository,
            IRepository<Vendor> vendorRepository)
        {
            _vendorProductRepository = vendorProductRepository;
            _vendorRepository = vendorRepository;
        }

        public async Task<VendorPricingOption> GetBestPriceOptionAsync(long productId)
        {
            var allOptions = await GetAllVendorPricingOptionsAsync(productId);

            if (!allOptions.Any())
            {
                return null;
            }

            // Filter available options and sort by final price
            var bestOption = allOptions
                .Where(x => x.IsAvailable)
                .OrderBy(x => x.FinalPrice)
                .FirstOrDefault();

            return bestOption;
        }

        public async Task<List<VendorPricingOption>> GetAllVendorPricingOptionsAsync(long productId)
        {
            var vendorProducts = await _vendorProductRepository.Query()
                .Where(x => x.ProductId == productId && x.IsActive)
                .ToListAsync();

            if (!vendorProducts.Any())
            {
                return new List<VendorPricingOption>();
            }

            var vendorIds = vendorProducts.Select(x => x.VendorId).ToList();
            var vendors = await _vendorRepository.Query()
                .Where(x => vendorIds.Contains(x.Id))
                .ToListAsync();

            var vendorDict = vendors.ToDictionary(x => x.Id, x => x.Name);

            return vendorProducts
                .Select(vp => new VendorPricingOption
                {
                    VendorProductId = vp.Id,
                    VendorId = vp.VendorId,
                    VendorName = vendorDict.TryGetValue(vp.VendorId, out var name) ? name : "Unknown",
                    AssignedPrice = vp.AssignedPrice ?? 0,
                    DiscountPrice = vp.DiscountPrice ?? 0,
                    StockQuantity = vp.StockQuantity,
                    IsActive = vp.IsActive,
                    LatestUpdatedOn = vp.LatestUpdatedOn
                })
                .OrderBy(x => x.FinalPrice)
                .ToList();
        }

        public async Task<int> GetAggregatedStockAsync(long productId)
        {
            return await _vendorProductRepository.Query()
                .Where(x => x.ProductId == productId && x.IsActive)
                .SumAsync(x => x.StockQuantity);
        }

        public async Task<bool> HasMultipleActiveVendorsAsync(long productId)
        {
            var count = await GetVendorCountAsync(productId);
            return count > 1;
        }

        public async Task<int> GetVendorCountAsync(long productId)
        {
            return await _vendorProductRepository.Query()
                .Where(x => x.ProductId == productId && x.IsActive)
                .Select(x => x.VendorId)
                .Distinct()
                .CountAsync();
        }

        public async Task<List<VendorAllocation>> AllocateQuantityAsync(long productId, int requestedQuantity)
        {
            var allOptions = await GetAllVendorPricingOptionsAsync(productId);
            var allocations = new List<VendorAllocation>();
            var remainingQuantity = requestedQuantity;

            // Sort by best price first for cost optimization
            foreach (var option in allOptions.OrderBy(x => x.FinalPrice))
            {
                if (remainingQuantity <= 0)
                {
                    break;
                }

                var allocatedQuantity = Math.Min(remainingQuantity, option.StockQuantity);
                
                allocations.Add(new VendorAllocation
                {
                    VendorId = option.VendorId,
                    VendorProductId = option.VendorProductId,
                    AllocatedQuantity = allocatedQuantity,
                    Price = option.FinalPrice,
                    AvailableStock = option.StockQuantity,
                    CanFulfillFull = option.StockQuantity >= requestedQuantity
                });

                remainingQuantity -= allocatedQuantity;
            }

            return allocations;
        }
    }
}
