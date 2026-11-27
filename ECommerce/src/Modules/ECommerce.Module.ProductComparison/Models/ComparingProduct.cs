using System;
using ECommerce.Infrastructure.Models;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.ProductComparison.Models
{
    public class ComparingProduct : EntityBase
    {
        public DateTimeOffset CreatedOn { get; set; }

        public long UserId { get; set; }

        public User User { get; set; }

        public long ProductId { get; set; }

        public Product Product { get; set; }
    }
}
