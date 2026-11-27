using System.Collections.Generic;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class ProductPortfolio : EntityBase
    {
        // Id is inherited from EntityBase
        public long Id { get; set; }
        public long SupplierProfileId { get; set; }

        // StorageCapacityValue will be used to store the incoming "storage_capacity" string
        public string StorageCapacityValue { get; set; }

        public string StorageCapacityUnit { get; set; }

        public IList<ProductInputType> InputTypes { get; set; } = new List<ProductInputType>();

        public IList<ProductMajorBrand> MajorBrands { get; set; } = new List<ProductMajorBrand>();

        public IList<ProductCustomInput> CustomInputs { get; set; } = new List<ProductCustomInput>();
    }
}
