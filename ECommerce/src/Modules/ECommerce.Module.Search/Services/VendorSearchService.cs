using System.Linq;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Localization;
using ECommerce.Module.Catalog.Areas.Catalog.ViewModels;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Search.Areas.Search.ViewModels;
using Microsoft.EntityFrameworkCore;

public interface IVendorSearchService
{
    Task<SearchResult> SearchProductsByVendorAsync(
        long vendorId,
        SearchOption searchOption,
        int pageSize
    );
}

public class VendorSearchService : IVendorSearchService
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Brand> _brandRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<LocalizedContentProperty> _localizedContentPropertyRepository;

    public VendorSearchService(
        IRepository<Product> productRepository,
        IRepository<Brand> brandRepository,
        IRepository<Category> categoryRepository,
        IRepository<LocalizedContentProperty> localizedContentPropertyRepository
    )
    {
        _productRepository = productRepository;
        _brandRepository = brandRepository;
        _categoryRepository = categoryRepository;
        _localizedContentPropertyRepository = localizedContentPropertyRepository;
    }

    public async Task<SearchResult> SearchProductsByVendorAsync(
        long vendorId,
        SearchOption searchOption,
        int pageSize
    )
    {
        var model = new SearchResult
        {
            CurrentSearchOption = searchOption,
            FilterOption = new FilterOption(),
        };

        var matchedLocalizedProductIds = _localizedContentPropertyRepository
            .Query()
            .Where(x =>
                x.EntityType == nameof(Product)
                && x.Value.ToLower().Contains(searchOption.Query.ToLower())
            )
            .Select(x => x.EntityId)
            .Distinct()
            .ToList();

        var queryable = _productRepository
            .Query()
            .Where(x =>
                x.IsPublished
                && x.IsVisibleIndividually
                && x.VendorId == vendorId
                && (
                    matchedLocalizedProductIds.Contains(x.Id)
                    || x.Name.ToLower().Contains(searchOption.Query.ToLower())
                    || x.ShortDescription.ToLower().Contains(searchOption.Query.ToLower())
                    || x.Description.ToLower().Contains(searchOption.Query.ToLower())
                    || x.Specification.ToLower().Contains(searchOption.Query.ToLower())
                )
            );

        if (!queryable.Any())
        {
            model.TotalProduct = 0;
            return model;
        }

        // You can add filter options logic here if needed

        if (searchOption.MinPrice.HasValue)
        {
            queryable = queryable.Where(x => x.Price >= searchOption.MinPrice.Value);
        }

        if (searchOption.MaxPrice.HasValue)
        {
            queryable = queryable.Where(x => x.Price <= searchOption.MaxPrice.Value);
        }

        if (
            string.Compare(searchOption.Category, "all", System.StringComparison.OrdinalIgnoreCase)
            != 0
        )
        {
            var categories = searchOption.GetCategories().ToArray();
            if (categories.Any())
            {
                queryable = queryable.Where(x =>
                    x.Categories.Any(c => categories.Contains(c.Category.Slug))
                );
            }
        }

        var brands = searchOption.GetBrands().ToArray();
        if (brands.Any())
        {
            queryable = queryable.Where(x => x.BrandId.HasValue && brands.Contains(x.Brand.Slug));
        }

        model.TotalProduct = queryable.Count();
        var currentPageNum = searchOption.Page <= 0 ? 1 : searchOption.Page;
        var offset = (pageSize * currentPageNum) - pageSize;
        while (currentPageNum > 1 && offset >= model.TotalProduct)
        {
            currentPageNum--;
            offset = (pageSize * currentPageNum) - pageSize;
        }

        var products = queryable
            .Include(x => x.ThumbnailImage)
            .Skip(offset)
            .Take(pageSize)
            .Select(x => ProductThumbnail.FromProduct(x))
            .ToList();

        model.Products = products;
        model.CurrentSearchOption.PageSize = pageSize;
        model.CurrentSearchOption.Page = currentPageNum;

        return model;
    }
}
