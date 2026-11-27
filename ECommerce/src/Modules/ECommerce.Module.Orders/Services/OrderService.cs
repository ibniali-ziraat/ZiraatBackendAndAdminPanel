using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Catalog.Services;
using ECommerce.Module.Checkouts.Areas.Checkouts.ViewModels;
using ECommerce.Module.Checkouts.Models;
using ECommerce.Module.Checkouts.Services;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Orders.Areas.Orders.ViewModels;
using ECommerce.Module.Orders.Events;
using ECommerce.Module.Orders.Models;
using ECommerce.Module.Pricing.Services;
using ECommerce.Module.ShippingPrices.Services;
using ECommerce.Module.ShoppingCart.Models;
using ECommerce.Module.Tax.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ImainOrder= ECommerce.Module.Orders.Services.IOrderService;
using ICoreOrder = ECommerce.Module.Core.Services.IOrderService;

namespace ECommerce.Module.Orders.Services
{
    public class OrderService : ImainOrder,ICoreOrder
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly ICouponService _couponService;
        private readonly IRepository<CheckoutItem> _checkoutItemRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly ITaxService _taxService;
        private readonly ICheckoutService _checkoutService;
        private readonly IRepositoryWithTypedId<Checkout, Guid> _checkoutRepository;
        private readonly IShippingPriceService _shippingPriceService;
        private readonly IRepository<UserAddress> _userAddressRepository;
        private readonly IRepository<District> _districtRepository;
        private readonly IMediator _mediator;
        private readonly IProductPricingService _productPricingService;
        private readonly IRepository<CustomerSettlement> _customerSettlementRepository;
        private readonly IRepository<VendorProduct> _vendorProductRepository;


        public OrderService(IRepository<Order> orderRepository,
            ICouponService couponService,
            IRepository<CheckoutItem> checkoutItemRepository,
            IRepository<OrderItem> orderItemRepository,
            ITaxService taxService,
            ICheckoutService checkoutSerivce,
            IRepositoryWithTypedId<Checkout, Guid> checkoutRepository,
            IShippingPriceService shippingPriceService,
            IRepository<UserAddress> userAddressRepository,
            IRepository<District> districtRepository,
            IMediator mediator,
            IProductPricingService productPricingService,
            IRepository<CustomerSettlement> customerSettlementRepository,
            IRepository<VendorProduct> vendorProductRepository)
        {
            _orderRepository = orderRepository;
            _couponService = couponService;
            _checkoutItemRepository = checkoutItemRepository;
            _orderItemRepository = orderItemRepository;
            _taxService = taxService;
            _checkoutService = checkoutSerivce;
            _checkoutRepository = checkoutRepository;
            _shippingPriceService = shippingPriceService;
            _userAddressRepository = userAddressRepository;
            _districtRepository = districtRepository;
            _mediator = mediator;
            _productPricingService = productPricingService;
            _customerSettlementRepository = customerSettlementRepository;
            _vendorProductRepository = vendorProductRepository;
        }

        public async Task<Result<Order>> CreateOrder(Guid checkoutId, string paymentMethod, decimal paymentFeeAmount, OrderStatus orderStatus = OrderStatus.New)
        {
            var checkout = await _checkoutRepository
               .Query()
               .Where(x => x.Id == checkoutId).FirstOrDefaultAsync();

            if (checkout == null)
            {
                return Result.Fail<Order>($"Checkout id {checkoutId} cannot be found");
            }

            var shippingData = JsonConvert.DeserializeObject<DeliveryInformationVm>(
                checkout.ShippingData,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Include
                });
            Address billingAddress;
            Address shippingAddress;
            if (shippingData.ShippingAddressId == 0)
            {
                var districtValidation = await ValidateDistrict(shippingData.NewAddressForm.DistrictId);
                if (!districtValidation.Succeeded)
                {
                    return Result.Fail<Order>(districtValidation.ErrorMessage);
                }

                var address = new Address
                {
                    AddressLine1 = shippingData.NewAddressForm.AddressLine1,
                    AddressLine2 = shippingData.NewAddressForm.AddressLine2,
                    ContactName = shippingData.NewAddressForm.ContactName,
                    CountryId = shippingData.NewAddressForm.CountryId,
                    StateOrProvinceId = shippingData.NewAddressForm.StateOrProvinceId,
                    DistrictId = shippingData.NewAddressForm.DistrictId,
                    City = shippingData.NewAddressForm.City,
                    ZipCode = shippingData.NewAddressForm.ZipCode,
                    Phone = shippingData.NewAddressForm.Phone
                };

                var userAddress = new UserAddress
                {
                    Address = address,
                    AddressType = AddressType.Shipping,
                    UserId = checkout.CustomerId
                };

                _userAddressRepository.Add(userAddress);

                shippingAddress = address;
            }
            else
            {
                shippingAddress = _userAddressRepository.Query().Where(x => x.Id == shippingData.ShippingAddressId).Select(x => x.Address).First();
            }

            if (shippingData.UseShippingAddressAsBillingAddress)
            {
                billingAddress = shippingAddress;
            }
            else if (shippingData.BillingAddressId == 0)
            {
                var districtValidation = await ValidateDistrict(shippingData.NewBillingAddressForm.DistrictId);
                if (!districtValidation.Succeeded)
                {
                    return Result.Fail<Order>(districtValidation.ErrorMessage);
                }

                var address = new Address
                {
                    AddressLine1 = shippingData.NewBillingAddressForm.AddressLine1,
                    AddressLine2 = shippingData.NewBillingAddressForm.AddressLine2,
                    ContactName = shippingData.NewBillingAddressForm.ContactName,
                    CountryId = shippingData.NewBillingAddressForm.CountryId,
                    StateOrProvinceId = shippingData.NewBillingAddressForm.StateOrProvinceId,
                    DistrictId = shippingData.NewBillingAddressForm.DistrictId,
                    City = shippingData.NewBillingAddressForm.City,
                    ZipCode = shippingData.NewBillingAddressForm.ZipCode,
                    Phone = shippingData.NewBillingAddressForm.Phone
                };

                var userAddress = new UserAddress
                {
                    Address = address,
                    AddressType = AddressType.Billing,
                    UserId = checkout.CustomerId
                };

                _userAddressRepository.Add(userAddress);

                billingAddress = address;
            }
            else
            {
                billingAddress = _userAddressRepository.Query().Where(x => x.Id == shippingData.BillingAddressId).Select(x => x.Address).First();
            }

            return await CreateOrder(checkoutId, paymentMethod, paymentFeeAmount, shippingData.ShippingMethod, billingAddress, shippingAddress, orderStatus);
        }

        public async Task<Result<Order>> CreateOrder(Guid checkoutId, string paymentMethod, decimal paymentFeeAmount, string shippingMethodName, Address billingAddress, Address shippingAddress, OrderStatus orderStatus = OrderStatus.New)
        {
            var checkout = _checkoutRepository
                .Query()
                .Include(c => c.CheckoutItems).ThenInclude(x => x.Product)
                .Include(c => c.CheckoutItems).ThenInclude(x => x.Product).ThenInclude(x => x.LinkedProductLinks)
                .Include(c => c.CheckoutItems).ThenInclude(x => x.Product).ThenInclude(x => x.OptionCombinations)
                .Include(c => c.Customer)
                .Include(c => c.CreatedBy)
                .Where(x => x.Id == checkoutId).FirstOrDefault();

            if (checkout == null)
            {
                return Result.Fail<Order>($"Checkout id {checkoutId} cannot be found");
            }

            var checkingDiscountResult = await CheckForDiscountIfAny(checkout);
            if (!checkingDiscountResult.Succeeded)
            {
                return Result.Fail<Order>(checkingDiscountResult.ErrorMessage);
            }

            var validateShippingMethodResult = await ValidateShippingMethod(shippingMethodName, shippingAddress, checkout);
            if (!validateShippingMethodResult.Success)
            {
                return Result.Fail<Order>(validateShippingMethodResult.Error);
            }

            var shippingMethod = validateShippingMethodResult.Value;

            var orderBillingAddress = new OrderAddress()
            {
                AddressLine1 = billingAddress.AddressLine1,
                AddressLine2 = billingAddress.AddressLine2,
                ContactName = billingAddress.ContactName,
                CountryId = billingAddress.CountryId,
                StateOrProvinceId = billingAddress.StateOrProvinceId,
                DistrictId = billingAddress.DistrictId,
                City = billingAddress.City,
                ZipCode = billingAddress.ZipCode,
                Phone = billingAddress.Phone
            };

            var orderShippingAddress = new OrderAddress()
            {
                AddressLine1 = shippingAddress.AddressLine1,
                AddressLine2 = shippingAddress.AddressLine2,
                ContactName = shippingAddress.ContactName,
                CountryId = shippingAddress.CountryId,
                StateOrProvinceId = shippingAddress.StateOrProvinceId,
                DistrictId = shippingAddress.DistrictId,
                City = shippingAddress.City,
                ZipCode = shippingAddress.ZipCode,
                Phone = shippingAddress.Phone
            };

            var order = new Order
            {
                Customer = checkout.Customer,
                CreatedOn = DateTimeOffset.Now,
                CreatedBy = checkout.CreatedBy,
                LatestUpdatedOn = DateTimeOffset.Now,
                LatestUpdatedById = checkout.CreatedById,
                BillingAddress = orderBillingAddress,
                ShippingAddress = orderShippingAddress,
                PaymentMethod = paymentMethod,
                PaymentFeeAmount = paymentFeeAmount
            };
            var isBuyOnCredit = string.Equals(paymentMethod, "BuyOnCredit", StringComparison.OrdinalIgnoreCase);

            using (var transaction = _checkoutItemRepository.BeginTransaction())
            {
                foreach (var checkoutItem in checkout.CheckoutItems)
                {
                    if (!checkoutItem.Product.IsAllowToOrder || !checkoutItem.Product.IsPublished || checkoutItem.Product.IsDeleted)
                    {
                        return Result.Fail<Order>($"The product {checkoutItem.Product.Name} is not available any more");
                    }

                    var vendorProduct = GetVendorProduct(checkoutItem);
                    var vendorVariation = GetVendorProductVariation(vendorProduct, checkoutItem);
                    if (RequiresVendorVariation(vendorProduct) && vendorVariation == null)
                    {
                        return Result.Fail<Order>($"Please select a valid variation for {checkoutItem.Product.Name}");
                    }

                    var stockQuantity = GetResolvedStockQuantity(vendorProduct, vendorVariation, checkoutItem.Product.StockQuantity);
                    if (checkoutItem.Product.StockTrackingIsEnabled && stockQuantity < checkoutItem.Quantity)
                    {
                        return Result.Fail<Order>($"{checkoutItem.Product.Name} is currently out of stock");
                    }

                    var taxPercent = await GetTaxPercent(checkoutItem, shippingAddress);

                    var calculatedProductPrice = CalculateProductPrice(checkoutItem);

                    var productPrice = calculatedProductPrice.OldPrice ?? calculatedProductPrice.Price;
                    var taxAmountPerUnit = vendorVariation != null
                        ? VendorProductPricing.GetTaxAmount(vendorVariation, checkoutItem.Product.Price)
                        : vendorProduct != null
                            ? VendorProductPricing.GetTaxAmount(vendorProduct, checkoutItem.Product.Price)
                        : calculatedProductPrice.Price * taxPercent / 100;
                    if (isBuyOnCredit && vendorVariation != null)
                    {
                        productPrice = VendorProductPricing.GetSalePriceForCreditor(vendorVariation, checkoutItem.Product.Price) - taxAmountPerUnit;
                    }
                    else if (isBuyOnCredit && vendorProduct != null)
                    {
                        productPrice = VendorProductPricing.GetSalePriceForCreditor(vendorProduct, checkoutItem.Product.Price) - taxAmountPerUnit;
                    }
                    else if (checkout.IsProductPriceIncludeTax && vendorProduct == null)
                    {
                        productPrice = productPrice / (1 + (taxPercent / 100));
                    }

                    var orderItem = new OrderItem
                    {
                        Product = checkoutItem.Product,
                        VendorId = checkoutItem.VendorId,
                        VendorProductId = checkoutItem.VendorProductId,
                        VendorProductVariationId = vendorVariation?.Id ?? checkoutItem.VendorProductVariationId,
                        VariationName = GetVariationName(vendorVariation),
                        ProductPrice = productPrice,
                        Quantity = checkoutItem.Quantity,
                        TaxPercent = taxPercent,
                        TaxAmount = checkoutItem.Quantity * taxAmountPerUnit
                    };

                    var discountedItem = checkingDiscountResult.DiscountedProducts.FirstOrDefault(x => x.Id == checkoutItem.ProductId);
                    if (discountedItem != null)
                    {
                        orderItem.DiscountAmount = discountedItem.DiscountAmount;
                    }

                    if (!isBuyOnCredit && calculatedProductPrice.OldPrice.HasValue)
                    {
                        orderItem.DiscountAmount += orderItem.Quantity * (calculatedProductPrice.OldPrice.Value - calculatedProductPrice.Price);
                    }

                    order.AddOrderItem(orderItem);
                    if (checkoutItem.Product.StockTrackingIsEnabled)
                    {
                        if (vendorVariation != null)
                        {
                            vendorVariation.StockQuantity = vendorVariation.StockQuantity - checkoutItem.Quantity;
                            vendorVariation.LatestUpdatedOn = DateTimeOffset.Now;
                        }
                        else if (vendorProduct != null)
                        {
                            vendorProduct.StockQuantity = vendorProduct.StockQuantity - checkoutItem.Quantity;
                            vendorProduct.LatestUpdatedOn = DateTimeOffset.Now;
                        }
                        else
                        {
                            checkoutItem.Product.StockQuantity = checkoutItem.Product.StockQuantity - checkoutItem.Quantity;
                        }
                    }
                }

                _checkoutItemRepository.SaveChanges();
                transaction.Commit();
            }

            order.OrderStatus = orderStatus;
            order.OrderNote = checkout.OrderNote;
            order.CouponCode = checkingDiscountResult.CouponCode;
            order.CouponRuleName = checkout.CouponRuleName;
            order.DiscountAmount = checkingDiscountResult.DiscountAmount + order.OrderItems.Sum(x => x.DiscountAmount);
            order.ShippingFeeAmount = shippingMethod.Price;
            order.ShippingMethod = shippingMethod.Name;
            order.PaymentMethod = paymentMethod;
            order.PaymentFeeAmount = paymentFeeAmount;
            order.TaxAmount = order.OrderItems.Sum(x => x.TaxAmount);
            order.SubTotal = order.OrderItems.Sum(x => x.ProductPrice * x.Quantity);
            order.SubTotalWithDiscount = order.SubTotal - checkingDiscountResult.DiscountAmount;
            order.OrderTotal = order.SubTotal + order.TaxAmount + order.ShippingFeeAmount + order.PaymentFeeAmount - order.DiscountAmount;
            _orderRepository.Add(order);

            var vendorIds = checkout.CheckoutItems.Where(x => x.VendorId.HasValue).Select(x => x.VendorId.Value).Distinct().ToList();
            var hasNonVendorItems = checkout.CheckoutItems.Any(x => !x.VendorId.HasValue);
            var shouldCreateSubOrders = vendorIds.Count > 1 || (vendorIds.Count == 1 && hasNonVendorItems);
            if (vendorIds.Count == 1 && !hasNonVendorItems)
            {
                order.VendorId = vendorIds[0];
            }
            else if (shouldCreateSubOrders)
            {
                order.IsMasterOrder = true;
            }

            IList<Order> subOrders = new List<Order>();
            if (shouldCreateSubOrders)
            {
                foreach (var vendorId in vendorIds)
                {
                    var subOrder = new Order
                    {
                        CustomerId = checkout.CustomerId,
                        CreatedOn = DateTimeOffset.Now,
                        CreatedById = checkout.CreatedById,
                        LatestUpdatedOn = DateTimeOffset.Now,
                        LatestUpdatedById = checkout.CreatedById,
                        BillingAddress = orderBillingAddress,
                        ShippingAddress = orderShippingAddress,
                        VendorId = vendorId,
                        Parent = order,
                        PaymentMethod = paymentMethod,
                        PaymentFeeAmount = 0,
                        ShippingMethod = shippingMethod.Name
                    };

                    foreach (var cartItem in checkout.CheckoutItems.Where(x => x.VendorId == vendorId))
                    {
                        var vendorProduct = GetVendorProduct(cartItem);
                        var vendorVariation = GetVendorProductVariation(vendorProduct, cartItem);
                        if (RequiresVendorVariation(vendorProduct) && vendorVariation == null)
                        {
                            return Result.Fail<Order>($"Please select a valid variation for {cartItem.Product.Name}");
                        }

                        var taxPercent = await GetTaxPercent(cartItem, shippingAddress);
                        var calculatedProductPrice = CalculateProductPrice(cartItem);
                        var productPrice = calculatedProductPrice.OldPrice ?? calculatedProductPrice.Price;
                        var taxAmountPerUnit = vendorVariation != null
                            ? VendorProductPricing.GetTaxAmount(vendorVariation, cartItem.Product.Price)
                            : vendorProduct != null
                                ? VendorProductPricing.GetTaxAmount(vendorProduct, cartItem.Product.Price)
                            : calculatedProductPrice.Price * taxPercent / 100;
                        if (isBuyOnCredit && vendorVariation != null)
                        {
                            productPrice = VendorProductPricing.GetSalePriceForCreditor(vendorVariation, cartItem.Product.Price) - taxAmountPerUnit;
                        }
                        else if (isBuyOnCredit && vendorProduct != null)
                        {
                            productPrice = VendorProductPricing.GetSalePriceForCreditor(vendorProduct, cartItem.Product.Price) - taxAmountPerUnit;
                        }
                        else if (checkout.IsProductPriceIncludeTax && vendorProduct == null)
                        {
                            productPrice = productPrice / (1 + (taxPercent / 100));
                        }

                        var orderItem = new OrderItem
                        {
                            Product = cartItem.Product,
                            VendorId = cartItem.VendorId,
                            VendorProductId = cartItem.VendorProductId,
                            VendorProductVariationId = vendorVariation?.Id ?? cartItem.VendorProductVariationId,
                            VariationName = GetVariationName(vendorVariation),
                            ProductPrice = productPrice,
                            Quantity = cartItem.Quantity,
                            TaxPercent = taxPercent,
                            TaxAmount = cartItem.Quantity * taxAmountPerUnit
                        };

                        var discountedItem = checkingDiscountResult.DiscountedProducts.FirstOrDefault(x => x.Id == cartItem.ProductId);
                        if (discountedItem != null)
                        {
                            orderItem.DiscountAmount = discountedItem.DiscountAmount;
                        }

                        if (!isBuyOnCredit && calculatedProductPrice.OldPrice.HasValue)
                        {
                            orderItem.DiscountAmount += orderItem.Quantity * (calculatedProductPrice.OldPrice.Value - calculatedProductPrice.Price);
                        }

                        subOrder.AddOrderItem(orderItem);
                    }

                    subOrder.SubTotal = subOrder.OrderItems.Sum(x => x.ProductPrice * x.Quantity);
                    subOrder.TaxAmount = subOrder.OrderItems.Sum(x => x.TaxAmount);
                    subOrder.DiscountAmount = subOrder.OrderItems.Sum(x => x.DiscountAmount);
                    subOrder.OrderTotal = subOrder.SubTotal + subOrder.TaxAmount + subOrder.ShippingFeeAmount - subOrder.DiscountAmount;
                    _orderRepository.Add(subOrder);
                    subOrders.Add(subOrder);
                }
            }

            using (var transaction = _orderRepository.BeginTransaction())
            {
                _orderRepository.SaveChanges();
                await _mediator.Publish(new OrderCreated(order));
                foreach (var subOrder in subOrders)
                {
                    await _mediator.Publish(new OrderCreated(subOrder));
                }

                _couponService.AddCouponUsage(checkout.CustomerId, order.Id, checkingDiscountResult);
                _orderRepository.SaveChanges();
                transaction.Commit();
            }
            //add email smtp details in appsettings.json then uncomment the below code
           //await _mediator.Publish(new AfterOrderCreated(order));

            return Result.Ok(order);
        }

        public void CancelOrder(Order order)
        {
            order.OrderStatus = OrderStatus.Canceled;
            order.LatestUpdatedOn = DateTimeOffset.Now;

            var orderItems = _orderItemRepository.Query()
                .Include(x => x.Product).ThenInclude(x => x.LinkedProductLinks)
                .Include(x => x.Product).ThenInclude(x => x.OptionCombinations)
                .Where(x => x.Order.Id == order.Id);
            foreach (var item in orderItems)
            {
                if (item.Product.StockTrackingIsEnabled)
                {
                    var vendorProduct = GetVendorProduct(item);
                    var vendorVariation = GetVendorProductVariation(vendorProduct, item);
                    if (vendorVariation != null)
                    {
                        vendorVariation.StockQuantity = vendorVariation.StockQuantity + item.Quantity;
                        vendorVariation.LatestUpdatedOn = DateTimeOffset.Now;
                    }
                    else if (vendorProduct != null)
                    {
                        vendorProduct.StockQuantity = vendorProduct.StockQuantity + item.Quantity;
                        vendorProduct.LatestUpdatedOn = DateTimeOffset.Now;
                    }
                    else
                    {
                        item.Product.StockQuantity = item.Product.StockQuantity + item.Quantity;
                    }
                }
            }
        }

        public async Task<decimal> GetTax(Guid checkoutId, string countryId, long stateOrProvinceId, string zipCode)
        {
            decimal taxAmount = 0;

            var cartItems = _checkoutItemRepository.Query()
                .Where(x => x.CheckoutId == checkoutId)
                .Include(x => x.Product)
                .ThenInclude(x => x.LinkedProductLinks)
                .Include(x => x.Product)
                .ThenInclude(x => x.OptionCombinations)
                .ToList()
                .Select(x =>
                {
                    var vendorProduct = GetVendorProduct(x);
                    var vendorVariation = GetVendorProductVariation(vendorProduct, x);

                    return new CartItemVm
                    {
                        Quantity = x.Quantity,
                        Price = GetTaxableProductPrice(x),
                        TaxClassId = x.Product.TaxClassId,
                        TaxPercent = vendorVariation?.TaxRate ?? vendorProduct?.TaxRate,
                        TaxAmount = vendorVariation != null
                            ? VendorProductPricing.GetTaxAmount(vendorVariation, x.Product.Price)
                            : vendorProduct != null
                                ? VendorProductPricing.GetTaxAmount(vendorProduct, x.Product.Price)
                            : (decimal?)null
                    };
                }).ToList();

            foreach (var cartItem in cartItems)
            {
                if (cartItem.TaxAmount.HasValue)
                {
                    taxAmount = taxAmount + cartItem.Quantity * cartItem.TaxAmount.Value;
                }
                else if (cartItem.TaxPercent.HasValue)
                {
                    taxAmount = taxAmount + cartItem.Quantity * cartItem.Price * cartItem.TaxPercent.Value / 100;
                }
                else if (cartItem.TaxClassId.HasValue)
                {
                    var taxRate = await _taxService.GetTaxPercent(cartItem.TaxClassId, countryId, stateOrProvinceId, zipCode);
                    taxAmount = taxAmount + cartItem.Quantity * cartItem.Price * taxRate / 100;
                }
            }

            return taxAmount;
        }

        private async Task<CouponValidationResult> CheckForDiscountIfAny(Checkout checkout)
        {
            if (string.IsNullOrWhiteSpace(checkout.CouponCode))
            {
                return new CouponValidationResult { Succeeded = true, DiscountAmount = 0 };
            }

            var cartInfoForCoupon = new CartInfoForCoupon
            {
                Items = checkout.CheckoutItems.Select(x => new CartItemForCoupon
                    {
                        ProductId = x.ProductId,
                        VendorId = x.VendorId,
                        VendorProductId = x.VendorProductId,
                        VendorProductVariationId = x.VendorProductVariationId,
                        Price = GetProductPrice(x),
                        Quantity = x.Quantity
                    }).ToList()
            };

            var couponValidationResult = await _couponService.Validate(checkout.CustomerId, checkout.CouponCode, cartInfoForCoupon);
            return couponValidationResult;
        }

        private async Task<Result<ShippingPrice>> ValidateShippingMethod(string shippingMethodName, Address shippingAddress, Checkout checkout)
        {
            var applicableShippingPrices = await _shippingPriceService.GetApplicableShippingPrices(new GetShippingPriceRequest
            {
                OrderAmount = checkout.CheckoutItems.Sum(x => GetSubtotalProductPrice(x) * x.Quantity),
                ShippingAddress = shippingAddress
            });

            var shippingMethod = applicableShippingPrices.FirstOrDefault(x => x.Name == shippingMethodName);
            if (shippingMethod == null)
            {
                return Result.Fail<ShippingPrice>($"Invalid shipping method {shippingMethod}");
            }

            return Result.Ok(shippingMethod);
        }

        private async Task<DistrictValidationResult> ValidateDistrict(long? districtId)
        {
            if (!districtId.HasValue)
            {
                return new DistrictValidationResult { Succeeded = true };
            }

            var districtExists = await _districtRepository.Query()
                .AnyAsync(x => x.Id == districtId.Value);

            if (!districtExists)
            {
                return new DistrictValidationResult
                {
                    Succeeded = false,
                    ErrorMessage = $"District with id {districtId} does not exist"
                };
            }

            return new DistrictValidationResult { Succeeded = true };
        }

        private VendorProduct GetVendorProduct(CheckoutItem checkoutItem)
        {
            var assignedProductIds = GetAssignedProductIds(checkoutItem.Product, checkoutItem.ProductId);
            if (checkoutItem.VendorProductId.HasValue)
            {
                return _vendorProductRepository.Query()
                    .Include(x => x.Variations).ThenInclude(x => x.OptionCombinations)
                    .FirstOrDefault(x => x.Id == checkoutItem.VendorProductId.Value && assignedProductIds.Contains(x.ProductId) && x.IsActive);
            }

            if (checkoutItem.VendorId.HasValue)
            {
                return _vendorProductRepository.Query()
                    .Include(x => x.Variations).ThenInclude(x => x.OptionCombinations)
                    .FirstOrDefault(x => x.VendorId == checkoutItem.VendorId.Value && assignedProductIds.Contains(x.ProductId) && x.IsActive);
            }

            return null;
        }

        private VendorProduct GetVendorProduct(OrderItem orderItem)
        {
            var assignedProductIds = GetAssignedProductIds(orderItem.Product, orderItem.ProductId);
            if (orderItem.VendorProductId.HasValue)
            {
                return _vendorProductRepository.Query()
                    .Include(x => x.Variations).ThenInclude(x => x.OptionCombinations)
                    .FirstOrDefault(x => x.Id == orderItem.VendorProductId.Value && assignedProductIds.Contains(x.ProductId) && x.IsActive);
            }

            if (orderItem.VendorId.HasValue)
            {
                return _vendorProductRepository.Query()
                    .Include(x => x.Variations).ThenInclude(x => x.OptionCombinations)
                    .FirstOrDefault(x => x.VendorId == orderItem.VendorId.Value && assignedProductIds.Contains(x.ProductId) && x.IsActive);
            }

            return null;
        }

        private decimal GetProductPrice(CheckoutItem checkoutItem)
        {
            var vendorProduct = GetVendorProduct(checkoutItem);
            return GetProductPrice(vendorProduct, GetVendorProductVariation(vendorProduct, checkoutItem), checkoutItem.Product.Price);
        }

        private CalculatedProductPrice CalculateProductPrice(CheckoutItem checkoutItem)
        {
            var vendorProduct = GetVendorProduct(checkoutItem);
            if (vendorProduct == null)
            {
                return _productPricingService.CalculateProductPrice(checkoutItem.Product);
            }

            var vendorVariation = GetVendorProductVariation(vendorProduct, checkoutItem);
            if (RequiresVendorVariation(vendorProduct) && vendorVariation == null)
            {
                return _productPricingService.CalculateProductPrice(checkoutItem.Product);
            }

            return _productPricingService.CalculateProductPrice(
                GetProductPrice(vendorProduct, vendorVariation, checkoutItem.Product.Price),
                vendorVariation != null ? VendorProductPricing.GetVendorOldPrice(vendorVariation) : VendorProductPricing.GetVendorOldPrice(vendorProduct),
                null,
                null,
                null);
        }

        private async Task<decimal> GetTaxPercent(CheckoutItem checkoutItem, Address shippingAddress)
        {
            var vendorProduct = GetVendorProduct(checkoutItem);
            var vendorTaxRate = GetVendorProductVariation(vendorProduct, checkoutItem)?.TaxRate ?? vendorProduct?.TaxRate;
            if (vendorTaxRate.HasValue)
            {
                return vendorTaxRate.Value;
            }

            return await _taxService.GetTaxPercent(
                checkoutItem.Product.TaxClassId,
                shippingAddress.CountryId,
                shippingAddress.StateOrProvinceId,
                shippingAddress.ZipCode);
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

        private static int GetResolvedStockQuantity(VendorProduct vendorProduct, VendorProductVariation vendorVariation, int fallbackStock)
        {
            return vendorVariation?.StockQuantity
                ?? (RequiresVendorVariation(vendorProduct) ? 0 : vendorProduct?.StockQuantity)
                ?? fallbackStock;
        }

        private static bool RequiresVendorVariation(VendorProduct vendorProduct)
        {
            return vendorProduct?.Variations?.Any(x => x.IsActive) == true;
        }

        private static VendorProductVariation GetVendorProductVariation(VendorProduct vendorProduct, CheckoutItem checkoutItem)
        {
            return GetVendorProductVariation(vendorProduct, checkoutItem?.Product, checkoutItem?.VendorProductVariationId);
        }

        private static VendorProductVariation GetVendorProductVariation(VendorProduct vendorProduct, OrderItem orderItem)
        {
            return GetVendorProductVariation(vendorProduct, orderItem?.Product, orderItem?.VendorProductVariationId);
        }

        private string GetVariationName(OrderItem orderItem)
        {
            var vendorVariation = GetVendorProductVariation(GetVendorProduct(orderItem), orderItem);
            return GetVariationName(vendorVariation);
        }

        private static string GetVariationName(VendorProductVariation vendorVariation)
        {
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

        private static IList<long> GetAssignedProductIds(Product product, long productId)
        {
            var assignedProductIds = new HashSet<long> { productId };
            var parentProductId = product?.LinkedProductLinks?
                .FirstOrDefault(x => x.LinkType == ProductLinkType.Super)
                ?.ProductId;

            if (parentProductId.HasValue)
            {
                assignedProductIds.Add(parentProductId.Value);
            }

            return assignedProductIds.ToList();
        }

        public async Task<decimal> GetCustomerTotalOrderAmount(long vendorId, long customerId)
        {
            return await _orderRepository.Query()
                .Where(o => o.VendorId == vendorId && o.CustomerId == customerId)
                .SumAsync(o => o.OrderTotal);
        }

        public async Task<decimal> GetCustomerTotalSettledAmount(long vendorId, long customerId)
        {
            return await _customerSettlementRepository.Query()
                .Where(s => s.VendorId == vendorId && s.CustomerId == customerId && !s.IsDeleted)
                .SumAsync(s => s.AmountPaid);
        }

        public async Task<List<OrderDto>> GetCustomerOrders(long vendorId, long customerId)
        {
            var orders = await _orderRepository.Query()
                .Where(o => o.VendorId == vendorId && o.CustomerId == customerId)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                    .ThenInclude(i => i.LinkedProductLinks)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                    .ThenInclude(i => i.OptionCombinations)
                .OrderByDescending(o => o.CreatedOn)
                .ToListAsync();

            return orders.SelectMany(o => o.OrderItems.Select(item => new OrderDto
            {
                OrderId = o.Id,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.FullName,
                ItemName = item.Product?.Name,
                VendorId = item.VendorId,
                VendorProductId = item.VendorProductId,
                VendorProductVariationId = item.VendorProductVariationId,
                VariationName = GetVariationName(item),
                ItemQuantity = item.Quantity,
                ItemAmount = (item.ProductPrice * item.Quantity) - item.DiscountAmount + item.TaxAmount,
                ItemTaxAmount = item.TaxAmount,
                ItemTaxPercent = item.TaxPercent,
                OrderedDate = o.CreatedOn.DateTime,
                OrderAmount = o.OrderTotal,
                OrderStatus = o.OrderStatus.ToString(),
                PaymentMethod = o.PaymentMethod
            })).ToList();
        }

        public async Task<List<OrderDto>> GetVendorOrders(long vendorId)
        {
            var orders = await _orderRepository.Query()
                .Where(o => !o.IsMasterOrder && (o.VendorId == vendorId || o.OrderItems.Any(i => i.VendorId == vendorId)))
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                    .ThenInclude(i => i.LinkedProductLinks)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                    .ThenInclude(i => i.OptionCombinations)
                .OrderByDescending(o => o.CreatedOn)
                .ToListAsync();

            return orders.SelectMany(o =>
            {
                var vendorItems = o.OrderItems.Where(item => !item.VendorId.HasValue || item.VendorId == vendorId).ToList();
                if (!vendorItems.Any())
                {
                    vendorItems = o.OrderItems.ToList();
                }

                return vendorItems.Select(item => new OrderDto
                {
                    OrderId = o.Id,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer?.FullName,
                    ItemName = item.Product?.Name,
                    VendorId = item.VendorId,
                    VendorProductId = item.VendorProductId,
                    VendorProductVariationId = item.VendorProductVariationId,
                    VariationName = GetVariationName(item),
                    ItemQuantity = item.Quantity,
                    ItemAmount = (item.ProductPrice * item.Quantity) - item.DiscountAmount + item.TaxAmount,
                    ItemTaxAmount = item.TaxAmount,
                    ItemTaxPercent = item.TaxPercent,
                    OrderedDate = o.CreatedOn.DateTime,
                    OrderAmount = o.OrderTotal,
                    OrderStatus = o.OrderStatus.ToString(),
                    PaymentMethod = o.PaymentMethod
                });
            }).ToList();
        }

        private class DistrictValidationResult
        {
            public bool Succeeded { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
}
