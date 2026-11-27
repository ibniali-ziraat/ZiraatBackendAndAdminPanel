using System;
using ECommerce.Infrastructure.Models;
using ECommerce.Module.Catalog.Models;

namespace ECommerce.Module.WishList.Models
{
    public class WishListItem : EntityBase
    {
        public WishListItem()
        {
            CreatedOn = DateTimeOffset.Now;
            LatestUpdatedOn = DateTimeOffset.Now;
        }

        public long WishListId { get; set; }

        public WishList WishList { get; set; }

        public long ProductId { get; set; }

        public Product Product { get; set; }

        public long? VendorId { get; set; }

        public long? VendorProductId { get; set; }

        public string Description { get; set; }

        public int Quantity { get; set;}

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }
    }
}
