using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Checkouts.Areas.Checkouts.ViewModels;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Core.ViewModels;
using ECommerce.Module.Orders.Areas.Orders.ViewModels;
using ECommerce.Module.Orders.Models;

namespace ECommerce.Module.Orders.Areas.Orders.Controllers
{
    [Area("Orders")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IMediaService _mediaService;
        private readonly IRepository<Order> _orderRepository;
        private readonly IWorkContext _workContext;
        private readonly ICurrencyService _currencyService;
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IRepository<BusinessInformation> _businessInformationRepository;

        public OrderController(
            IRepository<Order> orderRepository,
            IWorkContext workContext,
            IMediaService mediaService,
            ICurrencyService currencyService,
            IRepository<VendorProduct> vendorProductRepository,
            IRepository<Vendor> vendorRepository,
            IRepository<BusinessInformation> businessInformationRepository)
        {
            _orderRepository = orderRepository;
            _workContext = workContext;
            _mediaService = mediaService;
            _currencyService = currencyService;
            _vendorProductRepository = vendorProductRepository;
            _vendorRepository = vendorRepository;
            _businessInformationRepository = businessInformationRepository;
        }

        [HttpGet("user/orders")]
        public async Task<IActionResult> OrderHistoryList()
        {
            var user = await _workContext.GetCurrentUser();
            var model = await _orderRepository
                .Query()
                .Where(x => x.CustomerId == user.Id && x.ParentId == null)
                .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.ThumbnailImage)
                .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.LinkedProductLinks)
                .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.OptionCombinations).ThenInclude(x => x.Option)
                .OrderByDescending(x => x.CreatedOn).ToListAsync();

            var model2 = model.Select(x => new OrderHistoryListItem(_currencyService)
            {
                Id = x.Id,
                CreatedOn = x.CreatedOn,
                SubTotal = x.SubTotal,
                OrderStatus = x.OrderStatus,
                OrderStatusString = x.OrderStatus.ToString(),
                OrderItems = x.OrderItems.Select(i => new OrderHistoryProductVm
                {
                    ProductId = i.ProductId,
                    VendorId = i.VendorId,
                    VendorProductId = i.VendorProductId,
                    VendorProductVariationId = i.VendorProductVariationId,
                    VendorAssignment = GetVendorAssignment(i),
                    VariationName = GetVariationName(i),
                    ProductName = i.Product.Name,
                    Quantity = i.Quantity,
                    ThumbnailImage = i.Product.ThumbnailImage != null ? i.Product.ThumbnailImage.FileName + "" : "",
                    ProductOptions = i.Product.OptionCombinations.Select(o => o.Value)
                }).ToList()
            }).ToList();

            foreach (var item in model2)
            {
                foreach (var product in item.OrderItems)
                {
                    product.ThumbnailImage = _mediaService.GetMediaUrl(product.ThumbnailImage);
                }
            }

            return Ok(model2);
        }

        [HttpGet("user/orders/{orderId}")]
        public async Task<IActionResult> OrderDetails(long orderId)
        {
            var user = await _workContext.GetCurrentUser();
            if (user == null)
            {
                return Unauthorized();
            }

            var order = _orderRepository
                .Query()
                .Include(x => x.ShippingAddress).ThenInclude(x => x.District)
                .Include(x => x.ShippingAddress).ThenInclude(x => x.StateOrProvince)
                .Include(x => x.ShippingAddress).ThenInclude(x => x.Country)
                .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.ThumbnailImage)
                .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.LinkedProductLinks)
                .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.OptionCombinations).ThenInclude(x => x.Option)
                .Include(x => x.Customer)
                .FirstOrDefault(x => x.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            if (order.CustomerId != user.Id)
            {
                return BadRequest(new { error = "You don't have permission to view this order" });
            }

            var vendors = await GetOrderVendors(order);
            var model = new OrderDetailVm(_currencyService)
            {
                Id = order.Id,
                VendorId = order.VendorId,
                Vendor = order.VendorId.HasValue ? vendors.GetValueOrDefault(order.VendorId.Value) : null,
                IsMasterOrder = order.IsMasterOrder,
                CreatedOn = order.CreatedOn,
                OrderStatus = (int)order.OrderStatus,
                OrderStatusString = order.OrderStatus.ToString(),
                CustomerId = order.CustomerId,
                CustomerName = order.Customer.FullName,
                CustomerEmail = order.Customer.Email,
                ShippingMethod = order.ShippingMethod,
                PaymentMethod = order.PaymentMethod,
                PaymentFeeAmount = order.PaymentFeeAmount,
                Subtotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                SubTotalWithDiscount = order.SubTotalWithDiscount,
                TaxAmount = order.TaxAmount,
                ShippingAmount = order.ShippingFeeAmount,
                OrderTotal = order.OrderTotal,
                OrderNote = order.OrderNote,
                ShippingAddress = new ShippingAddressVm
                {
                    AddressLine1 = order.ShippingAddress.AddressLine1,
                    CityName = order.ShippingAddress.City,
                    ZipCode = order.ShippingAddress.ZipCode,
                    ContactName = order.ShippingAddress.ContactName,
                    DistrictName = order.ShippingAddress.District?.Name,
                    StateOrProvinceName = order.ShippingAddress.StateOrProvince.Name,
                    Phone = order.ShippingAddress.Phone
                },
                OrderItems = order.OrderItems.Select(x => new OrderItemVm(_currencyService)
                {
                    Id = x.Id,
                    ProductId = x.Product.Id,
                    VendorId = x.VendorId,
                    Vendor = (x.VendorId ?? order.VendorId) is long vendorId ? vendors.GetValueOrDefault(vendorId) : null,
                    VendorProductId = x.VendorProductId,
                    VendorProductVariationId = x.VendorProductVariationId,
                    VendorAssignment = GetVendorAssignment(x),
                    ProductName = x.Product.Name,
                    VariationName = GetVariationName(x),
                    ProductPrice = x.ProductPrice,
                    Quantity = x.Quantity,
                    DiscountAmount = x.DiscountAmount,
                    ProductImage = x.Product.ThumbnailImage != null ? x.Product.ThumbnailImage.FileName : string.Empty,
                    TaxAmount = x.TaxAmount,
                    TaxPercent = x.TaxPercent,
                    VariationOptions = OrderItemVm.GetVariationOption(x.Product)
                }).ToList()
            };

            foreach (var item in model.OrderItems)
            {
                item.ProductImage = _mediaService.GetMediaUrl(item.ProductImage);
            }

            return Ok(model);
        }

        private async Task<Dictionary<long, OrderVendorVm>> GetOrderVendors(Order order)
        {
            var vendorIds = order.OrderItems.Select(x => x.VendorId).Append(order.VendorId)
                .Where(x => x.HasValue).Select(x => x.Value).Distinct().ToList();
            if (vendorIds.Count == 0)
            {
                return new Dictionary<long, OrderVendorVm>();
            }

            // Historical orders still identify soft-deleted vendors. Never expose vendor user records.
            var vendors = await _vendorRepository.Query().AsNoTracking()
                .Where(x => vendorIds.Contains(x.Id))
                .Select(x => new OrderVendorVm { VendorId = x.Id, VendorName = x.Name })
                .ToDictionaryAsync(x => x.VendorId);
            var businesses = await _businessInformationRepository.Query().AsNoTracking()
                .Where(x => vendorIds.Contains(x.VendorId))
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.VendorId,
                    x.ShopName,
                    Address = new OrderVendorAddressVm
                    {
                        Village = x.Village,
                        Tehsil = x.Tehsil,
                        District = x.District,
                        State = x.State,
                        Pincode = x.Pincode
                    }
                }).ToListAsync();

            foreach (var business in businesses.GroupBy(x => x.VendorId).Select(x => x.First()))
            {
                if (vendors.TryGetValue(business.VendorId, out var vendor))
                {
                    vendor.ShopName = business.ShopName;
                    vendor.Address = business.Address;
                }
            }

            return vendors;
        }

        private VendorProductAssignmentPricingVm GetVendorAssignment(OrderItem orderItem)
        {
            var vendorProduct = GetVendorProduct(orderItem);
            var vendorVariation = GetVendorProductVariation(vendorProduct, orderItem);
            return VendorProductAssignmentPricingVm.From(vendorProduct, vendorVariation, orderItem.Product?.Price ?? orderItem.ProductPrice);
        }

        private string GetVariationName(OrderItem orderItem)
        {
            var vendorVariation = GetVendorProductVariation(GetVendorProduct(orderItem), orderItem);
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

        private VendorProduct GetVendorProduct(OrderItem orderItem)
        {
            var assignedProductIds = GetAssignedProductIds(orderItem.Product, orderItem.ProductId);
            var vendorProductQuery = _vendorProductRepository.Query()
                .Include(x => x.Variations).ThenInclude(x => x.OptionCombinations)
                .Where(x => assignedProductIds.Contains(x.ProductId));

            return orderItem.VendorProductId.HasValue
                ? vendorProductQuery.FirstOrDefault(x => x.Id == orderItem.VendorProductId.Value)
                : orderItem.VendorId.HasValue
                    ? vendorProductQuery.FirstOrDefault(x => x.VendorId == orderItem.VendorId.Value)
                    : null;
        }

        private static VendorProductVariation GetVendorProductVariation(VendorProduct vendorProduct, OrderItem orderItem)
        {
            if (vendorProduct == null)
            {
                return null;
            }

            if (orderItem.VendorProductVariationId.HasValue)
            {
                var explicitVariation = vendorProduct.Variations.FirstOrDefault(x =>
                    x.Id == orderItem.VendorProductVariationId.Value && x.IsActive);
                if (explicitVariation != null)
                {
                    return explicitVariation;
                }
            }

            if (orderItem.Product == null || !orderItem.Product.OptionCombinations.Any())
            {
                return null;
            }

            var normalizedName = orderItem.Product.NormalizedName?.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedName))
            {
                var matchedByName = vendorProduct.Variations.FirstOrDefault(x =>
                    x.IsActive && string.Equals(x.NormalizedName?.Trim(), normalizedName, System.StringComparison.OrdinalIgnoreCase));
                if (matchedByName != null)
                {
                    return matchedByName;
                }
            }

            var productSignature = string.Join("|", orderItem.Product.OptionCombinations
                .Where(x => x != null)
                .OrderBy(x => x.OptionId)
                .Select(x => $"{x.OptionId}:{(x.Value ?? string.Empty).Trim().ToLowerInvariant()}"));

            return vendorProduct.Variations.FirstOrDefault(x =>
                x.IsActive && string.Join("|", x.OptionCombinations
                    .Where(o => o != null)
                    .OrderBy(o => o.OptionId)
                    .Select(o => $"{o.OptionId}:{(o.Value ?? string.Empty).Trim().ToLowerInvariant()}")) == productSignature);
        }

        private static System.Collections.Generic.IList<long> GetAssignedProductIds(ECommerce.Module.Catalog.Models.Product product, long productId)
        {
            var assignedProductIds = new System.Collections.Generic.HashSet<long> { productId };
            var parentProductId = product?.LinkedProductLinks?
                .FirstOrDefault(x => x.LinkType == ECommerce.Module.Catalog.Models.ProductLinkType.Super)
                ?.ProductId;

            if (parentProductId.HasValue)
            {
                assignedProductIds.Add(parentProductId.Value);
            }

            return assignedProductIds.ToList();
        }
    }
}
