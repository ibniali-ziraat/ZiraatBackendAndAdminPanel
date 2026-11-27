using System.Collections.Generic;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class ProductInputTypeDto
    {
        public long Id { get; set; }
        public string InputType { get; set; }
    }

    public class ProductMajorBrandDto
    {
        public long Id { get; set; }
        public string BrandName { get; set; }
    }

    public class ProductCustomInputDto
    {
        public long Id { get; set; }
        public string ProductName { get; set; }
        public string StockQuantity { get; set; }
        public string Unit { get; set; }
    }

    public class ProductPortfolioResponseVm
    {
        public long Id { get; set; }
        public long SupplierProfileId { get; set; }
        public string StorageCapacityValue { get; set; }
        public string StorageCapacityUnit { get; set; }

        public IList<ProductInputTypeDto> InputTypes { get; set; } = new List<ProductInputTypeDto>();
        public IList<ProductMajorBrandDto> MajorBrands { get; set; } = new List<ProductMajorBrandDto>();
        public IList<ProductCustomInputDto> CustomInputs { get; set; } = new List<ProductCustomInputDto>();
    }
}