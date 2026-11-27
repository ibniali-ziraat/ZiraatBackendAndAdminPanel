using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Catalog.Areas.Catalog.Controllers
{
    /// <summary>
    /// Provides customer-friendly product discovery with multi-vendor integration
    /// Includes trending, deals, and search with vendor aggregation
    /// </summary>
    [Area("Catalog")]
    [ApiController]
    [Route("api/products/discover")]
    public class ProductDiscoveryController : ControllerBase
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IRepository<VendorCustomerAssociation> _vendorCustomerAssociationRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IVendorProductService _vendorProductService;
        private readonly IMediaService _mediaService;

        public ProductDiscoveryController(
            IRepository<Product> productRepository,
            IRepository<VendorProduct> vendorProductRepository,
            IRepository<VendorCustomerAssociation> vendorCustomerAssociationRepository,
            IRepository<Category> categoryRepository,
            IVendorProductService vendorProductService,
            IMediaService mediaService
        )
        {
            _productRepository = productRepository;
            _vendorProductRepository = vendorProductRepository;
            _vendorCustomerAssociationRepository = vendorCustomerAssociationRepository;
            _categoryRepository = categoryRepository;
            _vendorProductService = vendorProductService;
            _mediaService = mediaService;
        }

        /// <summary>
        /// Get trending products with vendor aggregation
        /// Shows most viewed/popular products across multiple vendors
        /// </summary>
        [HttpGet("trending")]
        [Authorize]
        public async Task<IActionResult> GetTrendingProducts(
            [FromQuery] int? limit = 20,
            [FromQuery] long? categoryId = null,
            [FromQuery] int? minVendors = null
        )
        {
            try
            {
                var vendorIds = await GetCurrentCustomerVendorIds();
                var query = DiscoverableProducts(minVendors, vendorIds);

                if (categoryId.HasValue)
                {
                    query = query.Where(x =>
                        x.Categories.Any(c => c.CategoryId == categoryId.Value)
                    );
                }

                var products = await query
                    .OrderByDescending(x => x.CreatedOn)
                    .ThenBy(x => x.Id)
                    .Take(limit ?? 20)
                    .Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.Price,
                        x.ThumbnailImage,
                        x.Medias,
                    })
                    .ToListAsync();

                var productIds = products.Select(x => x.Id).ToList();
                var vendorOffers = await EligibleAssignments()
                    .Where(x => vendorIds.Contains(x.VendorId) && productIds.Contains(x.ProductId))
                    .OrderBy(x => x.Id)
                    .ToListAsync();
                var offersByProductId = vendorOffers
                    .GroupBy(x => x.ProductId)
                    .ToDictionary(x => x.Key, x => x.ToList());
                var trendingProducts = new List<object>();

                foreach (var product in products)
                {
                    var productOffers = offersByProductId[product.Id];
                    var bestOffer = productOffers
                        .OrderBy(x => x.DiscountPrice ?? x.AssignedPrice)
                        .ThenBy(x => x.Id)
                        .First();

                    var thumbnail =
                        product.ThumbnailImage != null
                            ? _mediaService.GetMediaUrl(product.ThumbnailImage)
                            : _mediaService.GetMediaUrl("no-image.png");

                    var images = product
                        .Medias.OrderBy(m => m.DisplayOrder)
                        .Select(m => new
                        {
                            mediaId = m.MediaId,
                            url = _mediaService.GetMediaUrl(m.Media),
                            displayOrder = m.DisplayOrder,
                        })
                        .ToList();

                    trendingProducts.Add(
                        new
                        {
                            productId = product.Id,
                            vendorProductId = bestOffer.Id,
                            vendorId = bestOffer.VendorId,
                            productName = product.Name,
                            views = 0,
                            originalPrice = bestOffer.AssignedPrice ?? product.Price,
                            lowestPrice = bestOffer.DiscountPrice ?? bestOffer.AssignedPrice,
                            vendorCount = productOffers.Select(x => x.VendorId).Distinct().Count(),
                            totalStock = productOffers.Sum(x => x.StockQuantity),
                            thumbnail = thumbnail,
                            images = images,
                        }
                    );
                }

                return Ok(new { count = trendingProducts.Count, products = trendingProducts });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving trending products", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get best deals - products with highest discounts across vendors
        /// </summary>
        [HttpGet("best-deals")]
        [Authorize]
        public async Task<IActionResult> GetBestDeals([FromQuery] int? limit = 20)
        {
            try
            {
                var vendorIds = await GetCurrentCustomerVendorIds();
                var vendorProducts = await EligibleAssignments()
                    .Where(x => vendorIds.Contains(x.VendorId))
                    .Where(x =>
                        x.StockQuantity > 0
                        && _productRepository
                            .Query()
                            .Any(p => p.Id == x.ProductId && p.IsPublished && !p.IsDeleted)
                    )
                    .OrderByDescending(x => ((x.AssignedPrice ?? 0) - (x.DiscountPrice ?? 0)))
                    .Take(limit ?? 20)
                    .Select(x => new
                    {
                        x.Id,
                        x.VendorId,
                        x.ProductId,
                        x.ProductName,
                        x.AssignedPrice,
                        x.DiscountPrice,
                    })
                    .ToListAsync();

                var deals = new List<object>();

                foreach (var vp in vendorProducts)
                {
                    var product = await _productRepository
                        .Query()
                        .Where(p => p.Id == vp.ProductId)
                        .Select(p => new
                        {
                            p.Id,
                            p.ThumbnailImage,
                            p.Medias,
                        })
                        .FirstOrDefaultAsync();

                    var thumbnail =
                        product?.ThumbnailImage != null
                            ? _mediaService.GetMediaUrl(product.ThumbnailImage)
                            : _mediaService.GetMediaUrl("no-image.png");

                    var images = product
                        ?.Medias.OrderBy(m => m.DisplayOrder)
                        .Select(m => new
                        {
                            mediaId = m.MediaId,
                            url = _mediaService.GetMediaUrl(m.Media),
                            displayOrder = m.DisplayOrder,
                        })
                        .ToList();

                    deals.Add(
                        new
                        {
                            vendorProductId = vp.Id,
                            vendorId = vp.VendorId,
                            productId = vp.ProductId,
                            productName = vp.ProductName,
                            originalPrice = vp.AssignedPrice,
                            discountedPrice = vp.DiscountPrice,
                            savingsAmount = (vp.AssignedPrice ?? 0)
                                - (vp.DiscountPrice ?? vp.AssignedPrice ?? 0),
                            savingsPercentage = CalculateDiscountPercentage(
                                vp.AssignedPrice,
                                vp.DiscountPrice
                            ),
                            thumbnail = thumbnail,
                            images = images,
                        }
                    );
                }

                return Ok(new { count = deals.Count, deals = deals });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving best deals", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Search products with vendor aggregation and filtering
        /// </summary>
        [HttpPost("search-with-vendors")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchWithVendors([FromBody] ProductSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.SearchTerm))
            {
                return BadRequest(new { message = "Search term is required" });
            }

            try
            {
                var searchTerm = request.SearchTerm.ToLower();
                var query = DiscoverableProducts(request.MinVendors)
                    .Where(x =>
                        (
                            x.Name.ToLower().Contains(searchTerm)
                            || (
                                x.ShortDescription != null
                                && x.ShortDescription.ToLower().Contains(searchTerm)
                            )
                        )
                    )
                    .AsQueryable();

                // Apply price filter
                if (request.MaxPrice.HasValue || request.MinPrice.HasValue)
                {
                    var offers = EligibleAssignments();
                    query = query.Where(p =>
                        offers.Any(v =>
                            v.ProductId == p.Id
                            && (
                                !request.MinPrice.HasValue
                                || (v.DiscountPrice ?? v.AssignedPrice) >= request.MinPrice.Value
                            )
                            && (
                                !request.MaxPrice.HasValue
                                || (v.DiscountPrice ?? v.AssignedPrice) <= request.MaxPrice.Value
                            )
                        )
                    );
                }

                var products = await query
                    .OrderBy(x => x.Id)
                    .Take(50)
                    .Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.Price,
                        x.ShortDescription,
                    })
                    .ToListAsync();

                var searchResults = new List<object>();

                foreach (var product in products)
                {
                    var aggregateData = await _vendorProductService.GetAggregateProductData(
                        product.Id
                    );

                    if (
                        request.MinVendors.HasValue
                        && aggregateData.VendorCount < request.MinVendors.Value
                    )
                    {
                        continue;
                    }

                    searchResults.Add(
                        new
                        {
                            productId = product.Id,
                            productName = product.Name,
                            description = product.ShortDescription,
                            originalPrice = product.Price,
                            lowestPrice = aggregateData.LowestPrice,
                            highestDiscount = aggregateData.HighestDiscount,
                            vendorCount = aggregateData.VendorCount,
                            totalStock = aggregateData.TotalAggregatedStock,
                            hasMultipleVendors = aggregateData.VendorCount > 1,
                        }
                    );
                }

                return Ok(
                    new
                    {
                        searchTerm = request.SearchTerm,
                        resultsCount = searchResults.Count,
                        results = searchResults,
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "Error searching products", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Compare specific products side-by-side with vendor options
        /// </summary>
        [HttpGet("compare-products")]
        [AllowAnonymous]
        public async Task<IActionResult> CompareProducts([FromQuery] string productIds)
        {
            if (string.IsNullOrWhiteSpace(productIds))
            {
                return BadRequest(new { message = "Product IDs are required" });
            }

            try
            {
                var ids = productIds
                    .Split(',')
                    .Select(x => long.TryParse(x.Trim(), out var id) ? id : 0)
                    .Where(x => x > 0)
                    .ToList();

                if (!ids.Any())
                {
                    return BadRequest(new { message = "Invalid product IDs" });
                }

                var products = await DiscoverableProducts()
                    .Where(x => ids.Contains(x.Id))
                    .ToListAsync();

                var comparison = new List<object>();

                foreach (var product in products)
                {
                    var aggregateData = await _vendorProductService.GetAggregateProductData(
                        product.Id
                    );

                    comparison.Add(
                        new
                        {
                            productId = product.Id,
                            productName = product.Name,
                            description = product.ShortDescription,
                            originalPrice = product.Price,
                            vendorCount = aggregateData.VendorCount,
                            lowestPrice = aggregateData.LowestPrice,
                            highestDiscount = aggregateData.HighestDiscount,
                            totalStock = aggregateData.TotalAggregatedStock,
                            rating = product.RatingAverage,
                            reviewCount = product.ReviewsCount,
                        }
                    );
                }

                return Ok(new { comparedProducts = comparison.Count, comparison = comparison });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "Error comparing products", error = ex.Message }
                );
            }
        }

        #region Helper Methods

        private IQueryable<VendorProduct> EligibleAssignments()
        {
            var today = DateTime.UtcNow.Date;
            return _vendorProductRepository
                .Query()
                .Where(x =>
                    x.IsActive
                    && x.Vendor != null
                    && !x.Vendor.IsDeleted
                    && (!x.ExpiryDate.HasValue || x.ExpiryDate.Value >= today)
                );
        }

        private IQueryable<Product> DiscoverableProducts(
            int? minVendors = null,
            List<long> vendorIds = null
        )
        {
            var assignments = EligibleAssignments();
            if (vendorIds != null)
            {
                assignments = assignments.Where(x => vendorIds.Contains(x.VendorId));
            }

            var requiredVendors = Math.Max(1, minVendors ?? 1);
            return _productRepository
                .Query()
                .Where(p =>
                    p.IsPublished
                    && !p.IsDeleted
                    && assignments
                        .Where(v => v.ProductId == p.Id)
                        .Select(v => v.VendorId)
                        .Distinct()
                        .Count() >= requiredVendors
                );
        }

        private async Task<List<long>> GetCurrentCustomerVendorIds()
        {
            var customerIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
                );

            if (!long.TryParse(customerIdValue, out var customerId))
            {
                return new List<long>();
            }

            return await _vendorCustomerAssociationRepository
                .Query()
                .Where(x => x.CustomerId == customerId && x.IsActive && !x.IsDeleted)
                .Select(x => x.VendorId)
                .Distinct()
                .ToListAsync();
        }

        private decimal CalculateDiscountPercentage(decimal? originalPrice, decimal? discountPrice)
        {
            if (originalPrice == null || originalPrice == 0 || discountPrice == null)
            {
                return 0;
            }

            return Math.Round(
                ((originalPrice.Value - discountPrice.Value) / originalPrice.Value) * 100,
                2
            );
        }

        #endregion
    }

    // Request models
    public class ProductSearchRequest
    {
        public string SearchTerm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinVendors { get; set; }
    }
}
