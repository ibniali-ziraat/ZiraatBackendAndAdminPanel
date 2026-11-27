using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Catalog.Services
{
    public class ProductPriceProvider : IProductPriceProvider
    {
        private readonly IRepository<Product> _productRepository;

        public ProductPriceProvider(IRepository<Product> productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<decimal?> GetProductPriceAsync(long productId)
        {
            var product = await _productRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == productId);

            return product?.Price;
        }

        public async Task<ProductInfoDto> GetProductInfoAsync(long productId)
        {
            var product = await _productRepository.Query()
                .Include(x => x.LinkedProductLinks)
                .Include(x => x.OptionValues).ThenInclude(x => x.Option)
                .Include(x => x.OptionCombinations).ThenInclude(x => x.Option)
                .FirstOrDefaultAsync(x => x.Id == productId);

            if (product == null)
            {
                return null;
            }

            return MapProductInfo(product);
        }

        public async Task<IList<ProductInfoDto>> SearchProductsAsync(string name, IEnumerable<long> excludedProductIds = null, int take = 5)
        {
            var excludedIds = (excludedProductIds ?? Enumerable.Empty<long>()).ToList();
            var searchTerm = name?.Trim();
            var query = _productRepository.Query()
                .Include(x => x.LinkedProductLinks)
                .Include(x => x.OptionValues).ThenInclude(x => x.Option)
                .Include(x => x.OptionCombinations).ThenInclude(x => x.Option)
                .Where(x => !x.IsDeleted && x.IsPublished && x.IsAllowToOrder);

            if (excludedIds.Any())
            {
                query = query.Where(x => !excludedIds.Contains(x.Id));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x =>
                    x.Name.Contains(searchTerm) ||
                    (x.Sku != null && x.Sku.Contains(searchTerm)) ||
                    x.OptionValues.Any(option =>
                        (option.Value != null && option.Value.Contains(searchTerm)) ||
                        (option.Option != null && option.Option.Name.Contains(searchTerm))) ||
                    x.OptionCombinations.Any(option =>
                        (option.Value != null && option.Value.Contains(searchTerm)) ||
                        (option.Option != null && option.Option.Name.Contains(searchTerm))));
            }

            var products = await query
                .OrderByDescending(x => x.IsVisibleIndividually)
                .ThenBy(x => x.Name)
                .Take(take)
                .ToListAsync();

            return products.Select(MapProductInfo).ToList();
        }

        private static ProductInfoDto MapProductInfo(Product product)
        {
            var superLink = product.LinkedProductLinks
                .FirstOrDefault(x => x.LinkType == ProductLinkType.Super);

            return new ProductInfoDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Sku = product.Sku,
                HasOptions = product.HasOptions,
                IsVariation = superLink != null,
                ParentProductId = superLink?.ProductId,
                VariationOptions = product.OptionCombinations
                    .OrderBy(x => x.SortIndex)
                    .Select(x => new ProductVariationOptionInfoDto
                    {
                        OptionId = x.OptionId,
                        OptionName = x.Option?.Name,
                        Value = x.Value,
                        SortIndex = x.SortIndex
                    })
                    .ToList(),
                Options = product.OptionValues
                    .OrderBy(x => x.SortIndex)
                    .Select(x => new ProductOptionInfoDto
                    {
                        Id = x.OptionId,
                        Name = x.Option?.Name,
                        DisplayType = x.DisplayType,
                        SortIndex = x.SortIndex,
                        Values = DeserializeOptionValues(x.Value)
                    })
                    .ToList()
            };
        }

        private static IList<ProductOptionValueInfoDto> DeserializeOptionValues(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<ProductOptionValueInfoDto>();
            }

            try
            {
                return JsonConvert.DeserializeObject<IList<ProductOptionValueInfoDto>>(value) ?? new List<ProductOptionValueInfoDto>();
            }
            catch (JsonException)
            {
                return new List<ProductOptionValueInfoDto>
                {
                    new ProductOptionValueInfoDto
                    {
                        Key = value,
                        Display = value
                    }
                };
            }
        }
    }
}
