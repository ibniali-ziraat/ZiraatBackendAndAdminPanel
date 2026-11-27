using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Localization;
using ECommerce.Module.Catalog.Areas.Catalog.ViewModels;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Catalog.Services;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Module.Search;
using ECommerce.Module.Search.Areas.Search.ViewModels;
using ECommerce.Module.Search.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Module.Search.Areas.Search.Controllers
{
    [Area("Search")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private int _pageSize;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Brand> _brandRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<LocalizedContentProperty> _localizedContentPropertyRepository;
        private readonly IMediaService _mediaService;
        private readonly IRepository<Query> _queryRepository;
        private readonly IProductPricingService _productPricingService;
        private readonly IContentLocalizationService _contentLocalizationService;
        private readonly IVendorSearchService _vendorSearchService;
        private readonly IRepository<VendorCustomerAssociation> _vendorCustomerAssociationRepository;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IRepository<VendorProduct> _vendorProductRepository;

        public SearchController(
            IRepository<Product> productRepository,
            IRepository<Brand> brandRepository,
            IRepository<Category> categoryRepository,
            IRepository<LocalizedContentProperty> localizedContentPropertyRepository,
            IMediaService mediaService,
            IRepository<Query> queryRepository,
            IProductPricingService productPricingService,
            IContentLocalizationService contentLocalizationService,
            IVendorSearchService vendorSearchService,
            IRepository<VendorCustomerAssociation> vendorCustomerAssociationRepository,
            IRepository<Vendor> vendorRepository,
            IRepository<VendorProduct> vendorProductRepository,
            IConfiguration config
        )
        {
            _productRepository = productRepository;
            _brandRepository = brandRepository;
            _categoryRepository = categoryRepository;
            _localizedContentPropertyRepository = localizedContentPropertyRepository;
            _mediaService = mediaService;
            _queryRepository = queryRepository;
            _productPricingService = productPricingService;
            _contentLocalizationService = contentLocalizationService;
            _vendorSearchService = vendorSearchService;
            _vendorCustomerAssociationRepository = vendorCustomerAssociationRepository;
            _vendorRepository = vendorRepository;
            _vendorProductRepository = vendorProductRepository;
            _pageSize = config.GetValue<int>("Catalog.ProductPageSize");
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string query,
            [FromQuery] string brand = null,
            [FromQuery] string category = null,
            [FromQuery] int page = 1,
            [FromQuery] string sort = null,
            [FromQuery] int? minPrice = null,
            [FromQuery] int? maxPrice = null
        )
        {
            var searchOption = new SearchOption
            {
                Query = query + "",
                Brand = brand,
                Category = category,
                Page = page,
                Sort = sort,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
            };
            searchOption.Query = searchOption.Query ?? string.Empty;
            // Case no query
            //if (searchOption.Query is null)
            //{
            //    return BadRequest(new { Message = "Invalid search query" });
            //}

            // Case searching for specific brand
            var brandEntity = _brandRepository
                .Query()
                .FirstOrDefault(x => x.Name == searchOption.Query && x.IsPublished);
            if (brandEntity != null)
            {
                return BadRequest(new { Message = "Invalid brand search" });
            }

            // Case brand included in the query
            var includedBrand = _brandRepository
                .Query()
                .FirstOrDefault(x => searchOption.Query.Contains(x.Name) && x.IsPublished);
            if (includedBrand != null)
            {
                searchOption.Query = searchOption.Query.Replace(
                    includedBrand.Name,
                    "",
                    StringComparison.OrdinalIgnoreCase
                );
            }

            searchOption.Query = searchOption.Query.Trim();

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
                return Ok(model);
            }

            AppendFilterOptionsToModel(model, queryable);

            if (searchOption.MinPrice.HasValue)
            {
                queryable = queryable.Where(x => x.Price >= searchOption.MinPrice.Value);
            }

            if (searchOption.MaxPrice.HasValue)
            {
                queryable = queryable.Where(x => x.Price <= searchOption.MaxPrice.Value);
            }

            if (
                string.Compare(
                    model.CurrentSearchOption.Category,
                    "all",
                    StringComparison.OrdinalIgnoreCase
                ) != 0
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

            // EF Core bug, so we have to covert to Array
            var brands = searchOption.GetBrands().ToArray();
            if (brands.Any())
            {
                queryable = queryable.Where(x =>
                    (
                        x.BrandId.HasValue && brands.Contains(x.Brand.Slug)
                        || (includedBrand != null && x.BrandId == includedBrand.Id)
                    )
                );
            }

            model.TotalProduct = queryable.Count();
            var currentPageNum = searchOption.Page <= 0 ? 1 : searchOption.Page;
            var offset = (_pageSize * currentPageNum) - _pageSize;
            while (currentPageNum > 1 && offset >= model.TotalProduct)
            {
                currentPageNum--;
                offset = (_pageSize * currentPageNum) - _pageSize;
            }

            if (!string.IsNullOrEmpty(searchOption.Query))
                SaveSearchQuery(searchOption, model);

            queryable = AppySort(searchOption, queryable);

            var products = queryable
                .Include(x => x.ThumbnailImage)
                .Include(x => x.OptionValues)
                .ThenInclude(x => x.Option)
                .Include(x => x.AttributeValues)
                .ThenInclude(x => x.Attribute)
                .ThenInclude(x => x.Group)
                .Skip(offset)
                .Take(_pageSize)
                .Select(x => ProductThumbnail.FromProduct(x))
                .ToList();

            foreach (var product in products)
            {
                product.Name = _contentLocalizationService.GetLocalizedProperty(
                    nameof(Product),
                    product.Id,
                    nameof(product.Name),
                    product.Name
                );
                product.ThumbnailUrl = _mediaService.GetThumbnailUrl(product.ThumbnailImage);
                product.CalculatedProductPrice = _productPricingService.CalculateProductPrice(
                    product
                );
            }

            model.Products = products;
            model.CurrentSearchOption.PageSize = _pageSize;
            model.CurrentSearchOption.Page = currentPageNum;

            return Ok(model);
        }

        [HttpGet("vendor/search")]
        public async Task<IActionResult> SearchByVendorsForCustomer(
            [FromQuery] string query,
            [FromQuery] string brand = null,
            [FromQuery] string category = null,
            [FromQuery] int page = 1,
            [FromQuery] string sort = null,
            [FromQuery] int? minPrice = null,
            [FromQuery] int? maxPrice = null
        )
        {
            // 1. Get current user/customer ID (adapt as per your auth system)
            var userIdStr = User.FindFirstValue(
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
            );
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var customerId))
                return Unauthorized(new { message = "User not authenticated" });

            // 2. Find all vendor associations
            var vendorIds = _vendorCustomerAssociationRepository
                .Query()
                .Where(x => x.CustomerId == customerId && x.IsActive && !x.IsDeleted)
                .Select(x => x.VendorId)
                .ToList();

            if (vendorIds == null || !vendorIds.Any())
                return Forbid("No vendor associations found for this customer.");

            // 3. Fetch vendor details for the associated vendors
            var vendorsById = _vendorRepository
                .Query()
                .Where(x => vendorIds.Contains(x.Id) && x.IsActive && !x.IsDeleted)
                .Select(x => new VendorDetail
                {
                    Id = x.Id,
                    Name = x.Name,
                    Slug = x.Slug,
                    Description = x.Description,
                    Email = x.Email,
                    IsActive = x.IsActive,
                })
                .ToDictionary(x => x.Id);

            // 4. Use the same search logic, but with a vendorId list
            var searchOption = new SearchOption
            {
                Query = query + "",
                Brand = brand,
                Category = category,
                Page = page,
                Sort = sort,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
            };

            // Build product details from admin-owned products, then overlay vendor price/stock.
            var matchedLocalizedProductIds = _localizedContentPropertyRepository
                .Query()
                .Where(x =>
                    x.EntityType == nameof(Product)
                    && x.Value.ToLower().Contains(searchOption.Query.ToLower())
                )
                .Select(x => x.EntityId)
                .Distinct()
                .ToList();

            var productQueryable = _productRepository
                .Query()
                .Where(x =>
                    x.IsPublished
                    && x.IsVisibleIndividually
                    && (
                        matchedLocalizedProductIds.Contains(x.Id)
                        || x.Name.ToLower().Contains(searchOption.Query.ToLower())
                        || x.ShortDescription.ToLower().Contains(searchOption.Query.ToLower())
                        || x.Description.ToLower().Contains(searchOption.Query.ToLower())
                        || x.Specification.ToLower().Contains(searchOption.Query.ToLower())
                    )
                );

            if (
                string.Compare(
                    searchOption.Category,
                    "all",
                    System.StringComparison.OrdinalIgnoreCase
                ) != 0
            )
            {
                var categories = searchOption.GetCategories().ToArray();
                if (categories.Any())
                    productQueryable = productQueryable.Where(x =>
                        x.Categories.Any(c => categories.Contains(c.Category.Slug))
                    );
            }

            var brands = searchOption.GetBrands().ToArray();
            if (brands.Any())
                productQueryable = productQueryable.Where(x =>
                    x.BrandId.HasValue && brands.Contains(x.Brand.Slug)
                );

            var adminProducts = productQueryable
                .Include(x => x.ThumbnailImage)
                .Include(x => x.OptionValues)
                .ThenInclude(x => x.Option)
                .Include(x => x.AttributeValues)
                .ThenInclude(x => x.Attribute)
                .ThenInclude(x => x.Group)
                .ToList();

            var adminProductIds = adminProducts.Select(x => x.Id).ToArray();
            var productsById = adminProducts.ToDictionary(x => x.Id);
            var vendorProducts = _vendorProductRepository
                .Query()
                .Where(x =>
                    x.IsActive
                    && (!x.ExpiryDate.HasValue || x.ExpiryDate.Value >= DateTime.UtcNow.Date)
                    && vendorIds.Contains(x.VendorId)
                    && adminProductIds.Contains(x.ProductId)
                    && x.StockQuantity > 0
                )
                .ToList();

            if (searchOption.MinPrice.HasValue)
            {
                vendorProducts = vendorProducts
                    .Where(x =>
                        VendorProductPricing.GetEffectivePrice(x, productsById[x.ProductId].Price)
                        >= searchOption.MinPrice.Value
                    )
                    .ToList();
            }

            if (searchOption.MaxPrice.HasValue)
            {
                vendorProducts = vendorProducts
                    .Where(x =>
                        VendorProductPricing.GetEffectivePrice(x, productsById[x.ProductId].Price)
                        <= searchOption.MaxPrice.Value
                    )
                    .ToList();
            }

            var model = new SearchResult
            {
                CurrentSearchOption = searchOption,
                FilterOption = new FilterOption(),
            };

            model.TotalProduct = vendorProducts.Count();
            var currentPageNum = searchOption.Page <= 0 ? 1 : searchOption.Page;
            var offset = (_pageSize * currentPageNum) - _pageSize;
            while (currentPageNum > 1 && offset >= model.TotalProduct)
            {
                currentPageNum--;
                offset = (_pageSize * currentPageNum) - _pageSize;
            }

            var sortedVendorProducts = SortVendorProducts(
                    searchOption,
                    vendorProducts,
                    productsById
                )
                .Skip(offset)
                .Take(_pageSize)
                .ToList();

            var products = new List<ProductThumbnail>();
            foreach (var vendorProduct in sortedVendorProducts)
            {
                var adminProduct = productsById[vendorProduct.ProductId];
                var productThumbnail = ProductThumbnail.FromProduct(adminProduct);
                productThumbnail.Price = VendorProductPricing.GetEffectivePrice(
                    vendorProduct,
                    adminProduct.Price
                );
                productThumbnail.OldPrice = vendorProduct.MaximumRetailPrice;
                productThumbnail.IsAvailableForCreditBuyers =
                    vendorProduct.IsAvailableForCreditBuyers;
                productThumbnail.NetSalePriceForZiraat =
                    VendorProductPricing.GetNetSalePriceForZiraat(
                        vendorProduct,
                        adminProduct.Price
                    );
                productThumbnail.CreditSaleHikePercentage = vendorProduct.CreditSaleHikePercentage;
                productThumbnail.SalePriceForCreditor = vendorProduct.IsAvailableForCreditBuyers
                    ? VendorProductPricing.GetSalePriceForCreditor(
                        vendorProduct,
                        adminProduct.Price
                    )
                    : (decimal?)null;
                productThumbnail.SpecialPrice = null;
                productThumbnail.SpecialPriceStart = null;
                productThumbnail.SpecialPriceEnd = null;
                productThumbnail.StockQuantity = vendorProduct.StockQuantity;
                productThumbnail.VendorId = vendorProduct.VendorId;
                productThumbnail.VendorProductId = vendorProduct.Id;
                if (vendorsById.TryGetValue(vendorProduct.VendorId, out var vendor))
                {
                    productThumbnail.Vendor = new ProductVendorDetail
                    {
                        Id = vendor.Id,
                        Name = vendor.Name,
                        Slug = vendor.Slug,
                        Description = vendor.Description,
                        Email = vendor.Email,
                        IsActive = vendor.IsActive,
                    };
                }
                products.Add(productThumbnail);
            }

            foreach (var product in products)
            {
                product.Name = _contentLocalizationService.GetLocalizedProperty(
                    nameof(Product),
                    product.Id,
                    nameof(product.Name),
                    product.Name
                );
                product.ThumbnailUrl = _mediaService.GetThumbnailUrl(product.ThumbnailImage);
                product.CalculatedProductPrice = _productPricingService.CalculateProductPrice(
                    product
                );
            }

            model.Products = products;
            model.CurrentSearchOption.PageSize = _pageSize;
            model.CurrentSearchOption.Page = currentPageNum;

            return Ok(model);
        }

        private static IQueryable<Product> AppySort(
            SearchOption searchOption,
            IQueryable<Product> query
        )
        {
            var sortBy = searchOption.Sort ?? string.Empty;
            switch (sortBy.ToLower())
            {
                case "price-desc":
                    query = query.OrderByDescending(x => x.Price);
                    break;
                default:
                    query = query.OrderBy(x => x.Price);
                    break;
            }

            return query;
        }

        private static IEnumerable<VendorProduct> SortVendorProducts(
            SearchOption searchOption,
            IEnumerable<VendorProduct> vendorProducts,
            IDictionary<long, Product> productsById
        )
        {
            var sortBy = searchOption.Sort ?? string.Empty;
            return sortBy.ToLower() == "price-desc"
                ? vendorProducts.OrderByDescending(x =>
                    VendorProductPricing.GetEffectivePrice(x, productsById[x.ProductId].Price)
                )
                : vendorProducts.OrderBy(x =>
                    VendorProductPricing.GetEffectivePrice(x, productsById[x.ProductId].Price)
                );
        }

        private void AppendFilterOptionsToModel(SearchResult model, IQueryable<Product> query)
        {
            model.FilterOption.Price.MaxPrice = query.Max(x => x.Price);
            model.FilterOption.Price.MinPrice = query.Min(x => x.Price);

            var getCategoryName = _contentLocalizationService.GetLocalizationFunction<Category>();

            model.FilterOption.Categories = query
                .SelectMany(x => x.Categories)
                .GroupBy(x => new
                {
                    x.Category.Id,
                    x.Category.Name,
                    x.Category.Slug,
                    x.Category.ParentId,
                })
                .Select(g => new FilterCategory
                {
                    Id = (int)g.Key.Id,
                    Name = g.Key.Name,
                    Slug = g.Key.Slug,
                    ParentId = g.Key.ParentId,
                    Count = g.Count(),
                })
                .ToList();

            foreach (var item in model.FilterOption.Categories)
            {
                item.Name = getCategoryName(item.Id, nameof(item.Name), item.Name);
            }

            // TODO an EF Core bug, so we have to do evaluation in client
            model.FilterOption.Brands = query
                .Include(x => x.Brand)
                .Where(x => x.BrandId != null)
                .ToList()
                .GroupBy(x => x.Brand)
                .Select(g => new FilterBrand
                {
                    Id = (int)g.Key.Id,
                    Name = g.Key.Name,
                    Slug = g.Key.Slug,
                    Count = g.Count(),
                })
                .ToList();
        }

        private void SaveSearchQuery(SearchOption searchOption, SearchResult model)
        {
            if (string.IsNullOrWhiteSpace(searchOption.Query))
            {
                return;
            }

            var query = new Query
            {
                CreatedOn = DateTimeOffset.Now,
                QueryText = searchOption.Query,
                ResultsCount = model.TotalProduct,
            };

            _queryRepository.Add(query);
            _queryRepository.SaveChanges();
        }
    }
}
