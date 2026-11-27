using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.ShoppingCart.Models;

namespace ECommerce.Module.ShoppingCart.Areas.ShoppingCart.Controllers
{
    /// <summary>
    /// Provides vendor-aware shopping cart operations
    /// Allows customers to manage vendors for items and optimize pricing
    /// </summary>
    [Area("ShoppingCart")]
    [ApiController]
    [Route("api/cart/vendor")]
    [Authorize]
    public class CartVendorEnhancementController : ControllerBase
    {
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IWorkContext _workContext;

        public CartVendorEnhancementController(
            IRepository<CartItem> cartItemRepository,
            IRepository<VendorProduct> vendorProductRepository,
            IWorkContext workContext)
        {
            _cartItemRepository = cartItemRepository;
            _vendorProductRepository = vendorProductRepository;
            _workContext = workContext;
        }

        /// <summary>
        /// Add item to cart with specific vendor selection
        /// </summary>
        [HttpPost("add-item-with-vendor")]
        public async Task<IActionResult> AddItemWithVendor([FromBody] AddCartItemVendorRequest request)
        {
            if (request == null || request.VendorProductId <= 0 || request.Quantity <= 0)
            {
                return BadRequest(new { message = "Invalid vendor product ID or quantity" });
            }

            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var vendorProduct = await _vendorProductRepository.Query()
                    .Include(x => x.Vendor)
                    .FirstOrDefaultAsync(x => x.Id == request.VendorProductId && x.IsActive);

                if (vendorProduct == null)
                {
                    return NotFound(new { message = "Vendor product not found" });
                }

                if (vendorProduct.StockQuantity < request.Quantity)
                {
                    return BadRequest(new { message = "Insufficient stock" });
                }

                // TODO: Implement cart item creation with vendor reference
                var cartItem = new CartItem
                {
                    CustomerId = currentUser.Id,
                    ProductId = vendorProduct.ProductId,
                    Quantity = request.Quantity,
                    VendorId = vendorProduct.VendorId,
                    VendorProductId = vendorProduct.Id
                };

                _cartItemRepository.Add(cartItem);
                await _cartItemRepository.SaveChangesAsync();

                return Ok(new
                {
                    message = "Item added to cart",
                    cartItemId = cartItem.Id,
                    vendorName = vendorProduct.Vendor?.Name,
                    price = vendorProduct.DiscountPrice ?? vendorProduct.AssignedPrice
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error adding item to cart", error = ex.Message });
            }
        }

        /// <summary>
        /// Change vendor for an existing cart item
        /// </summary>
        [HttpPost("update-item-vendor")]
        public async Task<IActionResult> UpdateItemVendor([FromBody] UpdateCartItemVendorRequest request)
        {
            if (request == null || request.CartItemId <= 0 || request.VendorProductId <= 0)
            {
                return BadRequest(new { message = "Invalid cart item or vendor product ID" });
            }

            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var cartItem = await _cartItemRepository.Query()
                    .FirstOrDefaultAsync(x => x.Id == request.CartItemId && x.CustomerId == currentUser.Id);

                if (cartItem == null)
                {
                    return NotFound(new { message = "Cart item not found" });
                }

                var vendorProduct = await _vendorProductRepository.Query()
                    .Include(x => x.Vendor)
                    .FirstOrDefaultAsync(x => x.Id == request.VendorProductId && x.IsActive);

                if (vendorProduct == null)
                {
                    return NotFound(new { message = "Vendor product not found" });
                }

                if (vendorProduct.StockQuantity < cartItem.Quantity)
                {
                    return BadRequest(new { message = "Insufficient stock at this vendor" });
                }

                // TODO: Update cart item vendor reference
                cartItem.VendorId = vendorProduct.VendorId;
                cartItem.VendorProductId = vendorProduct.Id;
                await _cartItemRepository.SaveChangesAsync();

                return Ok(new
                {
                    message = "Vendor updated for cart item",
                    newPrice = vendorProduct.DiscountPrice ?? vendorProduct.AssignedPrice,
                    vendorName = vendorProduct.Vendor?.Name
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error updating vendor", error = ex.Message });
            }
        }

        /// <summary>
        /// Get cart with detailed vendor information
        /// </summary>
        [HttpGet("list-with-details")]
        public async Task<IActionResult> GetCartWithVendorDetails()
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var cartItems = await _cartItemRepository.Query()
                    .Include(x => x.Product)
                    .Where(x => x.CustomerId == currentUser.Id)
                    .ToListAsync();

                var cartDetails = new List<object>();

                foreach (var item in cartItems)
                {
                    var vendorProduct = await _vendorProductRepository.Query()
                        .Include(x => x.Vendor)
                        .FirstOrDefaultAsync(x =>
                            x.ProductId == item.ProductId
                            && x.IsActive
                            && (item.VendorProductId.HasValue
                                ? x.Id == item.VendorProductId.Value
                                : x.VendorId == item.VendorId));

                    if (vendorProduct != null)
                    {
                        cartDetails.Add(new
                        {
                            cartItemId = item.Id,
                            productId = item.ProductId,
                            productName = item.Product.Name,
                            vendorId = vendorProduct.VendorId,
                            vendorName = vendorProduct.Vendor?.Name,
                            quantity = item.Quantity,
                            unitPrice = vendorProduct.DiscountPrice ?? vendorProduct.AssignedPrice,
                            totalPrice = (vendorProduct.DiscountPrice ?? vendorProduct.AssignedPrice ?? 0) * item.Quantity,
                            stock = vendorProduct.StockQuantity
                        });
                    }
                }

                var cartTotal = cartDetails.Sum(x => (decimal)((dynamic)x).totalPrice);

                return Ok(new
                {
                    itemCount = cartDetails.Count,
                    items = cartDetails,
                    total = Math.Round(cartTotal, 2)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving cart details", error = ex.Message });
            }
        }

        /// <summary>
        /// Optimize cart pricing by selecting cheapest vendor for each item
        /// </summary>
        [HttpPost("optimize-pricing")]
        public async Task<IActionResult> OptimizePricing()
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var cartItems = await _cartItemRepository.Query()
                    .Where(x => x.CustomerId == currentUser.Id)
                    .ToListAsync();

                var optimizations = new List<object>();

                foreach (var item in cartItems)
                {
                    var vendorOptions = await _vendorProductRepository.Query()
                        .Where(x => x.ProductId == item.ProductId && x.IsActive && x.StockQuantity >= item.Quantity)
                        .OrderBy(x => x.DiscountPrice ?? x.AssignedPrice)
                        .FirstOrDefaultAsync();

                    if (vendorOptions != null && vendorOptions.VendorId != item.VendorId)
                    {
                        var oldPrice = (item.Product.Price) * item.Quantity;
                        var newPrice = (vendorOptions.DiscountPrice ?? vendorOptions.AssignedPrice ?? 0) * item.Quantity;
                        var savings = oldPrice - newPrice;

                        // Update vendor
                        item.VendorId = vendorOptions.VendorId;
                        item.VendorProductId = vendorOptions.Id;

                        optimizations.Add(new
                        {
                            productId = item.ProductId,
                            oldPrice = Math.Round(oldPrice, 2),
                            newPrice = Math.Round(newPrice, 2),
                            savings = Math.Round(savings, 2)
                        });
                    }
                }

                if (optimizations.Any())
                {
                    await _cartItemRepository.SaveChangesAsync();
                }

                var totalSavings = optimizations.Sum(x => (decimal)((dynamic)x).savings);

                return Ok(new
                {
                    optimizationCount = optimizations.Count,
                    totalSavings = Math.Round(totalSavings, 2),
                    optimizations = optimizations
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error optimizing cart pricing", error = ex.Message });
            }
        }
    }

    // Request models
    public class AddCartItemVendorRequest
    {
        public long VendorProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateCartItemVendorRequest
    {
        public long CartItemId { get; set; }
        public long VendorProductId { get; set; }
    }
}
