using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class ProductCustomInput : EntityBase
    {
        // Id inherited from EntityBase
        public long Id { get; set; }
        public long ProductPortfolioId { get; set; }

        public string ProductName { get; set; }

        public string StockQuantity { get; set; }

        public string Unit { get; set; }

        public ProductPortfolio ProductPortfolio { get; set; }
    }
}
