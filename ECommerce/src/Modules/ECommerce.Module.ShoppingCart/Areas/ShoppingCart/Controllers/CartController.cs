using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Pricing.Services;
using ECommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels;
using ECommerce.Module.ShoppingCart.Models;
using ECommerce.Module.ShoppingCart.Services;

namespace ECommerce.Module.ShoppingCart.Areas.ShoppingCart.Controllers
{
    [Area("ShoppingCart")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly ICartService _cartService;
        private readonly IMediaService _mediaService;
        private readonly IWorkContext _workContext;
        private readonly ICurrencyService _currencyService;
        private readonly IStringLocalizer _localizer;
        private readonly IRepository<VendorProduct> _vendorProductRepository;

        public CartController(
            IRepository<CartItem> cartItemRepository,
            ICartService cartService,
            IMediaService mediaService,
            IWorkContext workContext,
            ICurrencyService currencyService,
            IStringLocalizerFactory stringLocalizerFactory,
            IRepository<VendorProduct> vendorProductRepository)
        {
            _cartItemRepository = cartItemRepository;
            _cartService = cartService;
            _mediaService = mediaService;
            _workContext = workContext;
            _currencyService = currencyService;
            _localizer = stringLocalizerFactory.Create(null);
            _vendorProductRepository = vendorProductRepository;
        }

        [HttpPost("cart/add-item")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartModel model)
        {
            var currentUser = await _workContext.GetCurrentUser();
            if (currentUser == null) return Unauthorized();
            var result = await _cartService.AddToCart(currentUser.Id, model.ProductId, model.Quantity, model.VendorProductId, model.VendorId, model.VariationName, model.VendorProductVariationId);
            if (result.Success)
            {
                return Ok(new { productId = result.ProductId ?? model.ProductId });
            }
            else
            {
                return Ok(result);
            }
        }

        [HttpGet("cart/add-item-result")]
        public async Task<IActionResult> AddToCartResult(long productId)
        {
            var currentUser = await _workContext.GetCurrentUser();
            if (currentUser == null) return Unauthorized();
            var cart = await _cartService.GetCartDetails(currentUser.Id);

            var model = new AddToCartResultVm(_currencyService)
            {
                CartItemCount = cart.Items.Count,
                CartAmount = cart.SubTotal
            };

            var addedProduct = cart.Items.First(x => x.ProductId == productId);
            model.ProductName = addedProduct.ProductName;
            model.ProductImage = addedProduct.ProductImage;
            model.ProductPrice = addedProduct.ProductPrice;
            model.CalculatedProductPrice = addedProduct.CalculatedProductPrice;
            model.VariationName = addedProduct.VariationName;
            model.VendorId = addedProduct.VendorId;
            model.VendorProductId = addedProduct.VendorProductId;
            model.VendorProductVariationId = addedProduct.VendorProductVariationId;
            model.VendorAssignment = addedProduct.VendorAssignment;
            model.Quantity = addedProduct.Quantity;

            return Ok(model);
        }

        [HttpGet("cart")]
        public IActionResult Index()
        {
            return Ok();
        }

        [HttpGet("cart/list")]
        public async Task<IActionResult> List()
        {
            var currentUser = await _workContext.GetCurrentUser();
            if (currentUser == null) return new JsonResult(new CartVm(_currencyService));
            var cart = await _cartService.GetCartDetails(currentUser.Id);
            if (cart == null)
            {
                cart = new CartVm(_currencyService);
            }

            return new JsonResult(cart);
        }

        [HttpPost("cart/update-item-quantity")]
        public async Task<IActionResult> UpdateQuantity([FromBody] CartQuantityUpdate model)
        {
            if (model.Quantity <= 0)
            {
                return Ok(new { Error = true, Message = _localizer["The quantity must be larger than zero"].Value });
            }
            var currentUser = await _workContext.GetCurrentUser();
            if (currentUser == null) return Unauthorized();

            var cartItem = _cartItemRepository.Query()
                .Include(x => x.Product).ThenInclude(x => x.OptionCombinations)
                .Include(x => x.Product).ThenInclude(x => x.LinkedProductLinks)
                .FirstOrDefault(x => x.Id == model.CartItemId && x.CustomerId == currentUser.Id);
            if (cartItem == null)
            {
                return NotFound();
            }

            if (model.Quantity > cartItem.Quantity) // always allow user to descrease the quality
            {
                var availableStock = cartItem.Product.StockQuantity;
                if (cartItem.VendorProductId.HasValue || cartItem.VendorId.HasValue)
                {
                    var vendorProductQuery = _vendorProductRepository
                        .Query()
                        .Include(x => x.Variations).ThenInclude(x => x.OptionCombinations)
                        .Where(x => x.IsActive);

                    if (cartItem.VendorProductId.HasValue)
                    {
                        vendorProductQuery = vendorProductQuery.Where(x => x.Id == cartItem.VendorProductId.Value);
                    }
                    else
                    {
                        var assignedProductIds = GetAssignedProductIds(cartItem.Product);
                        vendorProductQuery = vendorProductQuery.Where(x =>
                            x.VendorId == cartItem.VendorId.Value && assignedProductIds.Contains(x.ProductId));
                    }

                    var vendorProduct = vendorProductQuery.FirstOrDefault();
                    if (vendorProduct == null)
                    {
                        return Ok(new { Error = true, Message = _localizer["The selected vendor product is not available any more"].Value });
                    }

                    var vendorVariation = GetVendorProductVariation(vendorProduct, cartItem.Product, cartItem.VendorProductVariationId);
                    availableStock = vendorVariation?.StockQuantity
                        ?? (RequiresVendorVariation(vendorProduct) ? 0 : vendorProduct.StockQuantity);
                }

                if (cartItem.Product.StockTrackingIsEnabled && availableStock < model.Quantity)
                {
                    return Ok(new { Error = true, Message = _localizer["There are only {0} items available for {1}.", availableStock, cartItem.Product.Name].Value });
                }
            }

            cartItem.Quantity = model.Quantity;
            _cartItemRepository.SaveChanges();

            return await List();
        }

        private static VendorProductVariation GetVendorProductVariation(VendorProduct vendorProduct, Product product, long? vendorProductVariationId)
        {
            if (vendorProduct == null)
            {
                return null;
            }

            if (vendorProductVariationId.HasValue)
            {
                var explicitVariation = vendorProduct.Variations.FirstOrDefault(x =>
                    x.Id == vendorProductVariationId.Value && x.IsActive);
                if (explicitVariation != null)
                {
                    return explicitVariation;
                }
            }

            return MatchVendorProductVariation(vendorProduct, product);
        }

        private static VendorProductVariation MatchVendorProductVariation(VendorProduct vendorProduct, Product product)
        {
            if (vendorProduct == null || product == null || !product.OptionCombinations.Any())
            {
                return null;
            }

            var normalizedName = product.NormalizedName?.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedName))
            {
                var matchedByName = vendorProduct.Variations.FirstOrDefault(x =>
                    x.IsActive && string.Equals(x.NormalizedName?.Trim(), normalizedName, System.StringComparison.OrdinalIgnoreCase));
                if (matchedByName != null)
                {
                    return matchedByName;
                }
            }

            var productSignature = BuildVariationSignature(product.OptionCombinations);
            return vendorProduct.Variations.FirstOrDefault(x =>
                x.IsActive && BuildVariationSignature(x.OptionCombinations) == productSignature);
        }

        private static string BuildVariationSignature(IEnumerable<ProductOptionCombination> options)
        {
            return string.Join("|", options
                .Where(x => x != null)
                .OrderBy(x => x.OptionId)
                .Select(x => $"{x.OptionId}:{(x.Value ?? string.Empty).Trim().ToLowerInvariant()}"));
        }

        private static string BuildVariationSignature(IEnumerable<VendorProductVariationOption> options)
        {
            return string.Join("|", options
                .Where(x => x != null)
                .OrderBy(x => x.OptionId)
                .Select(x => $"{x.OptionId}:{(x.Value ?? string.Empty).Trim().ToLowerInvariant()}"));
        }

        private static bool RequiresVendorVariation(VendorProduct vendorProduct)
        {
            return vendorProduct?.Variations?.Any(x => x.IsActive) == true;
        }

        private static IList<long> GetAssignedProductIds(Product product)
        {
            var assignedProductIds = new HashSet<long>();
            if (product == null)
            {
                return assignedProductIds.ToList();
            }

            assignedProductIds.Add(product.Id);
            var parentProductId = product.LinkedProductLinks?
                .FirstOrDefault(x => x.LinkType == ProductLinkType.Super)
                ?.ProductId;

            if (parentProductId.HasValue)
            {
                assignedProductIds.Add(parentProductId.Value);
            }

            return assignedProductIds.ToList();
        }

        [HttpPost("cart/apply-coupon")]
        public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponForm model)
        {
            var currentUser = await _workContext.GetCurrentUser();
            if (currentUser == null) return Unauthorized();

            var validationResult = await _cartService.ApplyCoupon(currentUser.Id, model.CouponCode);
            if (validationResult != null && !validationResult.Succeeded)
            {
                return BadRequest(validationResult);
            }
            if (validationResult.Succeeded)
            {
                var cartVm = await _cartService.GetCartDetails(currentUser.Id);
                cartVm.Discount = validationResult.DiscountAmount;
                return new JsonResult(cartVm);
            }

            return new JsonResult(validationResult);
        }


        [HttpPost("cart/remove-item")]
        public async Task<IActionResult> Remove([FromBody] long itemId)
        {
            var currentUser = await _workContext.GetCurrentUser();
            if (currentUser == null) return Unauthorized();

            var cartItem = _cartItemRepository.Query().FirstOrDefault(x => x.Id == itemId && x.CustomerId == currentUser.Id);
            if (cartItem == null)
            {
                return NotFound();
            }

            _cartItemRepository.Remove(cartItem);
            _cartItemRepository.SaveChanges();

            return await List();
        }
    }
}
