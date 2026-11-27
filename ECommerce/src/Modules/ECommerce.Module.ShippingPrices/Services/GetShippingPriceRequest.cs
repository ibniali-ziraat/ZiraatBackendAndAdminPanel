using System.Collections.Generic;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Shipping.Models;

namespace ECommerce.Module.ShippingPrices.Services
{
    public class GetShippingPriceRequest
    {
        public Address ShippingAddress { get; set; }

        public Address WarehouseAddress { get; set; }

        public IList<ShippingItem> ShippingItem { get; set; } = new List<ShippingItem>();

        public decimal OrderAmount { get; set; }
    }
}
