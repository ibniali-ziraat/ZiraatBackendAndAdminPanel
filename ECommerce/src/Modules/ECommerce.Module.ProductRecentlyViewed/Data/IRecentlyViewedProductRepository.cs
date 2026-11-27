using System.Linq;
using ECommerce.Module.Catalog.Models;

namespace ECommerce.Module.ProductRecentlyViewed.Data
{
    public interface IRecentlyViewedProductRepository
    {
        IQueryable<Product> GetRecentlyViewedProduct(long userId);
    }
}
