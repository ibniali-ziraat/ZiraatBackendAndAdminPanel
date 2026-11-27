using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Inventory.Models
{
    public class ProductBackInStockSubscription : EntityBase
    {
        public long ProductId { get; set; }

        public string CustomerEmail { get; set; }
    }
}
