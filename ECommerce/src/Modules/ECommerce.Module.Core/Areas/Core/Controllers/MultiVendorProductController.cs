using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Core.Extensions;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    /// <summary>
    /// Handles multi-vendor scenarios for the same product
    /// Provides pricing comparison, stock aggregation, and vendor allocation
    /// </summary>
    [Area("Vendors")]
    [ApiController]
    [Route("api/vendors/products/multi-vendor")]
    [Authorize(Roles = "admin, vendor")]
    public class MultiVendorProductController : ControllerBase
    {
        private readonly IMultiVendorProductStrategy _strategy;
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IWorkContext _workContext;

        public MultiVendorProductController(
            IMultiVendorProductStrategy strategy,
            IRepository<VendorProduct> vendorProductRepository,
            IRepository<Vendor> vendorRepository,
            IWorkContext workContext)
        {
            _strategy = strategy;
            _vendorProductRepository = vendorProductRepository;
            _vendorRepository = vendorRepository;
            _workContext = workContext;
        }

        /// <summary>
        /// Gets all vendor pricing options for a product
        /// Shows price comparison across all vendors
        /// </summary>
        [HttpGet("product/{productId:long}/pricing-options")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPricingOptions(long productId)
        {
            try
            {
                var options = await _strategy.GetAllVendorPricingOptionsAsync(productId);

                if (!options.Any())
                {
                    return NotFound(new
                    {
                        message = "No active vendor options found for this product"
                    });
                }

                var bestOption = options.FirstOrDefault();
                var aggregatedStock = options.Sum(x => x.StockQuantity);

                return Ok(new
                {
                    productId = productId,
                    vendorCount = options.Select(x => x.VendorId).Distinct().Count(),
                    bestPrice = bestOption?.FinalPrice,
                    highestDiscount = options.Max(x => x.DiscountPercentage),
                    totalAggregatedStock = aggregatedStock,
                    options = options.Select(x => new
                    {
                        vendorProductId = x.VendorProductId,
                        vendorId = x.VendorId,
                        vendorName = x.VendorName,
                        assignedPrice = x.AssignedPrice,
                        discountPrice = x.DiscountPrice,
                        finalPrice = x.FinalPrice,
                        discountPercentage = x.DiscountPercentage,
                        stock = x.StockQuantity,
                        isAvailable = x.IsAvailable,
                        isBestPrice = x.FinalPrice == bestOption?.FinalPrice,
                        isBestDiscount = x.DiscountPercentage == options.Max(o => o.DiscountPercentage)
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the best price option for a product
        /// </summary>
        [HttpGet("product/{productId:long}/best-price")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBestPrice(long productId)
        {
            try
            {
                var bestOption = await _strategy.GetBestPriceOptionAsync(productId);

                if (bestOption == null)
                {
                    return NotFound(new
                    {
                        message = "No available vendor options for this product"
                    });
                }

                return Ok(new
                {
                    vendorProductId = bestOption.VendorProductId,
                    vendorId = bestOption.VendorId,
                    vendorName = bestOption.VendorName,
                    assignedPrice = bestOption.AssignedPrice,
                    discountPrice = bestOption.DiscountPrice,
                    finalPrice = bestOption.FinalPrice,
                    discountPercentage = bestOption.DiscountPercentage,
                    stock = bestOption.StockQuantity,
                    isAvailable = bestOption.IsAvailable
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets aggregated stock from all vendors for a product
        /// </summary>
        [HttpGet("product/{productId:long}/aggregated-stock")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAggregatedStock(long productId)
        {
            try
            {
                var aggregatedStock = await _strategy.GetAggregatedStockAsync(productId);
                var vendorCount = await _strategy.GetVendorCountAsync(productId);

                return Ok(new
                {
                    productId = productId,
                    totalAggregatedStock = aggregatedStock,
                    vendorCount = vendorCount,
                    hasMultipleVendors = vendorCount > 1
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Checks bulk availability and suggests allocation across vendors
        /// Useful for order fulfillment
        /// </summary>
        [HttpPost("product/{productId:long}/check-bulk-availability")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckBulkAvailability(long productId, [FromBody] BulkAvailabilityRequest request)
        {
            try
            {
                if (request?.RequestedQuantity <= 0)
                {
                    return BadRequest(new { error = "Requested quantity must be greater than 0" });
                }

                var allocations = await _strategy.AllocateQuantityAsync(productId, request.RequestedQuantity);
                var aggregatedStock = await _strategy.GetAggregatedStockAsync(productId);

                var totalAllocated = allocations.Sum(x => x.AllocatedQuantity);
                var canFulfill = totalAllocated >= request.RequestedQuantity;

                return Ok(new
                {
                    productId = productId,
                    requestedQuantity = request.RequestedQuantity,
                    totalAvailableStock = aggregatedStock,
                    canFulfill = canFulfill,
                    totalAllocated = totalAllocated,
                    requiresMultipleVendors = allocations.Count > 1,
                    allocations = allocations.Select(x => new
                    {
                        vendorId = x.VendorId,
                        vendorProductId = x.VendorProductId,
                        allocatedQuantity = x.AllocatedQuantity,
                        price = x.Price,
                        availableStock = x.AvailableStock,
                        canFulfillFull = x.CanFulfillFull
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets detailed comparison of all vendors for admin dashboard
        /// </summary>
        [HttpGet("product/{productId:long}/vendor-comparison")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetVendorComparison(long productId)
        {
            try
            {
                var options = await _strategy.GetAllVendorPricingOptionsAsync(productId);

                if (!options.Any())
                {
                    return NotFound();
                }

                var aggregatedStock = options.Sum(x => x.StockQuantity);
                var bestPrice = options.Min(x => x.FinalPrice);
                var highestPrice = options.Max(x => x.FinalPrice);

                return Ok(new
                {
                    productId = productId,
                    statistics = new
                    {
                        vendorCount = options.Select(x => x.VendorId).Distinct().Count(),
                        totalAggregatedStock = aggregatedStock,
                        bestPrice = bestPrice,
                        highestPrice = highestPrice,
                        priceDifference = highestPrice - bestPrice,
                        averagePrice = Math.Round(options.Average(x => x.FinalPrice), 2),
                        highestDiscount = options.Max(x => x.DiscountPercentage)
                    },
                    vendors = options.Select(x => new
                    {
                        vendorId = x.VendorId,
                        vendorName = x.VendorName,
                        vendorProductId = x.VendorProductId,
                        assignedPrice = x.AssignedPrice,
                        discountPrice = x.DiscountPrice,
                        finalPrice = x.FinalPrice,
                        discountPercentage = x.DiscountPercentage,
                        stock = x.StockQuantity,
                        isActive = x.IsActive,
                        lastUpdated = x.LatestUpdatedOn
                    }).OrderBy(x => x.finalPrice).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets vendor statistics for a specific product
        /// </summary>
        [HttpGet("product/{productId:long}/vendor-stats")]
        [AllowAnonymous]
        public async Task<IActionResult> GetVendorStats(long productId)
        {
            try
            {
                var hasMultiple = await _strategy.HasMultipleActiveVendorsAsync(productId);
                var vendorCount = await _strategy.GetVendorCountAsync(productId);
                var aggregatedStock = await _strategy.GetAggregatedStockAsync(productId);
                var bestOption = await _strategy.GetBestPriceOptionAsync(productId);

                return Ok(new
                {
                    productId = productId,
                    vendorCount = vendorCount,
                    hasMultipleVendors = hasMultiple,
                    totalAggregatedStock = aggregatedStock,
                    bestPrice = bestOption?.FinalPrice,
                    bestVendor = bestOption?.VendorName,
                    isSingleVendor = vendorCount == 1
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    /// <summary>Request model for bulk availability check</summary>
    public class BulkAvailabilityRequest
    {
        public int RequestedQuantity { get; set; }
    }
}
