using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Module.Core.Services
{
    /// <summary>
    /// Abstraction for Order service to avoid circular dependency
    /// </summary>
    public interface IOrderService
    {
        Task<decimal> GetCustomerTotalOrderAmount(long vendorId, long customerId);

        Task<decimal> GetCustomerTotalSettledAmount(long vendorId, long customerId);

        Task<List<OrderDto>> GetCustomerOrders(long vendorId, long customerId);

        Task<List<OrderDto>> GetVendorOrders(long vendorId);
    }

    public class OrderDto
    {
        public long OrderId { get; set; }

        public string ItemName { get; set; }

        public long? VendorId { get; set; }

        public long? VendorProductId { get; set; }

        public long? VendorProductVariationId { get; set; }

        public string VariationName { get; set; }

        public long CustomerId { get; set; }

        public string CustomerName { get; set; }

        public int ItemQuantity { get; set; }

        public decimal ItemAmount { get; set; }

        public decimal ItemTaxAmount { get; set; }

        public decimal ItemTaxPercent { get; set; }

        public DateTime OrderedDate { get; set; }

        public decimal OrderAmount { get; set; }

        public string OrderStatus { get; set; }

        public string PaymentMethod { get; set; }
    }
}
