using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Helpers;
using ECommerce.Infrastructure.Web.SmartTable;
using ECommerce.Module.Checkouts.Areas.Checkouts.ViewModels;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Core.ViewModels;
using ECommerce.Module.Orders.Areas.Orders.ViewModels;
using ECommerce.Module.Orders.Events;
using ECommerce.Module.Orders.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Orders.Areas.Orders.Controllers
{
    [Area("Orders")]
    [Authorize(Roles = "admin, vendor, customer")]
    [Route("api/orders")]
    public class OrderApiController : ControllerBase
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;
        private readonly ICurrencyService _currencyService;
        private readonly IMediaService _mediaService;
        private readonly IRepository<VendorProduct> _vendorProductRepository;

        public OrderApiController(
            IRepository<Order> orderRepository,
            IWorkContext workContext,
            IMediator mediator,
            ICurrencyService currencyService,
            IMediaService mediaService,
            IRepository<VendorProduct> vendorProductRepository
        )
        {
            _orderRepository = orderRepository;
            _workContext = workContext;
            _mediator = mediator;
            _currencyService = currencyService;
            _mediaService = mediaService;
            _vendorProductRepository = vendorProductRepository;
        }

        [HttpGet]
        public async Task<ActionResult> Get(int status, int numRecords)
        {
            var orderStatus = (OrderStatus)status;
            if ((numRecords <= 0) || (numRecords > 100))
            {
                numRecords = 5;
            }

            var query = _orderRepository.Query();
            if (orderStatus != 0)
            {
                query = query.Where(x => x.OrderStatus == orderStatus);
            }

            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin"))
            {
                query = query.Where(x => x.VendorId == currentUser.VendorId);
            }

            var model = query
                .OrderByDescending(x => x.CreatedOn)
                .Take(numRecords)
                .Select(x => new
                {
                    x.Id,
                    CustomerName = x.Customer.FullName,
                    x.OrderTotal,
                    OrderTotalString = _currencyService.FormatCurrency(x.OrderTotal),
                    OrderStatus = x.OrderStatus.ToString(),
                    x.CreatedOn,
                });

            return new JsonResult(model);
        }

        [HttpPost("grid")]
        public async Task<ActionResult> List([FromBody] SmartTableParam param)
        {
            var query = _orderRepository.Query();

            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin"))
            {
                query = query.Where(x => x.VendorId == currentUser.VendorId);
            }

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.Id != null)
                {
                    long id = search.Id;
                    query = query.Where(x => x.Id == id);
                }

                if (search.Status != null)
                {
                    var status = (OrderStatus)search.Status;
                    query = query.Where(x => x.OrderStatus == status);
                }

                if (search.CustomerName != null)
                {
                    string customerName = search.CustomerName;
                    query = query.Where(x => x.Customer.FullName.Contains(customerName));
                }

                if (search.CreatedOn != null)
                {
                    if (search.CreatedOn.before != null)
                    {
                        DateTimeOffset before = search.CreatedOn.before;
                        query = query.Where(x => x.CreatedOn <= before);
                    }

                    if (search.CreatedOn.after != null)
                    {
                        DateTimeOffset after = search.CreatedOn.after;
                        query = query.Where(x => x.CreatedOn >= after);
                    }
                }
            }

            var orders = query.ToSmartTableResult(
                param,
                order => new
                {
                    order.Id,
                    CustomerName = order.Customer.FullName,
                    order.OrderTotal,
                    OrderTotalString = _currencyService.FormatCurrency(order.OrderTotal),
                    OrderStatus = order.OrderStatus.ToString(),
                    order.CreatedOn,
                }
            );

            return new JsonResult(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var order = _orderRepository
                .Query()
                .Include(x => x.ShippingAddress)
                .ThenInclude(x => x.District)
                .Include(x => x.ShippingAddress)
                .ThenInclude(x => x.StateOrProvince)
                .Include(x => x.ShippingAddress)
                .ThenInclude(x => x.Country)
                .Include(x => x.OrderItems)
                .ThenInclude(x => x.Product)
                .ThenInclude(x => x.ThumbnailImage)
                .Include(x => x.OrderItems)
                .ThenInclude(x => x.Product)
                .ThenInclude(x => x.Medias)
                .ThenInclude(x => x.Media)
                .Include(x => x.OrderItems)
                .ThenInclude(x => x.Product)
                .ThenInclude(x => x.LinkedProductLinks)
                .Include(x => x.OrderItems)
                .ThenInclude(x => x.Product)
                .ThenInclude(x => x.OptionCombinations)
                .ThenInclude(x => x.Option)
                .Include(x => x.Customer)
                .FirstOrDefault(x => x.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin") && order.VendorId != currentUser.VendorId)
            {
                return BadRequest(new { error = "You don't have permission to manage this order" });
            }

            var model = new OrderDetailVm(_currencyService)
            {
                Id = order.Id,
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
                    Phone = order.ShippingAddress.Phone,
                },
                OrderItems = order
                    .OrderItems.Select(x => new OrderItemVm(_currencyService)
                    {
                        Id = x.Id,
                        ProductId = x.Product.Id,
                        VendorId = x.VendorId,
                        VendorProductId = x.VendorProductId,
                        VendorProductVariationId = x.VendorProductVariationId,
                        VendorAssignment = GetVendorAssignment(x),
                        ProductName = x.Product.Name,
                        VariationName = GetVariationName(x),
                        ProductImage = _mediaService.GetThumbnailUrl(x.Product.ThumbnailImage),
                        Images = MapProductImages(x.Product),
                        ProductPrice = x.ProductPrice,
                        Quantity = x.Quantity,
                        DiscountAmount = x.DiscountAmount,
                        TaxAmount = x.TaxAmount,
                        TaxPercent = x.TaxPercent,
                        VariationOptions = OrderItemVm.GetVariationOption(x.Product),
                    })
                    .ToList(),
            };

            if (order.IsMasterOrder)
            {
                model.SubOrderIds = _orderRepository
                    .Query()
                    .Where(x => x.ParentId == order.Id)
                    .Select(x => x.Id)
                    .ToList();
            }

            await _mediator.Publish(new OrderDetailGot { OrderDetailVm = model });

            return new JsonResult(model);
        }

        private IList<MediaViewModel> MapProductImages(
            ECommerce.Module.Catalog.Models.Product product
        )
        {
            var images =
                product
                    ?.Medias?.Where(x => x.Media?.MediaType == MediaType.Image)
                    .Select(productMedia => new MediaViewModel
                    {
                        Url = _mediaService.GetMediaUrl(productMedia.Media),
                        ThumbnailUrl = _mediaService.GetThumbnailUrl(productMedia.Media),
                    })
                    .ToList() ?? new List<MediaViewModel>();

            if (!images.Any())
            {
                images.Add(
                    new MediaViewModel
                    {
                        Url = _mediaService.GetMediaUrl(product?.ThumbnailImage),
                        ThumbnailUrl = _mediaService.GetThumbnailUrl(product?.ThumbnailImage),
                    }
                );
            }

            return images;
        }

        [HttpPost("change-order-status/{id}")]
        public async Task<IActionResult> ChangeStatus(long id, [FromBody] OrderStatusForm model)
        {
            var order = _orderRepository.Query().FirstOrDefault(x => x.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin") && order.VendorId != currentUser.VendorId)
            {
                return BadRequest(new { error = "You don't have permission to manage this order" });
            }

            if (Enum.IsDefined(typeof(OrderStatus), model.StatusId))
            {
                var oldStatus = order.OrderStatus;
                var newStatus = (OrderStatus)model.StatusId;
                if (oldStatus == newStatus)
                {
                    return Accepted();
                }

                order.OrderStatus = newStatus;
                await _orderRepository.SaveChangesAsync();

                var orderStatusChanged = new OrderChanged
                {
                    OrderId = order.Id,
                    OldStatus = oldStatus,
                    NewStatus = order.OrderStatus,
                    Order = order,
                    UserId = currentUser.Id,
                    Note = model.Note,
                };

                await _mediator.Publish(orderStatusChanged);
                return Accepted();
            }

            return BadRequest(new { Error = "unsupported order status" });
        }

        [HttpGet("order-status")]
        public IActionResult GetOrderStatus()
        {
            var model = EnumHelper
                .ToDictionary(typeof(OrderStatus))
                .Select(x => new { Id = x.Key, Name = x.Value });
            return new JsonResult(model);
        }

        [HttpPost("export")]
        public async Task<ActionResult<OrderExportVm>> Export([FromBody] SmartTableParam param)
        {
            var query = _orderRepository.Query();

            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin"))
            {
                query = query.Where(x => x.VendorId == currentUser.VendorId);
            }

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.Id != null)
                {
                    long id = search.Id;
                    query = query.Where(x => x.Id == id);
                }

                if (search.Status != null)
                {
                    var status = (OrderStatus)search.Status;
                    query = query.Where(x => x.OrderStatus == status);
                }

                if (search.CustomerName != null)
                {
                    string customerName = search.CustomerName;
                    query = query.Where(x => x.Customer.FullName.Contains(customerName));
                }

                if (search.CreatedOn != null)
                {
                    if (search.CreatedOn.before != null)
                    {
                        DateTimeOffset before = search.CreatedOn.before;
                        query = query.Where(x => x.CreatedOn <= before);
                    }

                    if (search.CreatedOn.after != null)
                    {
                        DateTimeOffset after = search.CreatedOn.after;
                        query = query.Where(x => x.CreatedOn >= after);
                    }
                }
            }

            var orders = await query
                .Select(x => new OrderExportVm
                {
                    Id = x.Id,
                    OrderStatus = (int)x.OrderStatus,
                    IsMasterOrder = x.IsMasterOrder,
                    DiscountAmount = x.DiscountAmount,
                    CreatedOn = x.CreatedOn,
                    OrderStatusString = x.OrderStatus.GetDisplayName(),
                    PaymentFeeAmount = x.PaymentFeeAmount,
                    OrderTotal = x.OrderTotal,
                    Subtotal = x.SubTotal,
                    SubtotalWithDiscount = x.SubTotalWithDiscount,
                    PaymentMethod = x.PaymentMethod,
                    ShippingAmount = x.ShippingFeeAmount,
                    ShippingMethod = x.ShippingMethod,
                    TaxAmount = x.TaxAmount,
                    CustomerId = x.CustomerId,
                    CustomerName = x.Customer.FullName,
                    CustomerEmail = x.Customer.Email,
                    LatestUpdatedOn = x.LatestUpdatedOn,
                    Coupon = x.CouponCode,
                    Items = x.OrderItems.Count(),
                    BillingAddressId = x.BillingAddressId,
                    BillingAddressAddressLine1 = x.BillingAddress.AddressLine1,
                    BillingAddressAddressLine2 = x.BillingAddress.AddressLine2,
                    BillingAddressContactName = x.BillingAddress.ContactName,
                    BillingAddressCountryName = x.BillingAddress.Country.Name,
                    BillingAddressDistrictName = x.BillingAddress.District.Name,
                    BillingAddressZipCode = x.BillingAddress.ZipCode,
                    BillingAddressPhone = x.BillingAddress.Phone,
                    BillingAddressStateOrProvinceName = x.BillingAddress.StateOrProvince.Name,
                    ShippingAddressAddressLine1 = x.ShippingAddress.AddressLine1,
                    ShippingAddressAddressLine2 = x.ShippingAddress.AddressLine2,
                    ShippingAddressId = x.ShippingAddressId,
                    ShippingAddressContactName = x.ShippingAddress.ContactName,
                    ShippingAddressCountryName = x.ShippingAddress.Country.Name,
                    ShippingAddressDistrictName = x.ShippingAddress.District.Name,
                    ShippingAddressPhone = x.ShippingAddress.Phone,
                    ShippingAddressStateOrProvinceName = x.ShippingAddress.StateOrProvince.Name,
                    ShippingAddressZipCode = x.ShippingAddress.ZipCode,
                })
                .ToListAsync();

            foreach (var order in orders)
            {
                order.SubtotalString = _currencyService.FormatCurrency(order.Subtotal);
                order.DiscountAmountString = _currencyService.FormatCurrency(order.DiscountAmount);
                order.SubtotalWithDiscountString = _currencyService.FormatCurrency(
                    order.SubtotalWithDiscount
                );
                order.TaxAmountString = _currencyService.FormatCurrency(order.TaxAmount);
                order.ShippingAmountString = _currencyService.FormatCurrency(order.ShippingAmount);
                order.PaymentFeeAmountString = _currencyService.FormatCurrency(
                    order.PaymentFeeAmount
                );
                order.OrderTotalString = _currencyService.FormatCurrency(order.OrderTotal);
            }

            var csvString = CsvConverter.ExportCsv(orders);
            var csvBytes = Encoding.UTF8.GetBytes(csvString);
            // MS Excel need the BOM to display UTF8 Correctly
            var csvBytesWithUTF8BOM = Encoding.UTF8.GetPreamble().Concat(csvBytes).ToArray();
            return File(csvBytesWithUTF8BOM, "text/csv", "orders-export.csv");
        }

        [HttpPost("lines-export")]
        public async Task<ActionResult<OrderLineExportVm>> OrderLinesExport(
            [FromBody] SmartTableParam param,
            [FromServices] IRepository<OrderItem> orderItemRepository
        )
        {
            var query = orderItemRepository.Query();

            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin"))
            {
                query = query.Where(x => x.Order.VendorId == currentUser.VendorId);
            }

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.Id != null)
                {
                    long id = search.Id;
                    query = query.Where(x => x.Id == id);
                }

                if (search.Status != null)
                {
                    var status = (OrderStatus)search.Status;
                    query = query.Where(x => x.Order.OrderStatus == status);
                }

                if (search.CustomerName != null)
                {
                    string customerName = search.CustomerName;
                    query = query.Where(x => x.Order.Customer.FullName.Contains(customerName));
                }

                if (search.CreatedOn != null)
                {
                    if (search.CreatedOn.before != null)
                    {
                        DateTimeOffset before = search.CreatedOn.before;
                        query = query.Where(x => x.Order.CreatedOn <= before);
                    }

                    if (search.CreatedOn.after != null)
                    {
                        DateTimeOffset after = search.CreatedOn.after;
                        query = query.Where(x => x.Order.CreatedOn >= after);
                    }
                }
            }

            var orderItems = await query
                .Select(x => new OrderLineExportVm()
                {
                    Id = x.Id,
                    OrderStatus = (int)x.Order.OrderStatus,
                    IsMasterOrder = x.Order.IsMasterOrder,
                    DiscountAmount = x.Order.DiscountAmount,
                    CreatedOn = x.Order.CreatedOn,
                    OrderStatusString = x.Order.OrderStatus.ToString(),
                    PaymentFeeAmount = x.Order.PaymentFeeAmount,
                    OrderTotal = x.Order.OrderTotal,
                    Subtotal = x.Order.SubTotal,
                    SubtotalWithDiscount = x.Order.SubTotalWithDiscount,
                    PaymentMethod = x.Order.PaymentMethod,
                    ShippingAmount = x.Order.ShippingFeeAmount,
                    ShippingMethod = x.Order.ShippingMethod,
                    TaxAmount = x.Order.TaxAmount,
                    CustomerId = x.Order.CustomerId,
                    CustomerName = x.Order.Customer.FullName,
                    CustomerEmail = x.Order.Customer.Email,
                    LatestUpdatedOn = x.Order.LatestUpdatedOn,
                    Coupon = x.Order.CouponCode,
                    Items = x.Order.OrderItems.Count(),
                    BillingAddressId = x.Order.BillingAddressId,
                    BillingAddressAddressLine1 = x.Order.BillingAddress.AddressLine1,
                    BillingAddressAddressLine2 = x.Order.BillingAddress.AddressLine2,
                    BillingAddressContactName = x.Order.BillingAddress.ContactName,
                    BillingAddressCountryName = x.Order.BillingAddress.Country.Name,
                    BillingAddressDistrictName = x.Order.BillingAddress.District.Name,
                    BillingAddressZipCode = x.Order.BillingAddress.ZipCode,
                    BillingAddressPhone = x.Order.BillingAddress.Phone,
                    BillingAddressStateOrProvinceName = x.Order.BillingAddress.StateOrProvince.Name,
                    ShippingAddressAddressLine1 = x.Order.ShippingAddress.AddressLine1,
                    ShippingAddressAddressLine2 = x.Order.ShippingAddress.AddressLine2,
                    ShippingAddressId = x.Order.ShippingAddressId,
                    ShippingAddressContactName = x.Order.ShippingAddress.ContactName,
                    ShippingAddressCountryName = x.Order.ShippingAddress.Country.Name,
                    ShippingAddressDistrictName = x.Order.ShippingAddress.District.Name,
                    ShippingAddressPhone = x.Order.ShippingAddress.Phone,
                    ShippingAddressStateOrProvinceName = x.Order
                        .ShippingAddress
                        .StateOrProvince
                        .Name,
                    ShippingAddressZipCode = x.Order.ShippingAddress.ZipCode,
                    OrderLineDiscountAmount = x.DiscountAmount,
                    OrderLineQuantity = x.Quantity,
                    OrderLineTaxAmount = x.TaxAmount,
                    OrderLineTaxPercent = x.TaxPercent,
                    OrderLineId = x.Id,
                    ProductId = x.ProductId,
                    VendorId = x.VendorId,
                    VendorProductId = x.VendorProductId,
                    VendorProductVariationId = x.VendorProductVariationId,
                    ProductName = x.Product.Name,
                    ProductPrice = x.ProductPrice,
                })
                .ToListAsync();

            var orderLineIds = orderItems.Select(x => x.OrderLineId).ToList();
            var orderItemsById = await orderItemRepository
                .Query()
                .Include(x => x.Product)
                .ThenInclude(x => x.LinkedProductLinks)
                .Include(x => x.Product)
                .ThenInclude(x => x.OptionCombinations)
                .Where(x => orderLineIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            foreach (var item in orderItems)
            {
                if (orderItemsById.TryGetValue(item.OrderLineId, out var orderItem))
                {
                    item.VariationName = GetVariationName(orderItem);
                }

                item.SubtotalString = _currencyService.FormatCurrency(item.Subtotal);
                item.DiscountAmountString = _currencyService.FormatCurrency(item.DiscountAmount);
                item.SubtotalWithDiscountString = _currencyService.FormatCurrency(
                    item.SubtotalWithDiscount
                );
                item.TaxAmountString = _currencyService.FormatCurrency(item.TaxAmount);
                item.ShippingAmountString = _currencyService.FormatCurrency(item.ShippingAmount);
                item.PaymentFeeAmountString = _currencyService.FormatCurrency(
                    item.PaymentFeeAmount
                );
                item.OrderTotalString = _currencyService.FormatCurrency(item.OrderTotal);

                item.OrderLineTaxAmountString = _currencyService.FormatCurrency(
                    item.OrderLineTaxAmount
                );
                item.OrderLineProductPriceString = _currencyService.FormatCurrency(
                    item.ProductPrice
                );
                item.OrderLineDiscountAmountString = _currencyService.FormatCurrency(
                    item.OrderLineDiscountAmount
                );
                item.OrderLineTotalString = _currencyService.FormatCurrency(item.OrderLineTotal);
                item.OrderLineRowTotalString = _currencyService.FormatCurrency(
                    item.OrderLineRowTotal
                );
            }

            var csvString = CsvConverter.ExportCsv(orderItems);
            var csvBytes = Encoding.UTF8.GetBytes(csvString);
            // MS Excel need the BOM to display UTF8 Correctly
            var csvBytesWithUTF8BOM = Encoding.UTF8.GetPreamble().Concat(csvBytes).ToArray();
            return File(csvBytesWithUTF8BOM, "text/csv", "order-lines-export.csv");
        }

        private VendorProductAssignmentPricingVm GetVendorAssignment(OrderItem orderItem)
        {
            var vendorProduct = GetVendorProduct(orderItem);
            var vendorVariation = GetVendorProductVariation(vendorProduct, orderItem);
            return VendorProductAssignmentPricingVm.From(
                vendorProduct,
                vendorVariation,
                orderItem.Product?.Price ?? orderItem.ProductPrice
            );
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

            return string.Join(
                ", ",
                vendorVariation
                    .OptionCombinations.OrderBy(x => x.SortIndex)
                    .ThenBy(x => x.OptionId)
                    .Select(x =>
                        string.IsNullOrWhiteSpace(x.OptionName)
                            ? x.Value
                            : $"{x.OptionName}: {x.Value}"
                    )
                    .Where(x => !string.IsNullOrWhiteSpace(x))
            );
        }

        private VendorProduct GetVendorProduct(OrderItem orderItem)
        {
            var assignedProductIds = GetAssignedProductIds(orderItem.Product, orderItem.ProductId);
            var vendorProductQuery = _vendorProductRepository
                .Query()
                .Include(x => x.Variations)
                .ThenInclude(x => x.OptionCombinations)
                .Where(x => assignedProductIds.Contains(x.ProductId));

            return orderItem.VendorProductId.HasValue
                    ? vendorProductQuery.FirstOrDefault(x =>
                        x.Id == orderItem.VendorProductId.Value
                    )
                : orderItem.VendorId.HasValue
                    ? vendorProductQuery.FirstOrDefault(x => x.VendorId == orderItem.VendorId.Value)
                : null;
        }

        private static VendorProductVariation GetVendorProductVariation(
            VendorProduct vendorProduct,
            OrderItem orderItem
        )
        {
            if (vendorProduct == null)
            {
                return null;
            }

            if (orderItem.VendorProductVariationId.HasValue)
            {
                var explicitVariation = vendorProduct.Variations.FirstOrDefault(x =>
                    x.Id == orderItem.VendorProductVariationId.Value && x.IsActive
                );
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
                    x.IsActive
                    && string.Equals(
                        x.NormalizedName?.Trim(),
                        normalizedName,
                        StringComparison.OrdinalIgnoreCase
                    )
                );
                if (matchedByName != null)
                {
                    return matchedByName;
                }
            }

            var productSignature = string.Join(
                "|",
                orderItem
                    .Product.OptionCombinations.Where(x => x != null)
                    .OrderBy(x => x.OptionId)
                    .Select(x =>
                        $"{x.OptionId}:{(x.Value ?? string.Empty).Trim().ToLowerInvariant()}"
                    )
            );

            return vendorProduct.Variations.FirstOrDefault(x =>
                x.IsActive
                && string.Join(
                    "|",
                    x.OptionCombinations.Where(o => o != null)
                        .OrderBy(o => o.OptionId)
                        .Select(o =>
                            $"{o.OptionId}:{(o.Value ?? string.Empty).Trim().ToLowerInvariant()}"
                        )
                ) == productSignature
            );
        }

        private static IList<long> GetAssignedProductIds(
            ECommerce.Module.Catalog.Models.Product product,
            long productId
        )
        {
            var assignedProductIds = new HashSet<long> { productId };
            var parentProductId = product
                ?.LinkedProductLinks?.FirstOrDefault(x =>
                    x.LinkType == ECommerce.Module.Catalog.Models.ProductLinkType.Super
                )
                ?.ProductId;

            if (parentProductId.HasValue)
            {
                assignedProductIds.Add(parentProductId.Value);
            }

            return assignedProductIds.ToList();
        }
    }
}
