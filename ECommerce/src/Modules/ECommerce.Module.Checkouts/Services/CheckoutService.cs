using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Catalog.Services;
using ECommerce.Module.Checkouts.Areas.Checkouts.ViewModels;
using ECommerce.Module.Checkouts.Models;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Core.ViewModels;
using ECommerce.Module.Pricing.Services;
using ECommerce.Module.ShippingPrices.Services;
using ECommerce.Module.Tax.Services;

namespace ECommerce.Module.Checkouts.Services
{
    public class CheckoutService : ICheckoutService
    {
        private readonly IRepositoryWithTypedId<Checkout, Guid> _checkoutRepository;
        private readonly IRepository<UserAddress> _userAddressRepository;
        private readonly IShippingPriceService _shippingPriceService;
        private readonly IRepository<CheckoutItem> _checkoutItemRepository;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly IMediaService _mediaService;
        private readonly ICouponService _couponService;
        private readonly bool _isProductPriceIncludeTax;
        private readonly IProductPricingService _productPriceService;
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IRepository<VendorCustomerAssociation> _vendorCustomerAssociationRepository;
        private readonly IRepository<Product> _productRepository;

        public CheckoutService(
            IRepositoryWithTypedId<Checkout, Guid> checkoutRepository,
            IRepository<UserAddress> userAddressRepository,
            IShippingPriceService shippingPriceService,
            IRepository<CheckoutItem> checkoutItemRepository,
            ITaxService taxService,
            ICurrencyService currencyService,
            IMediaService mediaService,
            ICouponService couponService,
            IConfiguration config,
            IProductPricingService productPriceService,
            IRepository<VendorProduct> vendorProductRepository,
            IRepository<VendorCustomerAssociation> vendorCustomerAssociationRepository,
            IRepository<Product> productRepository)
        {
            _checkoutRepository = checkoutRepository;
            _userAddressRepository = userAddressRepository;
            _shippingPriceService = shippingPriceService;
            _checkoutItemRepository = checkoutItemRepository;
            _taxService = taxService;
            _currencyService = currencyService;
            _mediaService = mediaService;
            _couponService = couponService;
            _isProductPriceIncludeTax = config.GetValue<bool>("Catalog.IsProductPriceIncludeTax");
            _productPriceService = productPriceService;
            _vendorProductRepository = vendorProductRepository;
            _vendorCustomerAssociationRepository = vendorCustomerAssociationRepository;
            _productRepository = productRepository;
        }

        public async Task<Checkout> Create(long customerId, long createdById, IList<CartItemToCheckoutVm> cartItems, string couponCode)
        {
            var checkout = new Checkout
            {
                CustomerId = customerId,
                CreatedById = createdById,
                IsProductPriceIncludeTax = _isProductPriceIncludeTax,
                CouponCode = couponCode
            };
            
            foreach(var cartItem in cartItems)
            {
                var vendorProduct = ResolveVendorProduct(customerId, cartItem.ProductId, cartItem.VendorProductId, cartItem.VendorId);
                var vendorVariation = GetVendorProductVariation(vendorProduct, cartItem.ProductId, cartItem.VendorProductVariationId);
                var checkOutItem = new CheckoutItem
                {
                    Checkout = checkout,
                    ProductId = cartItem.ProductId,
                    VendorId = vendorProduct?.VendorId ?? cartItem.VendorId,
                    VendorProductId = vendorProduct?.Id ?? cartItem.VendorProductId,
                    VendorProductVariationId = vendorVariation?.Id ?? cartItem.VendorProductVariationId,
                    Quantity = cartItem.Quantity,
                    CreatedOn = DateTimeOffset.Now
                };

                checkout.CheckoutItems.Add(checkOutItem);
            }

            _checkoutRepository.Add(checkout);
            await _checkoutRepository.SaveChangesAsync();
            return checkout;
        }

        public async Task<CheckoutTaxAndShippingPriceVm> UpdateTaxAndShippingPrices(Guid checkoutId, TaxAndShippingPriceRequestVm model)
        {
            var checkout = await _checkoutRepository.Query().FirstOrDefaultAsync(x => x.Id == checkoutId);
            if (checkout == null)
            {
                throw new ApplicationException($"Checkout id {checkoutId} not found");
            }

            Address address = null;
            if (model.ExistingShippingAddressId != 0)
            {
                address = await _userAddressRepository.Query()
                    .Where(x => x.Id == model.ExistingShippingAddressId)
                    .Select(x => x.Address)
                    .FirstOrDefaultAsync();

                if (address == null)
                {
                    throw new ApplicationException($"Address id {model.ExistingShippingAddressId} not found");
                }
            }
            else if (model.NewShippingAddress != null)
            {
                address = new Address
                {
                    CountryId = model.NewShippingAddress.CountryId,
                    StateOrProvinceId = model.NewShippingAddress.StateOrProvinceId,
                    DistrictId = model.NewShippingAddress.DistrictId,
                    ZipCode = model.NewShippingAddress.ZipCode,
                    AddressLine1 = model.NewShippingAddress.AddressLine1,
                };
            }

            if (address == null)
            {
                throw new ApplicationException("A valid shipping address must be provided");
            }

            var orderTaxAndShippingPrice = new CheckoutTaxAndShippingPriceVm
            {
                IsProductPriceIncludedTax = checkout.IsProductPriceIncludeTax,
                CheckoutVm = await GetCheckoutDetails(checkoutId)
            };

            checkout.TaxAmount = orderTaxAndShippingPrice.CheckoutVm.TaxAmount = await GetTax(checkoutId, address.CountryId, address.StateOrProvinceId, address.ZipCode);

            var request = new GetShippingPriceRequest
            {
                OrderAmount = orderTaxAndShippingPrice.CheckoutVm.OrderTotal,
                ShippingAddress = address
            };

            orderTaxAndShippingPrice.ShippingPrices = await _shippingPriceService.GetApplicableShippingPrices(request);
            
            var selectedShippingMethod = string.IsNullOrWhiteSpace(model.SelectedShippingMethodName)
                ? orderTaxAndShippingPrice.ShippingPrices.FirstOrDefault()
                : orderTaxAndShippingPrice.ShippingPrices.FirstOrDefault(x => x.Name == model.SelectedShippingMethodName);
            
            if (selectedShippingMethod != null)
            {
                checkout.ShippingAmount = orderTaxAndShippingPrice.CheckoutVm.ShippingAmount = selectedShippingMethod.Price;
                checkout.ShippingMethod = orderTaxAndShippingPrice.SelectedShippingMethodName = selectedShippingMethod.Name;
            }

            await _checkoutRepository.SaveChangesAsync();
            return orderTaxAndShippingPrice;
        }

        public async Task<decimal> GetTax(Guid checkoutId, string countryId, long stateOrProvinceId, string zipCode)
        {
            decimal taxAmount = 0;

            var checkoutItems = _checkoutItemRepository.Query()
                .Where(x => x.CheckoutId == checkoutId)
                .Include(x => x.Product).ThenInclude(x => x.TaxClass)
                .ToList()
                .Select(x =>
                {
                    var vendorProduct = GetVendorProduct(x);
                    var vendorVariation = GetVendorProductVariation(vendorProduct, x.ProductId, x.VendorProductVariationId);

                    return new CheckoutItemWithTaxVm
                    {
                        Quantity = x.Quantity,
                        Price = GetTaxableProductPrice(x),
                        TaxClassId = x.Product.TaxClass?.Id,
                        TaxPercent = vendorVariation?.TaxRate ?? (RequiresVendorVariation(vendorProduct) ? null : vendorProduct?.TaxRate),
                        TaxAmount = vendorVariation != null
                            ? VendorProductPricing.GetTaxAmount(vendorVariation, x.Product.Price)
                            : vendorProduct != null && !RequiresVendorVariation(vendorProduct)
                            ? VendorProductPricing.GetTaxAmount(vendorProduct, x.Product.Price)
                            : (decimal?)null
                    };
                }).ToList();

            foreach (var item in checkoutItems)
            {
                if (item.TaxAmount.HasValue)
                {
                    taxAmount = taxAmount + item.Quantity * item.TaxAmount.Value;
                }
                else if (item.TaxPercent.HasValue)
                {
                    taxAmount = taxAmount + item.Quantity * item.Price * item.TaxPercent.Value / 100;
                }
                else if (item.TaxClassId.HasValue)
                {
                    var taxRate = await _taxService.GetTaxPercent(item.TaxClassId, countryId, stateOrProvinceId, zipCode);
                    taxAmount = taxAmount + item.Quantity * item.Price * taxRate / 100;
                }
            }

            return taxAmount;
        }

        public async Task<CheckoutVm> GetCheckoutDetails(Guid checkoutId)
        {
            var checkout = await _checkoutRepository.Query().FirstOrDefaultAsync(x => x.Id == checkoutId);

            var checkoutVm = new CheckoutVm(_currencyService)
            {
                Id = checkout.Id,
                CouponCode = checkout.CouponCode,
                IsProductPriceIncludeTax = checkout.IsProductPriceIncludeTax,
                
                TaxAmount = checkout.TaxAmount,
                ShippingAmount = checkout.ShippingAmount,
                OrderNote = checkout.OrderNote
            };

            checkoutVm.Items = _checkoutItemRepository
                .Query()
                .Include(x => x.Product).ThenInclude(p => p.ThumbnailImage)
                .Include(x => x.Product).ThenInclude(p => p.OptionCombinations).ThenInclude(o => o.Option)
                .Where(x => x.CheckoutId == checkout.Id).ToList()
                .Select(x => new CheckoutItemVm(_currencyService)
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    VendorId = x.VendorId,
                    VendorProductId = x.VendorProductId,
                    VendorProductVariationId = x.VendorProductVariationId,
                    VendorAssignment = GetVendorAssignment(x),
                    ProductName = x.Product.Name,
                    VariationName = GetVariationName(x),
                    CalculatedProductPrice = CalculateProductPrice(x),
                    ProductPrice = GetProductPrice(x),
                    ProductStockQuantity = GetStockQuantity(x),
                    ProductStockTrackingIsEnabled = x.Product.StockTrackingIsEnabled,
                    IsProductAvailabeToOrder = x.Product.IsAllowToOrder && x.Product.IsPublished && !x.Product.IsDeleted,
                    ProductImage = _mediaService.GetThumbnailUrl(x.Product.ThumbnailImage),
                    Quantity = x.Quantity,
                    VariationOptions = CheckoutItemVm.GetVariationOption(x.Product)
                }).ToList();

            checkoutVm.HasVendorAssignments = checkoutVm.Items.Any(x => x.VendorAssignment != null);
            checkoutVm.SubTotal = checkoutVm.Items.Sum(x => x.Quantity * (x.CalculatedProductPrice.OldPrice ?? x.ProductPrice));
            if (!string.IsNullOrWhiteSpace(checkoutVm.CouponCode))
            {
                var cartInfoForCoupon = new CartInfoForCoupon
                {
                    Items = checkoutVm.Items.Select(x => new CartItemForCoupon
                    {
                        ProductId = x.ProductId,
                        VendorId = x.VendorId,
                        VendorProductId = x.VendorProductId,
                        VendorProductVariationId = x.VendorProductVariationId,
                        Price = x.ProductPrice,
                        Quantity = x.Quantity
                    }).ToList()
                };
                var couponValidationResult = await _couponService.Validate(checkout.CustomerId, checkout.CouponCode, cartInfoForCoupon);
                if (couponValidationResult.Succeeded)
                {
                    checkoutVm.Discount = couponValidationResult.DiscountAmount;
                }
                else
                {
                    checkoutVm.CouponValidationErrorMessage = couponValidationResult.ErrorMessage;
                }
            }

            checkoutVm.Discount += checkoutVm.Items
                .Where(x => x.CalculatedProductPrice.OldPrice.HasValue)
                .Sum(x => x.Quantity * (x.CalculatedProductPrice.OldPrice.Value - x.CalculatedProductPrice.Price)); 

            return checkoutVm;
        }

        private VendorProduct GetVendorProduct(CheckoutItem checkoutItem)
        {
            if (checkoutItem.VendorProductId.HasValue)
            {
                return _vendorProductRepository.Query()
                    .Include(x => x.Variations).ThenInclude(x => x.OptionCombinations)
                    .FirstOrDefault(x => x.Id == checkoutItem.VendorProductId.Value && x.IsActive);
            }

            if (checkoutItem.VendorId.HasValue)
            {
                var assignedProductIds = GetAssignedProductIds(checkoutItem.ProductId);
                return _vendorProductRepository.Query()
                    .Include(x => x.Variations).ThenInclude(x => x.OptionCombinations)
                    .FirstOrDefault(x => x.VendorId == checkoutItem.VendorId.Value && assignedProductIds.Contains(x.ProductId) && x.IsActive);
            }

            return null;
        }

        private VendorProduct ResolveVendorProduct(long customerId, long productId, long? vendorProductId, long? vendorId)
        {
            var assignedProductIds = GetAssignedProductIds(productId);
            var query = _vendorProductRepository.Query()
                .Include(x => x.Variations).ThenInclude(x => x.OptionCombinations)
                .Where(x => assignedProductIds.Contains(x.ProductId) && x.IsActive);

            if (vendorProductId.HasValue)
            {
                var vendorProduct = query.FirstOrDefault(x => x.Id == vendorProductId.Value);
                return vendorProduct != null && IsVendorAssociated(customerId, vendorProduct.VendorId)
                    ? vendorProduct
                    : null;
            }

            if (vendorId.HasValue)
            {
                if (!IsVendorAssociated(customerId, vendorId.Value))
                {
                    return null;
                }

                return query.FirstOrDefault(x => x.VendorId == vendorId.Value);
            }

            var associatedVendorIds = _vendorCustomerAssociationRepository.Query()
                .Where(x => x.CustomerId == customerId && x.IsActive && !x.IsDeleted)
                .Select(x => x.VendorId)
                .ToList();

            if (!associatedVendorIds.Any())
            {
                return null;
            }

            var productPrice = _productRepository.Query()
                .Where(x => x.Id == productId)
                .Select(x => x.Price)
                .FirstOrDefault();

            var product = GetProductWithOptions(productId);

            return query
                .Where(x => associatedVendorIds.Contains(x.VendorId) && x.StockQuantity > 0)
                .ToList()
                .Where(x => GetResolvedStockQuantity(x, MatchVendorProductVariation(x, product)) > 0)
                .OrderBy(x => GetProductPrice(x, MatchVendorProductVariation(x, product), productPrice))
                .FirstOrDefault();
        }

        private bool IsVendorAssociated(long customerId, long vendorId)
        {
            return _vendorCustomerAssociationRepository.Query()
                .Any(x => x.CustomerId == customerId && x.VendorId == vendorId && x.IsActive && !x.IsDeleted);
        }

        private decimal GetProductPrice(CheckoutItem checkoutItem)
        {
            var vendorProduct = GetVendorProduct(checkoutItem);
            return GetProductPrice(vendorProduct, GetVendorProductVariation(vendorProduct, checkoutItem.ProductId, checkoutItem.VendorProductVariationId), checkoutItem.Product.Price);
        }

        private int GetStockQuantity(CheckoutItem checkoutItem)
        {
            var vendorProduct = GetVendorProduct(checkoutItem);
            var vendorVariation = GetVendorProductVariation(vendorProduct, checkoutItem.ProductId, checkoutItem.VendorProductVariationId);
            return vendorVariation?.StockQuantity
                ?? (RequiresVendorVariation(vendorProduct) ? 0 : vendorProduct?.StockQuantity)
                ?? checkoutItem.Product.StockQuantity;
        }

        private CalculatedProductPrice CalculateProductPrice(CheckoutItem checkoutItem)
        {
            var vendorProduct = GetVendorProduct(checkoutItem);
            if (vendorProduct == null)
            {
                return _productPriceService.CalculateProductPrice(checkoutItem.Product);
            }

            var vendorVariation = GetVendorProductVariation(vendorProduct, checkoutItem.ProductId, checkoutItem.VendorProductVariationId);
            if (RequiresVendorVariation(vendorProduct) && vendorVariation == null)
            {
                return _productPriceService.CalculateProductPrice(checkoutItem.Product);
            }

            return _productPriceService.CalculateProductPrice(
                GetProductPrice(vendorProduct, vendorVariation, checkoutItem.Product.Price),
                vendorVariation != null ? VendorProductPricing.GetVendorOldPrice(vendorVariation) : VendorProductPricing.GetVendorOldPrice(vendorProduct),
                null,
                null,
                null);
        }

        private VendorProductAssignmentPricingVm GetVendorAssignment(CheckoutItem checkoutItem)
        {
            var vendorProduct = GetVendorProduct(checkoutItem);
            var vendorVariation = GetVendorProductVariation(vendorProduct, checkoutItem.ProductId, checkoutItem.VendorProductVariationId);
            if (RequiresVendorVariation(vendorProduct) && vendorVariation == null)
            {
                return null;
            }

            return VendorProductAssignmentPricingVm.From(vendorProduct, vendorVariation, checkoutItem.Product.Price);
        }

        private string GetVariationName(CheckoutItem checkoutItem)
        {
            var vendorProduct = GetVendorProduct(checkoutItem);
            var vendorVariation = GetVendorProductVariation(vendorProduct, checkoutItem.ProductId, checkoutItem.VendorProductVariationId);
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

        private IList<long> GetAssignedProductIds(long productId)
        {
            var product = _productRepository.Query()
                .Include(x => x.LinkedProductLinks)
                .FirstOrDefault(x => x.Id == productId);

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

            var product = GetProductWithOptions(productId);
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

        private Product GetProductWithOptions(long productId)
        {
            return _productRepository.Query()
                .Include(x => x.OptionCombinations)
                .FirstOrDefault(x => x.Id == productId);
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

        private decimal GetSubtotalProductPrice(CheckoutItem checkoutItem)
        {
            var calculatedProductPrice = CalculateProductPrice(checkoutItem);
            return calculatedProductPrice.OldPrice ?? calculatedProductPrice.Price;
        }

        private decimal GetTaxableProductPrice(CheckoutItem checkoutItem)
        {
            return CalculateProductPrice(checkoutItem).Price;
        }
    }
}
