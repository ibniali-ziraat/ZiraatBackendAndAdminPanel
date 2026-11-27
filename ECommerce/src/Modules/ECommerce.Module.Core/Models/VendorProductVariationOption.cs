using System.ComponentModel.DataAnnotations;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class VendorProductVariationOption : EntityBase
    {
        public long VendorProductVariationId { get; set; }

        public VendorProductVariation VendorProductVariation { get; set; }

        public long OptionId { get; set; }

        [StringLength(450)]
        public string OptionName { get; set; }

        [StringLength(450)]
        public string Value { get; set; }

        public int StockQuantity { get; set; }

        public int SortIndex { get; set; }
    }
}
