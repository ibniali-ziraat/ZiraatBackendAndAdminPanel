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
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Catalog.Areas.Catalog.Controllers
{
    /// <summary>
    /// Provides credit buyer specific pricing, availability, and eligibility information
    /// </summary>
    [Area("Catalog")]
    [ApiController]
    [Route("api/products/credit-buyers")]
    [Authorize]
    public class VendorCreditBuyerController : ControllerBase
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IWorkContext _workContext;

        public VendorCreditBuyerController(
            IRepository<Product> productRepository,
            IRepository<VendorProduct> vendorProductRepository,
            IRepository<Vendor> vendorRepository,
            IWorkContext workContext)
        {
            _productRepository = productRepository;
            _vendorProductRepository = vendorProductRepository;
            _vendorRepository = vendorRepository;
            _workContext = workContext;
        }

        /// <summary>
        /// Get credit purchase options for a specific product
        /// Shows vendors offering credit and their credit-specific pricing
        /// </summary>
        [HttpGet("{productId:long}/credit-options")]
        public async Task<IActionResult> GetCreditOptions(long productId)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var product = await _productRepository.Query()
                    .FirstOrDefaultAsync(x => x.Id == productId && x.IsPublished);

                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                // TODO: Check if user is eligible for credit buyer
                var isEligibleForCredit = await CheckCreditEligibility(currentUser.Id);

                var creditOptions = await _vendorProductRepository.Query()
                    .Where(x => x.ProductId == productId 
                        && x.IsActive 
                        && x.IsAvailableForCreditBuyers
                        && x.StockQuantity > 0)
                    .Include(x => x.Vendor)
                    .Select(x => new
                    {
                        vendorProductId = x.Id,
                        vendorId = x.VendorId,
                        vendorName = x.Vendor.Name,
                        regularPrice = x.AssignedPrice,
                        discountPrice = x.DiscountPrice,
                        creditSalePrice = x.SalePriceForCreditor,
                        creditHike = x.CreditSaleHikePercentage,
                        creditTerms = $"{x.CreditSaleHikePercentage}% markup",
                        stock = x.StockQuantity
                    })
                    .ToListAsync();

                if (!creditOptions.Any())
                {
                    return Ok(new
                    {
                        productId = productId,
                        isEligibleForCredit = isEligibleForCredit,
                        creditOptions = new List<object>(),
                        message = "No credit options available for this product"
                    });
                }

                return Ok(new
                {
                    productId = productId,
                    productName = product.Name,
                    isEligibleForCredit = isEligibleForCredit,
                    vendorCount = creditOptions.Count,
                    lowestCreditPrice = creditOptions.Min(x => (decimal?)x.creditSalePrice) ?? 0,
                    creditOptions = creditOptions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving credit options", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all products available for credit buyers
        /// Supports filtering by category and price range
        /// </summary>
        [HttpGet("eligible-products")]
        public async Task<IActionResult> GetEligibleProducts(
            [FromQuery] long? categoryId = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var isEligibleForCredit = await CheckCreditEligibility(currentUser.Id);
                if (!isEligibleForCredit)
                {
                    return Forbid();
                }

                var query = _vendorProductRepository.Query()
                    .Where(x => x.IsActive && x.IsAvailableForCreditBuyers && x.StockQuantity > 0)
                    .Include(x => x.Vendor);

                var products = await query
                    .OrderBy(x => x.SalePriceForCreditor)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new
                    {
                        vendorProductId = x.Id,
                        productId = x.ProductId,
                        productName = x.ProductName,
                        vendorName = x.Vendor.Name,
                        creditPrice = x.SalePriceForCreditor,
                        regularPrice = x.AssignedPrice,
                        creditTerms = $"{x.CreditSaleHikePercentage}% markup",
                        stock = x.StockQuantity
                    })
                    .ToListAsync();

                return Ok(new
                {
                    page = page,
                    pageSize = pageSize,
                    totalCount = await query.CountAsync(),
                    products = products
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving eligible products", error = ex.Message });
            }
        }

        /// <summary>
        /// Calculate credit price for a product from a specific vendor
        /// Shows the breakdown with markup applied
        /// </summary>
        [HttpPost("calculate-credit-price")]
        public async Task<IActionResult> CalculateCreditPrice([FromBody] CreditPriceCalculationRequest request)
        {
            if (request == null || request.VendorProductId <= 0 || request.Quantity <= 0)
            {
                return BadRequest(new { message = "Invalid vendor product ID or quantity" });
            }

            try
            {
                var vendorProduct = await _vendorProductRepository.Query()
                    .Include(x => x.Vendor)
                    .FirstOrDefaultAsync(x => x.Id == request.VendorProductId && x.IsActive);

                if (vendorProduct == null)
                {
                    return NotFound(new { message = "Vendor product not found" });
                }

                if (!vendorProduct.IsAvailableForCreditBuyers)
                {
                    return BadRequest(new { message = "This product is not available for credit buyers" });
                }

                if (vendorProduct.StockQuantity < request.Quantity)
                {
                    return BadRequest(new { message = "Insufficient stock for credit purchase" });
                }

                var regularPrice = vendorProduct.DiscountPrice ?? vendorProduct.AssignedPrice ?? 0;
                var creditPrice = vendorProduct.SalePriceForCreditor ?? regularPrice;
                var hikePercentage = vendorProduct.CreditSaleHikePercentage ?? 0;
                var hikeAmount = regularPrice * (hikePercentage / 100);

                var totalCreditPrice = creditPrice * request.Quantity;

                return Ok(new
                {
                    vendorProductId = request.VendorProductId,
                    productName = vendorProduct.ProductName,
                    vendorName = vendorProduct.Vendor?.Name,
                    quantity = request.Quantity,
                    regularPricePerUnit = Math.Round(regularPrice, 2),
                    creditHikePercentage = hikePercentage,
                    hikeAmountPerUnit = Math.Round(hikeAmount, 2),
                    creditPricePerUnit = Math.Round(creditPrice, 2),
                    totalRegularPrice = Math.Round(regularPrice * request.Quantity, 2),
                    totalHikeAmount = Math.Round(hikeAmount * request.Quantity, 2),
                    totalCreditPrice = Math.Round(totalCreditPrice, 2)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error calculating credit price", error = ex.Message });
            }
        }

        /// <summary>
        /// Get credit terms and conditions from vendors
        /// Shows payment terms, duration, and other conditions
        /// </summary>
        [HttpGet("vendor-credit-terms")]
        public async Task<IActionResult> GetVendorCreditTerms([FromQuery] long? vendorId = null)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var terms = new List<object>
                {
                    new
                    {
                        vendorId = 1,
                        vendorName = "Vendor 1",
                        creditTermsInDays = 30,
                        interestRate = 0,
                        processingFeePercentage = 2,
                        minCreditAmount = 1000,
                        maxCreditAmount = 100000,
                        conditions = "30 days credit with 2% processing fee"
                    },
                    new
                    {
                        vendorId = 2,
                        vendorName = "Vendor 2",
                        creditTermsInDays = 45,
                        interestRate = 0,
                        processingFeePercentage = 3,
                        minCreditAmount = 2000,
                        maxCreditAmount = 200000,
                        conditions = "45 days credit with 3% processing fee"
                    }
                };

                return Ok(new
                {
                    terms = terms
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving credit terms", error = ex.Message });
            }
        }

        #region Helper Methods

        private async Task<bool> CheckCreditEligibility(long userId)
        {
            // TODO: Implement actual credit eligibility check
            // This should verify: user profile status, payment history, credit score, etc.
            return true;
        }

        #endregion
    }

    // Request models
    public class CreditPriceCalculationRequest
    {
        public long VendorProductId { get; set; }
        public int Quantity { get; set; }
    }
}
