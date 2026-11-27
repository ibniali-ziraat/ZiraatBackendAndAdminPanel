using System;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.ProductRecentlyViewed.Models
{
    public class RecentlyViewedProduct : EntityBase
    {
        public long UserId { get; set; }

        public long ProductId { get; set; }

        public DateTimeOffset LatestViewedOn { get; set; }
    }
}
