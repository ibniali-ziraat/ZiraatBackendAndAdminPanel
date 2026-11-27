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
    /// Provides detailed pricing analysis and comparisons across vendors
    /// Helps customers make informed purchasing decisions
    /// </summary>
    [Area("Catalog")]
    [ApiController]
    [Route("api/products/pricing")]
    public class VendorPricingComparisonController : ControllerBase
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IVendorProductService _vendorProductService;

        public VendorPricingComparisonController(
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
        /// Get detailed pricing breakdown for a product across all vendors
        /// Shows MRP, assigned price, discounts, taxes, and final prices
        /// </summary>
        [HttpGet("{productId:long}/detailed-pricing")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDetailedPricing(long productId)
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
                    .Where(x => x.ProductId == productId && x.IsActive)
                    .Include(x => x.Vendor)
                    .OrderBy(x => x.AssignedPrice)
                    .ToListAsync();

                if (!vendorProducts.Any())
                {
                    return Ok(new
                    {
                        productId = productId,
                        productName = product.Name,
                        vendorPricing = new List<object>()
                    });
                }

                var pricing = vendorProducts.Select(vp => new
                {
                    vendorProductId = vp.Id,
                    vendorId = vp.VendorId,
                    vendorName = vp.Vendor?.Name,
                    mrp = vp.MaximumRetailPrice,
                    assignedPrice = vp.AssignedPrice,
                    discountPrice = vp.DiscountPrice,
                    discountAmount = (vp.AssignedPrice ?? 0) - (vp.DiscountPrice ?? vp.AssignedPrice ?? 0),
                    discountPercentage = CalculateDiscountPercentage(vp),
                    taxRate = vp.TaxRate,
                    taxAmount = CalculateTaxAmount(vp),
                    netSalePrice = vp.NetSalePriceForZiraat,
                    finalPrice = vp.DiscountPrice ?? vp.AssignedPrice,
                    isAvailableForCreditBuyers = vp.IsAvailableForCreditBuyers,
                    creditSalePrice = vp.SalePriceForCreditor,
                    creditHikePercentage = vp.CreditSaleHikePercentage,
                    stock = vp.StockQuantity,
                    isBestPrice = vp.AssignedPrice == vendorProducts.Min(x => x.AssignedPrice)
                }).ToList();

                return Ok(new
                {
                    productId = productId,
                    productName = product.Name,
                    vendorCount = pricing.Count,
                    lowestPrice = pricing.Min(x => (decimal?)x.finalPrice) ?? 0,
                    highestPrice = pricing.Max(x => (decimal?)x.finalPrice) ?? 0,
                    vendorPricing = pricing
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving pricing details", error = ex.Message });
            }
        }

        /// <summary>
        /// Compare prices across vendors for multiple products
        /// Useful for bulk purchase decisions
        /// </summary>
        [HttpPost("compare-multiple")]
        [AllowAnonymous]
        public async Task<IActionResult> CompareMultipleProducts([FromBody] MultiProductPricingRequest request)
        {
            if (request?.ProductIds == null || !request.ProductIds.Any())
            {
                return BadRequest(new { message = "Product IDs are required" });
            }

            try
            {
                var results = new List<object>();

                foreach (var productId in request.ProductIds.Distinct().Take(50))
                {
                    var vendorProducts = await _vendorProductRepository.Query()
                        .Where(x => x.ProductId == productId && x.IsActive)
                        .Select(x => new { x.AssignedPrice, x.DiscountPrice })
                        .ToListAsync();

                    if (vendorProducts.Any())
                    {
                        var lowestPrice = vendorProducts.Min(x => x.DiscountPrice ?? x.AssignedPrice);

                        results.Add(new
                        {
                            productId = productId,
                            lowestPrice = lowestPrice,
                            vendorCount = vendorProducts.Count
                        });
                    }
                }

                return Ok(new { comparedProducts = results.Count, data = results });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error comparing products", error = ex.Message });
            }
        }

        /// <summary>
        /// Get average pricing information for a vendor's products
        /// Shows how vendor's prices compare to market
        /// </summary>
        [HttpGet("vendor/{vendorId:long}/average-prices")]
        [AllowAnonymous]
        public async Task<IActionResult> GetVendorAveragePrices(long vendorId)
        {
            try
            {
                var vendor = await _vendorRepository.Query()
                    .FirstOrDefaultAsync(x => x.Id == vendorId);

                if (vendor == null)
                {
                    return NotFound(new { message = "Vendor not found" });
                }

                var vendorProducts = await _vendorProductRepository.Query()
                    .Where(x => x.VendorId == vendorId && x.IsActive)
                    .Select(x => new { x.AssignedPrice, x.DiscountPrice })
                    .ToListAsync();

                if (!vendorProducts.Any())
                {
                    return Ok(new
                    {
                        vendorId = vendorId,
                        vendorName = vendor.Name,
                        productCount = 0,
                        averagePrice = 0,
                        medianPrice = 0,
                        minPrice = 0,
                        maxPrice = 0
                    });
                }

                var prices = vendorProducts.Select(x => x.DiscountPrice ?? x.AssignedPrice ?? 0).OrderBy(x => x).ToList();

                var averagePrice = prices.Average();
                var medianPrice = prices.Count % 2 == 0
                    ? (prices[prices.Count / 2 - 1] + prices[prices.Count / 2]) / 2
                    : prices[prices.Count / 2];

                return Ok(new
                {
                    vendorId = vendorId,
                    vendorName = vendor.Name,
                    productCount = vendorProducts.Count,
                    averagePrice = Math.Round(averagePrice, 2),
                    medianPrice = Math.Round(medianPrice, 2),
                    minPrice = prices.First(),
                    maxPrice = prices.Last()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving vendor average prices", error = ex.Message });
            }
        }

        /// <summary>
        /// Calculate total cost for a cart with pricing breakdown
        /// Includes tax calculations per vendor
        /// </summary>
        [HttpPost("calculate-total")]
        [AllowAnonymous]
        public async Task<IActionResult> CalculateTotal([FromBody] CartPricingCalculationRequest request)
        {
            if (request?.Items == null || !request.Items.Any())
            {
                return BadRequest(new { message = "Cart items are required" });
            }

            try
            {
                decimal totalPrice = 0;
                decimal totalTax = 0;
                var itemBreakdown = new List<object>();

                foreach (var item in request.Items)
                {
                    var vendorProduct = await _vendorProductRepository.Query()
                        .FirstOrDefaultAsync(x => x.Id == item.VendorProductId && x.IsActive);

                    if (vendorProduct == null)
                    {
                        continue;
                    }

                    var itemPrice = (vendorProduct.DiscountPrice ?? vendorProduct.AssignedPrice ?? 0) * item.Quantity;
                    var itemTax = CalculateTaxAmount(vendorProduct) * item.Quantity;
                    var itemTotal = itemPrice + itemTax;

                    totalPrice += itemPrice;
                    totalTax += itemTax;

                    itemBreakdown.Add(new
                    {
                        vendorProductId = item.VendorProductId,
                        quantity = item.Quantity,
                        pricePerUnit = vendorProduct.DiscountPrice ?? vendorProduct.AssignedPrice,
                        itemTotal = Math.Round(itemTotal, 2)
                    });
                }

                return Ok(new
                {
                    subtotal = Math.Round(totalPrice, 2),
                    tax = Math.Round(totalTax, 2),
                    total = Math.Round(totalPrice + totalTax, 2),
                    itemCount = request.Items.Count,
                    itemBreakdown = itemBreakdown
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error calculating total", error = ex.Message });
            }
        }

        #region Helper Methods

        private decimal CalculateDiscountPercentage(VendorProduct vp)
        {
            if (vp.AssignedPrice == null || vp.DiscountPrice == null)
            {
                return 0;
            }

            return Math.Round(((vp.AssignedPrice.Value - vp.DiscountPrice.Value) / vp.AssignedPrice.Value) * 100, 2);
        }

        private decimal CalculateTaxAmount(VendorProduct vp)
        {
            if (vp.AssignedPrice == null || vp.TaxRate == null)
            {
                return 0;
            }

            return Math.Round((vp.AssignedPrice.Value * vp.TaxRate.Value) / 100, 2);
        }

        #endregion
    }

    // Request models
    public class MultiProductPricingRequest
    {
        public List<long> ProductIds { get; set; }
    }

    public class CartPricingCalculationRequest
    {
        public List<CartItemPricingDto> Items { get; set; }
    }

    public class CartItemPricingDto
    {
        public long VendorProductId { get; set; }
        public int Quantity { get; set; }
    }
}
