using Newtonsoft.Json;
using ECommerce.Infrastructure.Models;
using ECommerce.Module.Catalog.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Module.Orders.Models
{
    public class OrderItem : EntityBase
    {
        [JsonIgnore]
        public Order Order { get; set; }

        public long ProductId { get; set; }

        [JsonIgnore]
        public Product Product { get; set; }

        public long? VendorId { get; set; }

        public long? VendorProductId { get; set; }

        public long? VendorProductVariationId { get; set; }

        [NotMapped]
        public string VariationName { get; set; }

        public decimal ProductPrice { get; set; }

        public int Quantity { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TaxPercent { get; set; }
    }
}
