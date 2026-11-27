using System.Linq;
using System.Threading.Tasks;
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
    [Authorize(Roles = "admin, vendor")]
    [Route("api/invoices")]
    public class InvoiceApiController :ControllerBase
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IWorkContext _workContext;

        private readonly IPdfConverter _pdfConverter;
        private readonly ICurrencyService _currencyService;
        private readonly IRepository<VendorProduct> _vendorProductRepository;

        public InvoiceApiController(IRepository<Order> orderRepository, IWorkContext workContext, IPdfConverter pdfConverter, ICurrencyService currencyService, IRepository<VendorProduct> vendorProductRepository)
        {
            _orderRepository = orderRepository;
            _workContext = workContext;
            _pdfConverter = pdfConverter;
            _currencyService = currencyService;
            _vendorProductRepository = vendorProductRepository;
        }

        [HttpGet("print/{id}")]
        public async Task<IActionResult> Print(long id)
        {
            var order = await _orderRepository
                .Query()
                .Include(x => x.ShippingAddress).ThenInclude(x => x.District)
                .Include(x => x.ShippingAddress).ThenInclude(x => x.StateOrProvince)
                .Include(x => x.ShippingAddress).ThenInclude(x => x.Country)
                .Include(x => x.OrderItems).ThenInclude(x => x.Product)
                .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.LinkedProductLinks)
                .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.OptionCombinations).ThenInclude(x => x.Option)
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x => x.Id == id);

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
                    VendorProductId = x.VendorProductId,
                    VendorProductVariationId = x.VendorProductVariationId,
                    VendorAssignment = GetVendorAssignment(x),
                    ProductName = x.Product.Name,
                    VariationName = GetVariationName(x),
                    ProductPrice = x.ProductPrice,
                    Quantity = x.Quantity,
                    DiscountAmount = x.DiscountAmount,
                    TaxAmount = x.TaxAmount,
                    TaxPercent = x.TaxPercent,
                    VariationOptions = OrderItemVm.GetVariationOption(x.Product)
                }).ToList()
            };

            // Previously rendered a Razor view to HTML. Razor view rendering has been removed.
            // Create a minimal HTML representation of the invoice to feed the PDF converter.
            var invoiceHtml = $"<html><head><meta charset=\"utf-8\"/><title>Invoice {id}</title></head><body>" +
                              $"<h1>Invoice #{model.Id}</h1>" +
                              $"<p>Customer: {model.CustomerName} ({model.CustomerEmail})</p>" +
                              $"<p>Order Total: {model.OrderTotal:C}</p>" +
                              "<h2>Items</h2><ul>" +
                              string.Join("", model.OrderItems.Select(i => $"<li>{i.ProductName}{(string.IsNullOrWhiteSpace(i.VariationName) ? string.Empty : $" ({i.VariationName})")} - {i.Quantity} x {i.ProductPrice:C} = {(i.ProductPrice * i.Quantity):C}</li>")) +
                              "</ul></body></html>";

            byte[] pdf = _pdfConverter.Convert(invoiceHtml);
            return File(pdf, "application/pdf", $"Invoice-{id}.pdf");
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
