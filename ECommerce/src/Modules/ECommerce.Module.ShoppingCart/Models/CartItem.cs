using System;
using ECommerce.Infrastructure.Models;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.ShoppingCart.Models
{
    public class CartItem : EntityBase
    {
        public CartItem() 
        {
            CreatedOn = DateTimeOffset.Now;
            LatestUpdatedOn = DateTimeOffset.Now;
        }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }

        public long ProductId { get; set; }

        public Product Product { get; set; }

        public int Quantity { get; set; }

        public long CustomerId { get; set; }

        public User Customer { get; set; }

        public long? VendorId { get; set; }

        public long? VendorProductId { get; set; }

        public long? VendorProductVariationId { get; set; }
    }
}
