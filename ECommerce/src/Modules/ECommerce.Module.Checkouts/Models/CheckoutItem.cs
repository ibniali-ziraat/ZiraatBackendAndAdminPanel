using System;
using ECommerce.Infrastructure.Models;
using ECommerce.Module.Catalog.Models;

namespace ECommerce.Module.Checkouts.Models
{
    public class CheckoutItem : EntityBase
    {
        public DateTimeOffset CreatedOn { get; set; }

        public long ProductId { get; set; }

        public Product Product { get; set; }

        public long? VendorId { get; set; }

        public long? VendorProductId { get; set; }

        public long? VendorProductVariationId { get; set; }

        public int Quantity { get; set; }

        public Guid CheckoutId { get; set; }

        public Checkout Checkout { get; set; }
    }
}
