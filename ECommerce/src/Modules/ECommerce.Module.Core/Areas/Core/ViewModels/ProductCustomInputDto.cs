using System.Collections.Generic;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
   
    public class ProductPortfolioViewModel
    {
        public long? Id { get; set; }

        public long SupplierProfileId { get; set; }

        public string StorageCapacity { get; set; }

        public IList<string> InputTypes { get; set; } = new List<string>();

        public IList<string> MajorBrands { get; set; } = new List<string>();

        public IList<ProductCustomInputDto> CustomInputs { get; set; } = new List<ProductCustomInputDto>();
    }

    
}
