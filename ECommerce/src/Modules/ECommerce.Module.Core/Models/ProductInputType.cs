using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class ProductInputType : EntityBase
    {
        // Id inherited from EntityBase
        public long ProductPortfolioId { get; set; }

        public string InputType { get; set; }

        public ProductPortfolio ProductPortfolio { get; set; }
    }
}
