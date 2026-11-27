using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Module.Core.Services
{
    /// <summary>
    /// Provides lightweight product information without creating circular references
    /// </summary>
    public interface IProductPriceProvider
    {
        /// <summary>
        /// Gets product pricing information by product ID
        /// </summary>
        Task<decimal?> GetProductPriceAsync(long productId);

        /// <summary>
        /// Gets complete product information by product ID
        /// </summary>
        Task<ProductInfoDto> GetProductInfoAsync(long productId);

        /// <summary>
        /// Searches assignable products for vendor product assignment.
        /// Includes simple products and catalog variation products.
        /// </summary>
        Task<IList<ProductInfoDto>> SearchProductsAsync(string name, IEnumerable<long> excludedProductIds = null, int take = 5);
    }

    /// <summary>
    /// Lightweight product data transfer object
    /// </summary>
    public class ProductInfoDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Sku { get; set; }
        public bool HasOptions { get; set; }
        public bool IsVariation { get; set; }
        public long? ParentProductId { get; set; }
        public IList<ProductVariationOptionInfoDto> VariationOptions { get; set; } = new List<ProductVariationOptionInfoDto>();
        public IList<ProductOptionInfoDto> Options { get; set; } = new List<ProductOptionInfoDto>();
    }

    public class ProductVariationOptionInfoDto
    {
        public long OptionId { get; set; }
        public string OptionName { get; set; }
        public string Value { get; set; }
        public int SortIndex { get; set; }
    }

    public class ProductOptionInfoDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string DisplayType { get; set; }
        public int SortIndex { get; set; }
        public IList<ProductOptionValueInfoDto> Values { get; set; } = new List<ProductOptionValueInfoDto>();
    }

    public class ProductOptionValueInfoDto
    {
        public string Key { get; set; }
        public string Display { get; set; }
    }
}
