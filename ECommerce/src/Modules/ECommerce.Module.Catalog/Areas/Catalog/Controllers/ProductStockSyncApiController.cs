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
using ECommerce.Module.Core.Extensions;

namespace ECommerce.Module.Catalog.Areas.Catalog.Controllers
{
    /// <summary>
    /// Synchronizes vendor product assignments (stock, price, discount) to main product catalog
    /// 
    /// IMPORTANT: This controller is designed for SINGLE-VENDOR products only.
    /// 
    /// For MULTI-VENDOR scenarios:
    /// - Use IMultiVendorProductStrategy instead
    /// - Use MultiVendorProductController for pricing comparisons
    /// - Stock and pricing remain independent in VendorProduct table
    /// - Main Product table should NOT be updated when multiple vendors sell the product
    /// 
    /// RECOMMENDATION:
    /// - Set Product.VendorId if product is vendor-exclusive (single vendor)
    /// - Leave Product.VendorId = null for marketplace products (multiple vendors)
    /// - Use IMultiVendorProductStrategy to get aggregated data for null Product.VendorId
    /// </summary>
    [Area("Catalog")]
    [ApiController]
    [Authorize(Roles = "admin, vendor")]
    [Route("api/products/sync")]
    public class ProductStockSyncApiController : ControllerBase
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IWorkContext _workContext;

        public ProductStockSyncApiController(
            IRepository<Product> productRepository,
            IRepository<VendorProduct> vendorProductRepository,
            IWorkContext workContext)
        {
            _productRepository = productRepository;
            _vendorProductRepository = vendorProductRepository;
            _workContext = workContext;
        }

        /// <summary>
        /// Synchronizes a vendor's assigned product stock and pricing to main product
        /// Called after vendor updates product assignment
        /// </summary>
        [HttpPost("{vendorId:long}/products/{productId:long}/sync-stock")]
        public async Task<IActionResult> SyncVendorProductStock(long vendorId, long productId)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();

                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return BadRequest(new { error = "You don't have permission to sync this vendor's products" });
                }

                var vendorProduct = await _vendorProductRepository.Query()
                    .FirstOrDefaultAsync(x => x.VendorId == vendorId && x.ProductId == productId);

                if (vendorProduct == null)
                {
                    return NotFound(new { error = "Vendor product assignment not found" });
                }

                var product = await _productRepository.Query()
                    .FirstOrDefaultAsync(x => x.Id == productId);

                if (product == null)
                {
                    return NotFound(new { error = "Product not found" });
                }

                product.StockQuantity = vendorProduct.StockQuantity;
                product.Price = vendorProduct.AssignedPrice ?? product.Price;
                product.SpecialPrice = vendorProduct.DiscountPrice > 0 ? vendorProduct.DiscountPrice : product.SpecialPrice;
                product.LatestUpdatedBy = currentUser;
                product.LatestUpdatedOn = DateTimeOffset.Now;

                await _productRepository.SaveChangesAsync();

                return Ok(new
                {
                    message = "Product stock and pricing synchronized successfully",
                    vendorProductId = vendorProduct.Id,
                    stockQuantity = product.StockQuantity,
                    price = product.Price,
                    specialPrice = product.SpecialPrice
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Synchronizes all of a vendor's assigned products stock and pricing
        /// </summary>
        [HttpPost("{vendorId:long}/products/sync-all-stock")]
        public async Task<IActionResult> SyncAllVendorProductsStock(long vendorId)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();

                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return BadRequest(new { error = "You don't have permission to sync this vendor's products" });
                }

                var vendorProducts = await _vendorProductRepository.Query()
                    .Where(x => x.VendorId == vendorId)
                    .ToListAsync();

                if (!vendorProducts.Any())
                {
                    return Ok(new { message = "No vendor products to sync", syncedCount = 0 });
                }

                var products = await _productRepository.Query()
                    .Where(x => vendorProducts.Select(vp => vp.ProductId).Contains(x.Id))
                    .ToListAsync();

                foreach (var vendorProduct in vendorProducts)
                {
                    var product = products.FirstOrDefault(p => p.Id == vendorProduct.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity = vendorProduct.StockQuantity;
                        product.Price = vendorProduct.AssignedPrice ?? product.Price;
                        product.SpecialPrice = vendorProduct.DiscountPrice > 0 ? vendorProduct.DiscountPrice : product.SpecialPrice;
                        product.LatestUpdatedBy = currentUser;
                        product.LatestUpdatedOn = DateTimeOffset.Now;
                    }
                }

                await _productRepository.SaveChangesAsync();

                return Ok(new
                {
                    message = "All vendor products synchronized successfully",
                    syncedCount = vendorProducts.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets aggregated stock across all vendor assignments for a product
        /// Useful for admin dashboard to see total available stock
        /// </summary>
        [HttpGet("products/{productId:long}/aggregated-stock")]
        public async Task<IActionResult> GetAggregatedProductStock(long productId)
        {
            var product = await _productRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            var vendorAssignments = await _vendorProductRepository.Query()
                .Where(x => x.ProductId == productId && x.IsActive)
                .Select(x => new
                {
                    VendorId = x.VendorId,
                    VendorProductId = x.Id,
                    Stock = x.StockQuantity,
                    Price = x.AssignedPrice,
                    DiscountPrice = x.DiscountPrice
                })
                .ToListAsync();

            var aggregatedStock = vendorAssignments.Sum(x => x.Stock);

            return Ok(new
            {
                productId = productId,
                productName = product.Name,
                mainProductStock = product.StockQuantity,
                aggregatedVendorStock = aggregatedStock,
                vendorAssignments = vendorAssignments,
                totalAvailableStock = product.StockQuantity + aggregatedStock
            });
        }
    }
}
