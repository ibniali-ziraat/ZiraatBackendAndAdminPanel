using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Catalog.Areas.Catalog.Controllers
{
    /// <summary>
    /// Handles product listing with multi-vendor pricing and stock aggregation
    /// Provides APIs for displaying products with best prices and stock availability across vendors
    /// </summary>
    [Area("Catalog")]
    [ApiController]
    [Route("api/products/vendor-listings")]
    public class VendorProductListingApiController : ControllerBase
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IVendorProductService _vendorProductService;

        public VendorProductListingApiController(
            IRepository<Product> productRepository,
            IRepository<VendorProduct> vendorProductRepository,
            IRepository<Vendor> vendorRepository,
            IVendorProductService vendorProductService)
        {
            _productRepository = productRepository;
            _vendorProductRepository = vendorProductRepository;
            _vendorRepository = vendorRepository;
            _vendorProductService = vendorProductService;
        }

        /// <summary>
        /// Gets product details with all active vendor options
        /// Shows each vendor's price, discount, and stock availability
        /// </summary>
        [HttpGet("{productId:long}/all-vendors")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductWithAllVendors(long productId)
        {
            var product = await _productRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == productId && x.IsPublished);

            if (product == null)
            {
                return NotFound();
            }

            var aggregateData = await _vendorProductService.GetAggregateProductData(productId);

            if (aggregateData.VendorCount == 0)
            {
                return Ok(new
                {
                    product = new { id = product.Id, name = product.Name },
                    vendorOptions = new List<object>(),
                    message = "No active vendor assignments for this product"
                });
            }

            // Get vendor details for the options
            var vendorIds = aggregateData.VendorOptions.Select(x => x.VendorId).ToList();
            var vendors = await _vendorRepository.Query()
                .Where(x => vendorIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => new { x.Id, x.Name, x.Slug });

            var vendorOptions = aggregateData.VendorOptions.Select(option => new
            {
                vendorProductId = option.VendorProductId,
                vendorId = option.VendorId,
                vendorName = vendors.TryGetValue(option.VendorId, out var v) ? v.Name : "Unknown",
                price = option.AssignedPrice,
                discountPrice = option.DiscountPrice,
                stock = option.StockQuantity,
                isAvailable = option.StockQuantity > 0 && option.IsActive,
                isBestPrice = option.AssignedPrice == aggregateData.LowestPrice,
                isBestDiscount = option.DiscountPrice == aggregateData.HighestDiscount
            }).ToList();

            return Ok(new
            {
                product = new
                {
                    id = product.Id,
                    name = product.Name,
                    sku = product.Sku
                },
                aggregated = new
                {
                    totalStock = aggregateData.TotalAggregatedStock,
                    lowestPrice = aggregateData.LowestPrice,
                    highestDiscount = aggregateData.HighestDiscount,
                    vendorCount = aggregateData.VendorCount
                },
                vendorOptions = vendorOptions
            });
        }

        /// <summary>
        /// Gets best-priced vendor option for a product
        /// Used for product cards showing "from" pricing
        /// </summary>
        [HttpGet("{productId:long}/best-price")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBestPricedVendorOption(long productId)
        {
            var product = await _productRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == productId && x.IsPublished);

            if (product == null)
            {
                return NotFound();
            }

            var aggregateData = await _vendorProductService.GetAggregateProductData(productId);

            if (aggregateData.VendorCount == 0)
            {
                return Ok(new
                {
                    product = new { id = product.Id, name = product.Name },
                    bestOption = (object)null,
                    message = "No active vendors for this product"
                });
            }

            var bestPriceOption = aggregateData.VendorOptions
                .Where(x => x.AssignedPrice == aggregateData.LowestPrice && x.StockQuantity > 0)
                .FirstOrDefault();

            if (bestPriceOption == null)
            {
                return Ok(new
                {
                    product = new { id = product.Id, name = product.Name },
                    bestOption = (object)null,
                    message = "No vendors with available stock"
                });
            }

            var vendor = await _vendorRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == bestPriceOption.VendorId);

            return Ok(new
            {
                product = new { id = product.Id, name = product.Name },
                bestOption = new
                {
                    vendorProductId = bestPriceOption.VendorProductId,
                    vendorName = vendor?.Name,
                    price = bestPriceOption.AssignedPrice,
                    discountPrice = bestPriceOption.DiscountPrice,
                    stock = bestPriceOption.StockQuantity
                }
            });
        }

        /// <summary>
        /// Gets vendor product assignment details for a specific vendor's offering
        /// </summary>
        [HttpGet("vendor/{vendorProductId:long}/details")]
        [AllowAnonymous]
        public async Task<IActionResult> GetVendorProductDetails(long vendorProductId)
        {
            var vendorProduct = await _vendorProductRepository.Query()
                .Include(x => x.Vendor)
                .FirstOrDefaultAsync(x => x.Id == vendorProductId && x.IsActive);

            if (vendorProduct == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                vendorProductId = vendorProduct.Id,
                productId = vendorProduct.ProductId,
                productName = vendorProduct.ProductName,
                productSku = vendorProduct.ProductSku,
                vendor = new
                {
                    id = vendorProduct.Vendor.Id,
                    name = vendorProduct.Vendor.Name,
                    slug = vendorProduct.Vendor.Slug
                },
                pricing = new
                {
                    assignedPrice = vendorProduct.AssignedPrice,
                    originalPrice = vendorProduct.OriginalPrice,
                    discountPrice = vendorProduct.DiscountPrice,
                    discountPercentage = vendorProduct.DiscountPrice > 0
                        ? Math.Round((decimal)((vendorProduct.AssignedPrice - vendorProduct.DiscountPrice) / vendorProduct.AssignedPrice * 100), 2)
                        : 0
                },
                stock = new
                {
                    quantity = vendorProduct.StockQuantity,
                    isAvailable = vendorProduct.StockQuantity > 0
                },
                lastUpdated = vendorProduct.LatestUpdatedOn
            });
        }

        /// <summary>
        /// Compares prices across all vendors for a specific product
        /// Shows price differences and best deal
        /// </summary>
        [HttpGet("{productId:long}/price-comparison")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPriceComparison(long productId)
        {
            var product = await _productRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            var aggregateData = await _vendorProductService.GetAggregateProductData(productId);

            if (aggregateData.VendorCount == 0)
            {
                return Ok(new
                {
                    productId = productId,
                    vendors = new List<object>(),
                    message = "No vendor assignments found"
                });
            }

            var vendorIds = aggregateData.VendorOptions.Select(x => x.VendorId).ToList();
            var vendors = await _vendorRepository.Query()
                .Where(x => vendorIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var comparison = aggregateData.VendorOptions
                .OrderBy(x => x.AssignedPrice ?? decimal.MaxValue)
                .Select(option => new
                {
                    vendorProductId = option.VendorProductId,
                    vendorId = option.VendorId,
                    vendorName = vendors.TryGetValue(option.VendorId, out var name) ? name : "Unknown",
                    price = option.AssignedPrice,
                    discountPrice = option.DiscountPrice,
                    finalPrice = option.DiscountPrice > 0 ? option.DiscountPrice : option.AssignedPrice,
                    savings = option.DiscountPrice > 0
                        ? option.AssignedPrice - option.DiscountPrice
                        : 0,
                    stock = option.StockQuantity,
                    isAvailable = option.StockQuantity > 0
                })
                .ToList();

            return Ok(new
            {
                productId = productId,
                productName = product.Name,
                lowestPrice = comparison.First().finalPrice,
                vendors = comparison
            });
        }

        /// <summary>
        /// Checks stock availability across all vendors for bulk purchase consideration
        /// </summary>
        [HttpPost("{productId:long}/check-bulk-availability")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckBulkAvailability(long productId, [FromBody] int requestedQuantity)
        {
            var product = await _productRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            var aggregateData = await _vendorProductService.GetAggregateProductData(productId);

            var canFulfill = aggregateData.TotalAggregatedStock >= requestedQuantity;

            var vendorIds = aggregateData.VendorOptions.Select(x => x.VendorId).ToList();
            var vendors = await _vendorRepository.Query()
                .Where(x => vendorIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var fulfillmentOptions = aggregateData.VendorOptions
                .Where(x => x.StockQuantity > 0)
                .Select(option => new
                {
                    vendorProductId = option.VendorProductId,
                    vendorName = vendors.TryGetValue(option.VendorId, out var name) ? name : "Unknown",
                    availableQuantity = option.StockQuantity,
                    price = option.AssignedPrice,
                    canFulfillFull = option.StockQuantity >= requestedQuantity,
                    canFulfillPartial = option.StockQuantity > 0
                })
                .OrderByDescending(x => x.availableQuantity)
                .ToList();

            return Ok(new
            {
                productId = productId,
                requestedQuantity = requestedQuantity,
                totalAvailableStock = aggregateData.TotalAggregatedStock,
                canFulfillFromSingleVendor = fulfillmentOptions.Any(x => x.canFulfillFull),
                requiresMultipleVendors = canFulfill && !fulfillmentOptions.Any(x => x.canFulfillFull),
                fulfillmentOptions = fulfillmentOptions
            });
        }
    }
}
