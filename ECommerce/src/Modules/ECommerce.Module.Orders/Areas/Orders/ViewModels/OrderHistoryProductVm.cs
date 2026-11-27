using System.Collections.Generic;
using ECommerce.Module.Core.ViewModels;

namespace ECommerce.Module.Orders.Areas.Orders.ViewModels
{
    public class OrderHistoryProductVm
    {
        public long ProductId { get; set; }

        public long? VendorId { get; set; }

        public long? VendorProductId { get; set; }

        public long? VendorProductVariationId { get; set; }

        public VendorProductAssignmentPricingVm VendorAssignment { get; set; }

        public string ProductName { get; set; }

        public string VariationName { get; set; }

        public IEnumerable<string> ProductOptions { get; set; } = new List<string>();

        public string ProductOptionString
        {
            get { return string.Join(",", ProductOptions); }
        }

        public int Quantity { get; set; }

        public string ThumbnailImage { get; set; }
    }
}
