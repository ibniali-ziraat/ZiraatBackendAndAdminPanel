using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class ProductMajorBrand : EntityBase
    {
        // Id inherited from EntityBase
        public long ProductPortfolioId { get; set; }

        public string BrandName { get; set; }

        public ProductPortfolio ProductPortfolio { get; set; }
    }
}
