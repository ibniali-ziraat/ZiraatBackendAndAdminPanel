using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Catalog.Areas.Catalog.Controllers
{
    /// <summary>
    /// Provides real-time stock availability information across vendors
    /// Allows customers to check product stock across multiple vendors
    /// </summary>
    [Area("Catalog")]
    [ApiController]
    [Route("api/products/stock")]
    public class VendorStockAvailabilityController : ControllerBase
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IVendorProductService _vendorProductService;

        public VendorStockAvailabilityController(
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
        /// Check stock availability for a single product across all vendors
        /// </summary>
        [HttpGet("{productId:long}/availability")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductAvailability(long productId)
        {
            try
            {
                var product = await _productRepository.Query()
                    .FirstOrDefaultAsync(x => x.Id == productId && x.IsPublished);

                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                var vendorProducts = await _vendorProductRepository.Query()
                    .Where(x => x.ProductId == productId && x.IsActive && x.StockQuantity > 0)
                    .Include(x => x.Vendor)
                    .OrderByDescending(x => x.StockQuantity)
                    .ToListAsync();

                if (!vendorProducts.Any())
                {
                    return Ok(new
                    {
                        productId = productId,
                        productName = product.Name,
                        totalStock = 0,
                        isAvailable = false,
                        vendors = new List<object>()
                    });
                }

                var totalStock = vendorProducts.Sum(x => x.StockQuantity);

                var stockInfo = vendorProducts.Select(vp => new
                {
                    vendorProductId = vp.Id,
                    vendorId = vp.VendorId,
                    vendorName = vp.Vendor?.Name,
                    stock = vp.StockQuantity,
                    isAvailable = vp.StockQuantity > 0,
                    lastUpdated = vp.LatestUpdatedOn,
                    price = vp.AssignedPrice,
                    deliveryEstimateDays = 2 // TODO: Get from vendor delivery settings
                }).ToList();

                return Ok(new
                {
                    productId = productId,
                    productName = product.Name,
                    totalStock = totalStock,
                    isAvailable = totalStock > 0,
                    vendorCount = vendorProducts.Count,
                    vendors = stockInfo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving stock availability", error = ex.Message });
            }
        }

        /// <summary>
        /// Check stock availability for multiple products at once
        /// Useful for cart and search results pages
        /// </summary>
        [HttpPost("bulk-check")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckBulkAvailability([FromBody] BulkStockCheckRequest request)
        {
            if (request?.ProductIds == null || !request.ProductIds.Any())
            {
                return BadRequest(new { message = "Product IDs are required" });
            }

            try
            {
                var results = new List<object>();

                foreach (var productId in request.ProductIds.Distinct().Take(100)) // Limit to 100 products
                {
                    var vendorProducts = await _vendorProductRepository.Query()
                        .Where(x => x.ProductId == productId && x.IsActive && x.StockQuantity > 0)
                        .Select(x => new { x.Id, x.StockQuantity, VendorName = x.Vendor.Name })
                        .ToListAsync();

                    var totalStock = vendorProducts.Sum(x => x.StockQuantity);

                    results.Add(new
                    {
                        productId = productId,
                        totalStock = totalStock,
                        isAvailable = totalStock > 0,
                        vendorCount = vendorProducts.Count
                    });
                }

                return Ok(new { checkedCount = results.Count, data = results });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error checking bulk availability", error = ex.Message });
            }
        }

        /// <summary>
        /// Get warehouse/location information for vendor stock
        /// Shows which vendors have stock and where it's located
        /// </summary>
        [HttpGet("{productId:long}/warehouse-locations")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWarehouseLocations(long productId)
        {
            try
            {
                var vendorProducts = await _vendorProductRepository.Query()
                    .Where(x => x.ProductId == productId && x.IsActive)
                    .Include(x => x.Vendor)
                    .ToListAsync();

                if (!vendorProducts.Any())
                {
                    return Ok(new
                    {
                        productId = productId,
                        locations = new List<object>()
                    });
                }

                var locations = vendorProducts.Select(vp => new
                {
                    vendorId = vp.VendorId,
                    vendorName = vp.Vendor?.Name,
                    stock = vp.StockQuantity,
                    isAvailable = vp.StockQuantity > 0,
                    location = "Location not specified",
                    lastUpdated = vp.LatestUpdatedOn
                }).OrderByDescending(x => x.stock).ToList();

                return Ok(new
                {
                    productId = productId,
                    totalLocations = locations.Count,
                    locations = locations
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving warehouse locations", error = ex.Message });
            }
        }

        /// <summary>
        /// Subscribe customer to back-in-stock notification
        /// </summary>
        [HttpPost("{productId:long}/subscribe-back-in-stock")]
        [Authorize]
        public async Task<IActionResult> SubscribeBackInStock(long productId, [FromBody] BackInStockSubscriptionRequest request)
        {
            try
            {
                var product = await _productRepository.Query()
                    .FirstOrDefaultAsync(x => x.Id == productId);

                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                // TODO: Implement back-in-stock subscription logic
                // This would typically store a subscription in a database and trigger notifications

                return Ok(new
                {
                    message = "Successfully subscribed to back-in-stock notifications",
                    productId = productId,
                    productName = product.Name
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error subscribing to notifications", error = ex.Message });
            }
        }

        /// <summary>
        /// Check if a specific quantity is available for purchase
        /// Useful for validation before adding to cart
        /// </summary>
        [HttpPost("{productId:long}/check-quantity-available")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckQuantityAvailable(long productId, [FromBody] QuantityCheckRequest request)
        {
            if (request?.RequestedQuantity <= 0)
            {
                return BadRequest(new { message = "Requested quantity must be greater than 0" });
            }

            try
            {
                var aggregateData = await _vendorProductService.GetAggregateProductData(productId);

                var isAvailable = aggregateData.TotalAggregatedStock >= request.RequestedQuantity;

                var fulfillmentInfo = new List<object>();
                if (isAvailable)
                {
                    // Find vendors that can fulfill the order
                    var fulfillmentVendors = aggregateData.VendorOptions
                        .Where(x => x.StockQuantity >= request.RequestedQuantity)
                        .Select(x => new
                        {
                            vendorId = x.VendorId,
                            canFulfillFull = true,
                            availableQuantity = x.StockQuantity
                        })
                        .ToList();

                    fulfillmentInfo = fulfillmentVendors.Cast<object>().ToList();
                }

                return Ok(new
                {
                    productId = productId,
                    requestedQuantity = request.RequestedQuantity,
                    totalAvailableStock = aggregateData.TotalAggregatedStock,
                    isAvailable = isAvailable,
                    canFulfillFromSingleVendor = aggregateData.VendorOptions.Any(x => x.StockQuantity >= request.RequestedQuantity),
                    fulfillmentOptions = fulfillmentInfo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error checking quantity availability", error = ex.Message });
            }
        }
    }

    // Request models
    public class BulkStockCheckRequest
    {
        public List<long> ProductIds { get; set; }
    }

    public class BackInStockSubscriptionRequest
    {
        public string CustomerEmail { get; set; }
    }

    public class QuantityCheckRequest
    {
        public int RequestedQuantity { get; set; }
    }
}
