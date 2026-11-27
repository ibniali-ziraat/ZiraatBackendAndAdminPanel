using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Checkouts.Areas.Checkouts.ViewModels;
using ECommerce.Module.Checkouts.Models;
using ECommerce.Module.Checkouts.Services;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Pricing.Services;
using ECommerce.Module.ShoppingCart.Models;
using ECommerce.Module.ShoppingCart.Services;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace ECommerce.Module.Checkouts.Areas.Checkouts.Controllers
{
    [Area("Checkouts")]
    [ApiController]
    [Route("checkout")]
    [Authorize]
    public class CheckoutController : ControllerBase
    {
        private readonly IRepositoryWithTypedId<Country, string> _countryRepository;
        private readonly IRepository<StateOrProvince> _stateOrProvinceRepository;
        private readonly IRepository<UserAddress> _userAddressRepository;
        private readonly ICheckoutService _checkoutService;
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly IWorkContext _workContext;
        private readonly IRepositoryWithTypedId<Checkout, Guid> _checkoutRepository;
        private readonly ICouponService _couponService;
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IRepository<VendorCustomerAssociation> _vendorCustomerAssociationRepository;
        private readonly IRepository<Product> _productRepository;

        public CheckoutController(
            IRepository<StateOrProvince> stateOrProvinceRepository,
            IRepositoryWithTypedId<Country, string> countryRepository,
            IRepository<UserAddress> userAddressRepository,
            ICheckoutService checkout,
            IRepository<CartItem> cartItemRepository,
            IWorkContext workContext,
            IRepositoryWithTypedId<Checkout, Guid> checkoutRepository,
            ICouponService couponService,
            IRepository<VendorProduct> vendorProductRepository,
            IRepository<VendorCustomerAssociation> vendorCustomerAssociationRepository,
            IRepository<Product> productRepository)
        {
            _stateOrProvinceRepository = stateOrProvinceRepository;
            _countryRepository = countryRepository;
            _userAddressRepository = userAddressRepository;
            _checkoutService = checkout;
            _cartItemRepository = cartItemRepository;
            _workContext = workContext;
            _checkoutRepository = checkoutRepository;
            _couponService = couponService;
            _vendorProductRepository = vendorProductRepository;
            _vendorCustomerAssociationRepository = vendorCustomerAssociationRepository;
            _productRepository = productRepository;
        }

        //TODO: Consider to allow customer select a subset of products in cart and pass to this endpoint
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create(CheckoutFormVm checkoutFormVm)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }
            var currentUser = await _workContext.GetCurrentUser();
            var cartItems = await _cartItemRepository.Query().Where(x => x.CustomerId == currentUser.Id).ToListAsync();
            if (!cartItems.Any())
            {
                return NotFound();
            }

            var cartVendorValidation = ValidateCartVendorAssociations(currentUser.Id, cartItems);
            if (cartVendorValidation != null)
            {
                return BadRequest(cartVendorValidation);
            }

            var cartItemToCheckouts = cartItems.Select(x => new CartItemToCheckoutVm
            {
                ProductId = x.ProductId,
                VendorId = x.VendorId,
                VendorProductId = x.VendorProductId,
                VendorProductVariationId = x.VendorProductVariationId,
                Quantity = x.Quantity
            }).ToList();

            // Validate coupon if provided
            if (!string.IsNullOrWhiteSpace(checkoutFormVm.CouponCode))
            {
                var cartInfo = new CartInfoForCoupon
                {
                    Items = CreateCouponCartItems(currentUser.Id, cartItems)
                };

                var couponValidation = await _couponService.Validate(currentUser.Id, checkoutFormVm.CouponCode, cartInfo);
                if (!couponValidation.Succeeded)
                {
                    return BadRequest(couponValidation.ErrorMessage);
                }
            }

            var checkout = await _checkoutService.Create(currentUser.Id, currentUser.Id, cartItemToCheckouts, checkoutFormVm.CouponCode);

            return Ok(new { checkoutId = checkout.Id });
        }

        /// <summary>
        /// Validate coupon during checkout before proceeding
        /// </summary>
        [HttpPost("{checkoutId}/validate-coupon")]
        public async Task<IActionResult> ValidateCouponForCheckout(Guid checkoutId, [FromBody] ValidateCouponForCheckoutVm model)
        {
            var currentUser = await _workContext.GetCurrentUser();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var checkout = await _checkoutRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == checkoutId);
            
            if (checkout == null)
            {
                return NotFound(new { message = "Checkout not found" });
            }

            if (!IsCheckoutOwner(checkout, currentUser))
            {
                return Forbid();
            }

            try
            {
                if (string.IsNullOrWhiteSpace(model.CouponCode))
                {
                    return BadRequest(new { message = "Coupon code is required" });
                }

                // Get checkout items
                var cartItems = await _cartItemRepository.Query()
                    .Where(x => x.CustomerId == currentUser.Id)
                    .ToListAsync();

                var cartInfo = new CartInfoForCoupon
                {
                    Items = CreateCouponCartItems(currentUser.Id, cartItems)
                };

                var validationResult = await _couponService.Validate(currentUser.Id, model.CouponCode, cartInfo);

                if (validationResult.Succeeded)
                {
                    return Ok(new
                    {
                        succeeded = true,
                        couponCode = validationResult.CouponCode,
                        discountAmount = validationResult.DiscountAmount,
                        message = "Coupon is valid and has been applied"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        succeeded = false,
                        message = validationResult.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error validating coupon", error = ex.Message });
            }
        }

        [HttpGet("{checkoutId}/shipping")]
        public async Task<IActionResult> Shipping(Guid checkoutId)
        {
            var currentUser = await _workContext.GetCurrentUser();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var checkout = await _checkoutRepository.Query().FirstOrDefaultAsync(x => x.Id == checkoutId);
            if (checkout == null)
            {
                return NotFound();
            }

            if (!IsCheckoutOwner(checkout, currentUser))
            {
                return Forbid();
            }

            var model = new DeliveryInformationVm();
            model.CheckoutId = checkoutId;

            //// Deserialize ShippingData if it exists
            //if (!string.IsNullOrEmpty(checkout.ShippingData))
            //{
            //    model = JsonConvert.DeserializeObject<DeliveryInformationVm>(checkout.ShippingData);
            //    model.CheckoutId = checkoutId;
            //}

            PopulateShippingForm(model, currentUser);

            return Ok(model);
        }

        [HttpPost("{checkoutId}/shipping")]
        public async Task<IActionResult> Shipping(Guid checkoutId, DeliveryInformationVm model)
        {
            var currentUser = await _workContext.GetCurrentUser();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var checkout = await _checkoutRepository.Query().FirstOrDefaultAsync(x => x.Id == checkoutId);
            if (checkout == null)
            {
                return NotFound();
            }

            if (!IsCheckoutOwner(checkout, currentUser))
            {
                return Forbid();
            }

            // Ensure model and address forms are initialized
            if (model == null)
            {
                return BadRequest("Invalid request model");
            }

            if (model.NewAddressForm == null)
            {
                model.NewAddressForm = new AddressFormVm();
            }

            if (model.NewBillingAddressForm == null)
            {
                model.NewBillingAddressForm = new AddressFormVm();
            }

            if ((!model.NewAddressForm.IsValid() && model.ShippingAddressId == 0) ||
                (!model.NewBillingAddressForm.IsValid() && !model.UseShippingAddressAsBillingAddress && model.BillingAddressId == 0))
            {
                PopulateShippingForm(model, currentUser);
                //return Ok(model);
            }

            checkout.ShippingData = JsonConvert.SerializeObject(model);
            await _checkoutRepository.SaveChangesAsync();
            return Ok(new { checkoutId = checkoutId });
        }

        [HttpPost("{checkoutId}/update-tax-and-shipping-prices")]
        public async Task<IActionResult> UpdateTaxAndShippingPrices(Guid checkoutId, [FromBody] TaxAndShippingPriceRequestVm model)
        {
            var currentUser = await _workContext.GetCurrentUser();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var checkout = await _checkoutRepository.Query().FirstOrDefaultAsync(x => x.Id == checkoutId);
            if (checkout == null)
            {
                return NotFound();
            }

            if (!IsCheckoutOwner(checkout, currentUser))
            {
                return Forbid();
            }

            var checkoutVendorValidation = ValidateCheckoutVendorAssociations(checkout.Id, currentUser.Id);
            if (checkoutVendorValidation != null)
            {
                return BadRequest(checkoutVendorValidation);
            }

            var orderTaxAndShippingPrice = await _checkoutService.UpdateTaxAndShippingPrices(checkoutId, model);

            return Ok(orderTaxAndShippingPrice);
        }

        [HttpGet("success")]
        public IActionResult Success(long orderId)
        {
            return Ok(new { orderId });
        }

        [HttpGet("error")]
        public IActionResult Error(long orderId)
        {
            return BadRequest(new { orderId });
        }

        private void PopulateShippingForm(DeliveryInformationVm model, User currentUser)
        {
            model.ExistingShippingAddresses = _userAddressRepository.Query()
                .Where(x => (x.AddressType == AddressType.Shipping) && (x.UserId == currentUser.Id))
                .Select(x => new ShippingAddressVm
                {
                    UserAddressId = x.Id,
                    ContactName = x.Address.ContactName,
                    Phone = x.Address.Phone,
                    AddressLine1 = x.Address.AddressLine1,
                    CityName = x.Address.City,
                    ZipCode = x.Address.ZipCode,
                    DistrictName = x.Address.District.Name,
                    StateOrProvinceName = x.Address.StateOrProvince.Name,
                    CountryName = x.Address.Country.Name,
                    IsCityEnabled = x.Address.Country.IsCityEnabled,
                    IsZipCodeEnabled = x.Address.Country.IsZipCodeEnabled,
                    IsDistrictEnabled = x.Address.Country.IsDistrictEnabled
                }).ToList();

            model.ShippingAddressId = currentUser.DefaultShippingAddressId ?? 0;

            model.UseShippingAddressAsBillingAddress = true;

            model.NewAddressForm.ShipableContries = _countryRepository.Query()
                .Where(x => x.IsShippingEnabled)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                }).ToList();

            if (model.NewAddressForm.ShipableContries.Count == 1)
            {
                var onlyShipableCountryId = model.NewAddressForm.ShipableContries.First().Value;

                model.NewAddressForm.StateOrProvinces = _stateOrProvinceRepository
                .Query()
                .Where(x => x.CountryId == onlyShipableCountryId)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                }).ToList();
            }
        }

        private bool IsCheckoutOwner(Checkout checkout, User currentUser)
        {
            return checkout.CustomerId == currentUser.Id || checkout.CreatedById == currentUser.Id;
        }

        private object ValidateCartVendorAssociations(long customerId, IList<CartItem> cartItems)
        {
            foreach (var cartItem in cartItems)
            {
                var vendorProduct = ResolveVendorProduct(customerId, cartItem.ProductId, cartItem.VendorProductId, cartItem.VendorId);
                if (vendorProduct == null)
                {
                    return new
                    {
                        message = "No active vendor association is available for one or more cart items.",
                        productId = cartItem.ProductId,
                        cartItemId = cartItem.Id
                    };
                }

                var vendorVariation = GetVendorProductVariation(vendorProduct, cartItem.ProductId, cartItem.VendorProductVariationId);
                if (RequiresVendorVariation(vendorProduct) && vendorVariation == null)
                {
                    return new
                    {
                        message = "A selected vendor product variation is required for one or more cart items.",
                        productId = cartItem.ProductId,
                        cartItemId = cartItem.Id
                    };
                }
            }

            return null;
        }

        private IList<CartItemForCoupon> CreateCouponCartItems(long customerId, IList<CartItem> cartItems)
        {
            return cartItems.Select(x =>
            {
                var vendorProduct = ResolveVendorProduct(customerId, x.ProductId, x.VendorProductId, x.VendorId);
                var vendorVariation = GetVendorProductVariation(vendorProduct, x.ProductId, x.VendorProductVariationId);

                return new CartItemForCoupon
                {
                    ProductId = x.ProductId,
                    VendorId = vendorProduct?.VendorId ?? x.VendorId,
                    VendorProductId = vendorProduct?.Id ?? x.VendorProductId,
                    VendorProductVariationId = vendorVariation?.Id ?? x.VendorProductVariationId,
                    Price = vendorProduct != null && (!RequiresVendorVariation(vendorProduct) || vendorVariation != null)
                        ? GetProductPrice(vendorProduct, vendorVariation)
                        : (decimal?)null,
                    Quantity = x.Quantity
                };
            }).ToList();
        }

        private object ValidateCheckoutVendorAssociations(Guid checkoutId, long customerId)
        {
            var checkoutItems = _checkoutRepository.Query()
                .Where(x => x.Id == checkoutId)
                .SelectMany(x => x.CheckoutItems)
                .ToList();

            foreach (var checkoutItem in checkoutItems)
            {
                var vendorProduct = ResolveVendorProduct(customerId, checkoutItem.ProductId, checkoutItem.VendorProductId, checkoutItem.VendorId);
                if (vendorProduct == null)
                {
                    return new
                    {
                        message = "No active vendor association is available for one or more checkout items.",
                        productId = checkoutItem.ProductId,
                        checkoutItemId = checkoutItem.Id
                    };
                }

                var vendorVariation = GetVendorProductVariation(vendorProduct, checkoutItem.ProductId, checkoutItem.VendorProductVariationId);
                if (RequiresVendorVariation(vendorProduct) && vendorVariation == null)
                {
                    return new
                    {
                        message = "A selected vendor product variation is required for one or more checkout items.",
                        productId = checkoutItem.ProductId,
                        checkoutItemId = checkoutItem.Id
                    };
                }
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

            var product = GetProductWithOptions(productId);

            return query
                .Where(x => associatedVendorIds.Contains(x.VendorId) && x.StockQuantity > 0)
                .ToList()
                .FirstOrDefault(x => GetResolvedStockQuantity(x, MatchVendorProductVariation(x, product)) > 0);
        }

        private static decimal GetProductPrice(VendorProduct vendorProduct, VendorProductVariation vendorVariation)
        {
            if (vendorVariation != null)
            {
                return VendorProductPricing.GetEffectivePrice(vendorVariation);
            }

            return RequiresVendorVariation(vendorProduct)
                ? 0
                : VendorProductPricing.GetEffectivePrice(vendorProduct);
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

        private bool IsVendorAssociated(long customerId, long vendorId)
        {
            return _vendorCustomerAssociationRepository.Query()
                .Any(x => x.CustomerId == customerId && x.VendorId == vendorId && x.IsActive && !x.IsDeleted);
        }
    }
}

// View Model for validating coupon in checkout
public class ValidateCouponForCheckoutVm
{
    public string CouponCode { get; set; }
}
