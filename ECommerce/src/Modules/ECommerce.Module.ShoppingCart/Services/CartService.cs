using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.ShoppingCart.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Core.ViewModels;
using ECommerce.Module.Pricing.Services;
using ECommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels;
using Microsoft.Extensions.Localization;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Catalog.Services;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.ShoppingCart.Services
{
    public class CartService : ICartService
    {
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly IMediaService _mediaService;
        private readonly ICouponService _couponService;
        private readonly bool _isProductPriceIncludeTax;
        private readonly ICurrencyService _currencyService;
        private readonly IStringLocalizer _localizer;
        private readonly IProductPricingService _productPricingService;
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IRepository<VendorCustomerAssociation> _vendorCustomerAssociationRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Vendor> _vendorRepository;


        public CartService(IRepository<CartItem> cartItemRepository, ICouponService couponService,
            IMediaService mediaService, IConfiguration config, ICurrencyService currencyService, IStringLocalizerFactory stringLocalizerFactory, IProductPricingService productPricingService,
            IRepository<VendorProduct> vendorProductRepository,
            IRepository<VendorCustomerAssociation> vendorCustomerAssociationRepository,
            IRepository<Product> productRepository, IRepository<Vendor> vendorRepository)
        {
            _cartItemRepository = cartItemRepository;
            _couponService = couponService;
            _mediaService = mediaService;
            _currencyService = currencyService;
            _isProductPriceIncludeTax = config.GetValue<bool>("Catalog.IsProductPriceIncludeTax");
            _localizer = stringLocalizerFactory.Create(null);
            _productPricingService = productPricingService;
            _vendorProductRepository = vendorProductRepository;
            _vendorCustomerAssociationRepository = vendorCustomerAssociationRepository;
            _productRepository = productRepository;
            _vendorRepository = vendorRepository;
        }

        public async Task<AddToCartResult> AddToCart(long customerId, long productId, int quantity, long? vendorProductId = null, long? vendorId = null, string variationName = null, long? vendorProductVariationId = null)
        {
            var addToCartResult = new AddToCartResult { Success = false };

            if (quantity <= 0)
            {
                addToCartResult.ErrorMessage = _localizer["The quantity must be larger than zero"].Value;
                addToCartResult.ErrorCode = "wrong-quantity";
                return addToCartResult;
            }

            if (quantity > int.MaxValue)
            {
                return CreateQuantityOverflowError();
            }

            var resolvedProductId = await ResolveProductId(productId, variationName);
            var vendorProduct = await ResolveVendorProduct(customerId, resolvedProductId, vendorProductId, vendorId);
            if (vendorProduct == null)
            {
                addToCartResult.ErrorMessage = _localizer["No active vendor association is available for this product."].Value;
                addToCartResult.ErrorCode = "vendor-product-unavailable";
                return addToCartResult;
            }

            var vendorVariation = await ResolveVendorProductVariation(vendorProduct, resolvedProductId, vendorProductVariationId);
            if (RequiresVendorVariation(vendorProduct) && vendorVariation == null)
            {
                addToCartResult.ErrorMessage = _localizer["Please select a product variation."].Value;
                addToCartResult.ErrorCode = "vendor-product-variation-required";
                return addToCartResult;
            }

            var availableStock = vendorVariation?.StockQuantity ?? vendorProduct.StockQuantity;
            var stockProductName = vendorVariation?.Name ?? vendorProduct.ProductName;

            if (availableStock < quantity)
            {
                addToCartResult.ErrorMessage = _localizer["There are only {0} items available for {1}.", availableStock, stockProductName].Value;
                addToCartResult.ErrorCode = "insufficient-stock";
                return addToCartResult;
            }

            var resolvedVendorId = vendorProduct?.VendorId ?? vendorId;
            var resolvedVendorProductId = vendorProduct?.Id ?? vendorProductId;
            var resolvedVendorProductVariationId = vendorVariation?.Id ?? vendorProductVariationId;
            var cartItem = await _cartItemRepository.Query().FirstOrDefaultAsync(x =>
                x.ProductId == resolvedProductId
                && x.CustomerId == customerId
                && x.VendorId == resolvedVendorId
                && x.VendorProductId == resolvedVendorProductId
                && x.VendorProductVariationId == resolvedVendorProductVariationId);

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    CustomerId = customerId,
                    ProductId = resolvedProductId,
                    Quantity = quantity,
                    VendorId = resolvedVendorId,
                    VendorProductId = resolvedVendorProductId,
                    VendorProductVariationId = resolvedVendorProductVariationId,
                    CreatedOn = DateTimeOffset.Now
                };

                _cartItemRepository.Add(cartItem);
            }
            else
            {
                if (cartItem.Quantity + quantity > int.MaxValue)
                {
                    return CreateQuantityOverflowError();
                }

                if (availableStock < cartItem.Quantity + quantity)
                {
                    addToCartResult.ErrorMessage = _localizer["There are only {0} items available for {1}.", availableStock, stockProductName].Value;
                    addToCartResult.ErrorCode = "insufficient-stock";
                    return addToCartResult;
                }

                cartItem.Quantity = cartItem.Quantity + quantity;
                cartItem.LatestUpdatedOn = DateTimeOffset.Now;
            }

            await _cartItemRepository.SaveChangesAsync();

            addToCartResult.Success = true;
            addToCartResult.ProductId = resolvedProductId;

            return addToCartResult;

            AddToCartResult CreateQuantityOverflowError()
            {
                addToCartResult.ErrorMessage = _localizer["The quantity must be larger than zero"].Value;
                addToCartResult.ErrorCode = "wrong-quantity";

                return addToCartResult;
            }
        }

        // TODO separate getting product thumbnail, varation options from here
        public async Task<CartVm> GetCartDetails(long customerId)
        {
            var cartItems = await _cartItemRepository.Query().Where(x => x.CustomerId == customerId).ToListAsync();
            if (!cartItems.Any())
            {
                return null;
            }

            var cartVm = new CartVm(_currencyService);

            cartVm.Items = _cartItemRepository
                .Query()
                .Include(x => x.Product).ThenInclude(p => p.ThumbnailImage)
                .Include(x => x.Product).ThenInclude(p => p.OptionCombinations).ThenInclude(o => o.Option)
                .Where(x => x.CustomerId == customerId).ToList()
                .Select(x => new CartItemVm(_currencyService)
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    VendorId = x.VendorId,
                    VendorName = GetVendorDetails(x.VendorId),
                    VendorProductId = x.VendorProductId,
                    VendorProductVariationId = x.VendorProductVariationId,
                    VendorAssignment = GetVendorAssignment(x),
                    ProductName = x.Product.Name,
                    VariationName = GetVariationName(x),
                    ProductPrice = GetProductPrice(x),
                    CalculatedProductPrice = CalculateProductPrice(x),
                    ProductStockQuantity = GetStockQuantity(x),
                    ProductStockTrackingIsEnabled = x.Product.StockTrackingIsEnabled,
                    IsProductAvailabeToOrder = x.Product.IsAllowToOrder && x.Product.IsPublished && !x.Product.IsDeleted,
                    ProductImage = _mediaService.GetThumbnailUrl(x.Product.ThumbnailImage),
                    Quantity = x.Quantity,
                    VariationOptions = CartItemVm.GetVariationOption(x.Product)
                }).ToList();

            cartVm.SubTotal = cartVm.Items.Sum(x => x.Quantity * (x.CalculatedProductPrice.OldPrice ?? x.ProductPrice));
            if (!string.IsNullOrWhiteSpace(cartVm.CouponCode))
            {
                var cartInfoForCoupon = new CartInfoForCoupon
                {
                    Items = cartVm.Items.Select(x => new CartItemForCoupon
                    {
                        ProductId = x.ProductId,
                        VendorId = x.VendorId,
                        VendorProductId = x.VendorProductId,
                        VendorProductVariationId = x.VendorProductVariationId,
                        Price = x.ProductPrice,
                        Quantity = x.Quantity
                    }).ToList()
                };
                var couponValidationResult = await _couponService.Validate(customerId, cartVm.CouponCode, cartInfoForCoupon);
                if (couponValidationResult.Succeeded)
                {
                    cartVm.Discount = couponValidationResult.DiscountAmount;
                }
                else
                {
                    cartVm.CouponValidationErrorMessage = couponValidationResult.ErrorMessage;
                }
            }

            cartVm.Discount += cartVm.Items
                .Where(x => x.CalculatedProductPrice.OldPrice.HasValue)
                .Sum(x => x.Quantity * (x.CalculatedProductPrice.OldPrice.Value - x.CalculatedProductPrice.Price));

            return cartVm;
        }

        public async Task<CouponValidationResult> ApplyCoupon(long customerId, string couponCode)
        {
            var cartItems = await _cartItemRepository.Query()
                .Include(x => x.Product)
                .Where(x => x.CustomerId == customerId)
                .ToListAsync();

            var cartInfoForCoupon = new CartInfoForCoupon
            {
                Items = cartItems.Select(x =>
                {
                    var vendorProduct = GetVendorProduct(x);

                    return new CartItemForCoupon
                    {
                        ProductId = x.ProductId,
                        VendorId = x.VendorId,
                        VendorProductId = x.VendorProductId,
                        VendorProductVariationId = x.VendorProductVariationId,
                        Price = vendorProduct != null
                            ? GetProductPrice(vendorProduct, GetVendorProductVariation(vendorProduct, x.ProductId, x.VendorProductVariationId), x.Product?.Price ?? 0)
                            : (decimal?)null,
                        Quantity = x.Quantity
                    };
                }).ToList()
            };
            var couponValidationResult = await _couponService.Validate(customerId, couponCode, cartInfoForCoupon);

            return couponValidationResult;
        }

        public async Task MigrateCart(long fromUserId, long toUserId)
        {
            var cartItemsFrom = await _cartItemRepository.Query().Where(x => x.CustomerId == fromUserId).ToListAsync();
            var carItemsTo = await _cartItemRepository.Query().Where(x => x.CustomerId == toUserId).ToListAsync();
            foreach (var cartItem in cartItemsFrom)
            {
                var existingCartItem = carItemsTo.FirstOrDefault(x =>
                    x.ProductId == cartItem.ProductId
                    && x.VendorId == cartItem.VendorId
                    && x.VendorProductId == cartItem.VendorProductId
                    && x.VendorProductVariationId == cartItem.VendorProductVariationId);
                if (existingCartItem == null)
                {
                    existingCartItem = new CartItem
                    {
                        CustomerId = toUserId,
                        ProductId = cartItem.ProductId,
                        VendorId = cartItem.VendorId,
                        VendorProductId = cartItem.VendorProductId,
                        VendorProductVariationId = cartItem.VendorProductVariationId,
                        Quantity = cartItem.Quantity,
                        CreatedOn = DateTimeOffset.Now
                    };
                    _cartItemRepository.Add(existingCartItem);
                }
                else
                {
                    existingCartItem.Quantity = existingCartItem.Quantity + cartItem.Quantity;
                }
            }

            await _cartItemRepository.SaveChangesAsync();
        }

        private async Task<VendorProduct> ResolveVendorProduct(long customerId, long productId, long? vendorProductId, long? vendorId)
        {
            var assignedProductIds = await GetAssignedProductIds(productId);
            var query = _vendorProductRepository.Query()
                .Include(x => x.Variations).ThenInclude(x => x.OptionCombinations)
                .Where(x => assignedProductIds.Contains(x.ProductId) && x.IsActive);

            if (vendorProductId.HasValue)
            {
                var vendorProduct = await query.FirstOrDefaultAsync(x => x.Id == vendorProductId.Value);
                return vendorProduct != null && await IsVendorAssociated(customerId, vendorProduct.VendorId)
                    ? vendorProduct
                    : null;
            }

            if (vendorId.HasValue)
            {
                if (!await IsVendorAssociated(customerId, vendorId.Value))
                {
                    return null;
                }

                return await query.FirstOrDefaultAsync(x => x.VendorId == vendorId.Value);
            }

            var associatedVendorIds = await _vendorCustomerAssociationRepository.Query()
                .Where(x => x.CustomerId == customerId && x.IsActive && !x.IsDeleted)
                .Select(x => x.VendorId)
                .ToListAsync();

            if (!associatedVendorIds.Any())
            {
                return null;
            }

            var productPrice = await _productRepository.Query()
                .Where(x => x.Id == productId)
                .Select(x => x.Price)
                .FirstOrDefaultAsync();

            var product = await GetProductWithOptions(productId);

            return query
                .Where(x => associatedVendorIds.Contains(x.VendorId) && x.StockQuantity > 0)
                .ToList()
                .Where(x => GetResolvedStockQuantity(x, MatchVendorProductVariation(x, product)) > 0)
                .OrderBy(x => GetProductPrice(x, MatchVendorProductVariation(x, product), productPrice))
                .FirstOrDefault();
        }

        private async Task<bool> IsVendorAssociated(long customerId, long vendorId)
        {
            return await _vendorCustomerAssociationRepository.Query()
                .AnyAsync(x => x.CustomerId == customerId && x.VendorId == vendorId && x.IsActive && !x.IsDeleted);
        }

        private VendorProduct GetVendorProduct(CartItem cartItem)
        {
            if (cartItem.VendorProductId.HasValue)
            {
                return _vendorProductRepository.Query()
                    .Include(x => x.Variations).ThenInclude(x => x.OptionCombinations)
                    .FirstOrDefault(x => x.Id == cartItem.VendorProductId.Value && x.IsActive);
            }

            if (cartItem.VendorId.HasValue)
            {
                var assignedProductIds = GetAssignedProductIds(cartItem.ProductId).GetAwaiter().GetResult();
                return _vendorProductRepository.Query()
                    .Include(x => x.Variations).ThenInclude(x => x.OptionCombinations)
                    .FirstOrDefault(x => x.VendorId == cartItem.VendorId.Value && assignedProductIds.Contains(x.ProductId) && x.IsActive);
            }

            return null;
        }

        private decimal GetProductPrice(CartItem cartItem)
        {
            var vendorProduct = GetVendorProduct(cartItem);
            return GetProductPrice(vendorProduct, GetVendorProductVariation(vendorProduct, cartItem.ProductId, cartItem.VendorProductVariationId), cartItem.Product.Price);
        }

        private int GetStockQuantity(CartItem cartItem)
        {
            var vendorProduct = GetVendorProduct(cartItem);
            var vendorVariation = GetVendorProductVariation(vendorProduct, cartItem.ProductId, cartItem.VendorProductVariationId);
            return vendorVariation?.StockQuantity
                ?? (RequiresVendorVariation(vendorProduct) ? 0 : vendorProduct?.StockQuantity)
                ?? cartItem.Product.StockQuantity;
        }

        private CalculatedProductPrice CalculateProductPrice(CartItem cartItem)
        {
            var vendorProduct = GetVendorProduct(cartItem);
            if (vendorProduct == null)
            {
                return _productPricingService.CalculateProductPrice(cartItem.Product);
            }

            var vendorVariation = GetVendorProductVariation(vendorProduct, cartItem.ProductId, cartItem.VendorProductVariationId);
            if (RequiresVendorVariation(vendorProduct) && vendorVariation == null)
            {
                return _productPricingService.CalculateProductPrice(cartItem.Product);
            }

            return _productPricingService.CalculateProductPrice(
                GetProductPrice(vendorProduct, vendorVariation, cartItem.Product.Price),
                vendorVariation != null ? VendorProductPricing.GetOldPrice(vendorVariation) : VendorProductPricing.GetOldPrice(vendorProduct),
                null,
                null,
                null);
        }

        private VendorProductAssignmentPricingVm GetVendorAssignment(CartItem cartItem)
        {
            var vendorProduct = GetVendorProduct(cartItem);
            var vendorVariation = GetVendorProductVariation(vendorProduct, cartItem.ProductId, cartItem.VendorProductVariationId);
            if (RequiresVendorVariation(vendorProduct) && vendorVariation == null)
            {
                return null;
            }

            return VendorProductAssignmentPricingVm.From(vendorProduct, vendorVariation, cartItem.Product.Price);
        }

        private string GetVariationName(CartItem cartItem)
        {
            var vendorProduct = GetVendorProduct(cartItem);
            var vendorVariation = GetVendorProductVariation(vendorProduct, cartItem.ProductId, cartItem.VendorProductVariationId);
            if (vendorVariation == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(vendorVariation.Name))
            {
                return vendorVariation.Name;
            }

            return string.Join(", ", vendorVariation.OptionCombinations
                .OrderBy(x => x.SortIndex)
                .ThenBy(x => x.OptionId)
                .Select(x => string.IsNullOrWhiteSpace(x.OptionName)
                    ? x.Value
                    : $"{x.OptionName}: {x.Value}")
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static decimal GetProductPrice(VendorProduct vendorProduct, VendorProductVariation vendorVariation, decimal fallbackPrice)
        {
            if (vendorVariation != null)
            {
                return VendorProductPricing.GetEffectivePrice(vendorVariation, fallbackPrice);
            }

            return RequiresVendorVariation(vendorProduct)
                ? fallbackPrice
                : VendorProductPricing.GetEffectivePrice(vendorProduct, fallbackPrice);
        }

        private static int GetResolvedStockQuantity(VendorProduct vendorProduct, VendorProductVariation vendorVariation)
        {
            return vendorVariation?.StockQuantity
                ?? (RequiresVendorVariation(vendorProduct) ? 0 : vendorProduct?.StockQuantity)
                ?? 0;
        }

        private static bool RequiresVendorVariation(VendorProduct vendorProduct)
        {
            return vendorProduct?.Variations?.Any(x => x.IsActive) == true;
        }

        private async Task<long> ResolveProductId(long productId, string variationName)
        {
            if (string.IsNullOrWhiteSpace(variationName))
            {
                return productId;
            }

            var normalizedVariationName = variationName.Trim().ToLowerInvariant();
            var variationProductId = await _productRepository.Query()
                .Where(x => x.NormalizedName == normalizedVariationName)
                .Where(x => x.LinkedProductLinks.Any(link => link.ProductId == productId && link.LinkType == ProductLinkType.Super))
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync();

            return variationProductId ?? productId;
        }

        private async Task<IList<long>> GetAssignedProductIds(long productId)
        {
            var product = await _productRepository.Query()
                .Include(x => x.LinkedProductLinks)
                .FirstOrDefaultAsync(x => x.Id == productId);

            var assignedProductIds = new HashSet<long> { productId };
            var parentProductId = product?.LinkedProductLinks
                .FirstOrDefault(x => x.LinkType == ProductLinkType.Super)
                ?.ProductId;

            if (parentProductId.HasValue)
            {
                assignedProductIds.Add(parentProductId.Value);
            }

            return assignedProductIds.ToList();
        }

        private async Task<VendorProductVariation> ResolveVendorProductVariation(VendorProduct vendorProduct, long productId, long? vendorProductVariationId)
        {
            var explicitVariation = GetExplicitVendorProductVariation(vendorProduct, vendorProductVariationId);
            if (explicitVariation != null)
            {
                return explicitVariation;
            }

            var product = await GetProductWithOptions(productId);
            return MatchVendorProductVariation(vendorProduct, product);
        }

        private VendorProductVariation GetVendorProductVariation(VendorProduct vendorProduct, long productId, long? vendorProductVariationId)
        {
            if (vendorProduct == null)
            {
                return null;
            }

            var explicitVariation = GetExplicitVendorProductVariation(vendorProduct, vendorProductVariationId);
            if (explicitVariation != null)
            {
                return explicitVariation;
            }

            var product = _productRepository.Query()
                .Include(x => x.OptionCombinations)
                .FirstOrDefault(x => x.Id == productId);

            return MatchVendorProductVariation(vendorProduct, product);
        }

        private static VendorProductVariation GetExplicitVendorProductVariation(VendorProduct vendorProduct, long? vendorProductVariationId)
        {
            if (vendorProduct == null || !vendorProductVariationId.HasValue)
            {
                return null;
            }

            return vendorProduct.Variations.FirstOrDefault(x =>
                x.Id == vendorProductVariationId.Value && x.IsActive);
        }

        private async Task<Product> GetProductWithOptions(long productId)
        {
            return await _productRepository.Query()
                .Include(x => x.OptionCombinations)
                .FirstOrDefaultAsync(x => x.Id == productId);
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
                    x.IsActive && string.Equals(x.NormalizedName?.Trim(), normalizedName, StringComparison.OrdinalIgnoreCase));
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

        private string GetVendorDetails(long? vendorId)
        {
            if (!vendorId.HasValue)
            {
                return null;
            }

            var vendor = _vendorRepository.Query().FirstOrDefault(x => x.Id == vendorId.Value);
            return vendor?.Name;
        }
    }
}
