using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Catalog.Areas.Catalog.ViewModels;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Catalog.Services;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Events;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Catalog.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    [ApiController]
    [Route("api/catalog/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IMediaService _mediaService;
        private readonly IRepository<Product> _productRepository;
        private readonly IMediator _mediator;
        private readonly IProductPricingService _productPricingService;
        private readonly IContentLocalizationService _contentLocalizationService;
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IRepository<VendorCustomerAssociation> _vendorCustomerAssociationRepository;

        public ProductController(IRepository<Product> productRepository,
            IMediaService mediaService,
            IMediator mediator,
            IProductPricingService productPricingService,
            IContentLocalizationService contentLocalizationService,
            IRepository<VendorProduct> vendorProductRepository,
            IRepository<VendorCustomerAssociation> vendorCustomerAssociationRepository)
        {
            _productRepository = productRepository;
            _mediaService = mediaService;
            _mediator = mediator;
            _productPricingService = productPricingService;
            _contentLocalizationService = contentLocalizationService;
            _vendorProductRepository = vendorProductRepository;
            _vendorCustomerAssociationRepository = vendorCustomerAssociationRepository;
        }

        [HttpGet("product/product-overview")]
        public async Task<IActionResult> ProductOverview(long id, long? vendorProductId = null, long? vendorId = null)
        {
            var product = await _productRepository.Query()
                .Include(x => x.OptionValues)
                .Include(x => x.ProductLinks).ThenInclude(p => p.LinkedProduct)
                .Include(x => x.ThumbnailImage)
                .Include(x => x.Medias).ThenInclude(m => m.Media)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsPublished);

            if (product == null)
            {
                return NotFound();
            }

            var vendorProduct = ResolveVendorProduct(product, vendorProductId, vendorId);
            var model = new ProductDetail
            {
                Id = product.Id,
                Name = _contentLocalizationService.GetLocalizedProperty(product, nameof(product.Name), product.Name),
                CalculatedProductPrice = CalculateProductPrice(product, vendorProduct),
                IsCallForPricing = product.IsCallForPricing,
                IsAllowToOrder = product.IsAllowToOrder,
                StockTrackingIsEnabled = product.StockTrackingIsEnabled,
                StockQuantity = vendorProduct?.StockQuantity ?? product.StockQuantity,
                VendorId = vendorProduct?.VendorId,
                VendorProductId = vendorProduct?.Id,
                ShortDescription = _contentLocalizationService.GetLocalizedProperty(product, nameof(product.ShortDescription), product.ShortDescription),
                ReviewsCount = product.ReviewsCount,
                RatingAverage = product.RatingAverage,
            };

            MapProductVariantToProductVm(product, model, vendorProduct?.VendorId ?? vendorId);
            MapProductOptionToProductVm(product, model);
            MapProductImagesToProductVm(product, model);

            return Ok(model);
        }

        public async Task<IActionResult> ProductDetail(long id, long? vendorProductId = null, long? vendorId = null)
        {
            var product = _productRepository.Query()
                .Include(x => x.OptionValues)
                .Include(x => x.Categories).ThenInclude(c => c.Category)
                .Include(x => x.AttributeValues).ThenInclude(a => a.Attribute)
                .Include(x => x.ProductLinks).ThenInclude(p => p.LinkedProduct).ThenInclude(m => m.ThumbnailImage)
                .Include(x => x.ThumbnailImage)
                .Include(x => x.Medias).ThenInclude(m => m.Media)
                .Include(x => x.Brand)
                .FirstOrDefault(x => x.Id == id && x.IsPublished);
            if (product == null)
            {
                return NotFound();
            }

            var vendorProduct = ResolveVendorProduct(product, vendorProductId, vendorId);
            var model = new ProductDetail
            {
                Id = product.Id,
                Name = _contentLocalizationService.GetLocalizedProperty(product, nameof(product.Name), product.Name),
                Brand = product.Brand,
                CalculatedProductPrice = CalculateProductPrice(product, vendorProduct),
                IsCallForPricing = product.IsCallForPricing,
                IsAllowToOrder = product.IsAllowToOrder,
                StockTrackingIsEnabled = product.StockTrackingIsEnabled,
                StockQuantity = vendorProduct?.StockQuantity ?? product.StockQuantity,
                VendorId = vendorProduct?.VendorId,
                VendorProductId = vendorProduct?.Id,
                ShortDescription = _contentLocalizationService.GetLocalizedProperty(product, nameof(product.ShortDescription), product.ShortDescription),
                MetaTitle = product.MetaTitle,
                MetaKeywords = product.MetaKeywords,
                MetaDescription = product.MetaDescription,
                Description = _contentLocalizationService.GetLocalizedProperty(product, nameof(product.Description), product.Description),
                Specification = _contentLocalizationService.GetLocalizedProperty(product, nameof(product.Specification), product.Specification),
                ReviewsCount = product.ReviewsCount,
                RatingAverage = product.RatingAverage,
                Attributes = product.AttributeValues.Select(x => new ProductDetailAttribute { Name = x.Attribute.Name, Value = x.Value }).ToList(),
                Categories = product.Categories.Select(x => new ProductDetailCategory { Id = x.CategoryId, Name = x.Category.Name, Slug = x.Category.Slug }).ToList()
            };

            MapProductVariantToProductVm(product, model, vendorProduct?.VendorId ?? vendorId);
            MapRelatedProductToProductVm(product, model);
            MapProductOptionToProductVm(product, model);
            MapProductImagesToProductVm(product, model);

            await _mediator.Publish(new EntityViewed { EntityId = product.Id, EntityTypeId = "Product" });
            _productRepository.SaveChanges();

            return Ok(model);
        }

        private VendorProduct ResolveVendorProduct(Product product, long? vendorProductId, long? vendorId)
        {
            var query = _vendorProductRepository.Query()
                .Where(x => x.ProductId == product.Id && x.IsActive);

            if (vendorProductId.HasValue)
            {
                return query.FirstOrDefault(x => x.Id == vendorProductId.Value);
            }

            if (vendorId.HasValue)
            {
                return query.FirstOrDefault(x => x.VendorId == vendorId.Value);
            }

            var customerVendorIds = GetCurrentCustomerVendorIds();
            if (!customerVendorIds.Any())
            {
                return null;
            }

            return query
                .Where(x => customerVendorIds.Contains(x.VendorId) && x.StockQuantity > 0)
                .ToList()
                .OrderBy(x => VendorProductPricing.GetEffectivePrice(x, product.Price))
                .FirstOrDefault();
        }

        private List<long> GetCurrentCustomerVendorIds()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var customerId))
            {
                return new List<long>();
            }

            return _vendorCustomerAssociationRepository.Query()
                .Where(x => x.CustomerId == customerId && x.IsActive && !x.IsDeleted)
                .Select(x => x.VendorId)
                .ToList();
        }

        private CalculatedProductPrice CalculateProductPrice(Product product, VendorProduct vendorProduct)
        {
            if (vendorProduct == null)
            {
                return _productPricingService.CalculateProductPrice(product);
            }

            return _productPricingService.CalculateProductPrice(
                VendorProductPricing.GetEffectivePrice(vendorProduct, product.Price),
                VendorProductPricing.GetOldPrice(vendorProduct),
                null,
                null,
                null);
        }

        private void MapProductImagesToProductVm(Product product, ProductDetail model)
        {
            model.Images = product.Medias.Where(x => x.Media.MediaType == MediaType.Image).Select(productMedia => new MediaViewModel
            {
                Url = _mediaService.GetMediaUrl(productMedia.Media),
                ThumbnailUrl = _mediaService.GetThumbnailUrl(productMedia.Media)
            }).ToList();
        }

        private void MapProductOptionToProductVm(Product product, ProductDetail model)
        {
            foreach (var item in product.OptionValues)
            {
                var optionValues = JsonConvert.DeserializeObject<IList<ProductOptionValueVm>>(item.Value);
                foreach (var value in optionValues)
                {
                    if (!model.OptionDisplayValues.ContainsKey(value.Key))
                    {
                        model.OptionDisplayValues.Add(value.Key, new ProductOptionDisplay { DisplayType = item.DisplayType, Value = value.Display });
                    }
                }
            }
        }

        private void MapProductVariantToProductVm(Product product, ProductDetail model, long? vendorId)
        {
            if(!product.ProductLinks.Any(x => x.LinkType == ProductLinkType.Super))
            {
                return;
            }

            var variations = _productRepository
                .Query()
                .Include(x => x.OptionCombinations).ThenInclude(o => o.Option)
                .Include(x => x.Medias).ThenInclude(m => m.Media)
                .Where(x => x.LinkedProductLinks.Any(link => link.ProductId == product.Id && link.LinkType == ProductLinkType.Super))
                .Where(x => x.IsPublished)
                .ToList();

            foreach (var variation in variations)
            {
                var vendorVariationProduct = ResolveVendorProduct(variation, null, vendorId);
                var variationVm = new ProductDetailVariation
                {
                    Id = variation.Id,
                    Name = variation.Name,
                    NormalizedName = variation.NormalizedName,
                    IsAllowToOrder = variation.IsAllowToOrder,
                    IsCallForPricing = variation.IsCallForPricing,
                    StockTrackingIsEnabled = variation.StockTrackingIsEnabled,
                    StockQuantity = vendorVariationProduct?.StockQuantity ?? variation.StockQuantity,
                    CalculatedProductPrice = CalculateProductPrice(variation, vendorVariationProduct),
                    Images = variation.Medias.Where(x => x.Media.MediaType == MediaType.Image).Select(productMedia => new MediaViewModel
                    {
                        Url = _mediaService.GetMediaUrl(productMedia.Media),
                        ThumbnailUrl = _mediaService.GetThumbnailUrl(productMedia.Media)
                    }).ToList()
                };

                var optionCombinations = variation.OptionCombinations.OrderBy(x => x.SortIndex);
                foreach (var combination in optionCombinations)
                {
                    variationVm.Options.Add(new ProductDetailVariationOption
                    {
                        OptionId = combination.OptionId,
                        OptionName = combination.Option.Name,
                        Value = combination.Value
                    });
                }

                model.Variations.Add(variationVm);
            }
        }

        private void MapRelatedProductToProductVm(Product product, ProductDetail model)
        {
            var publishedProductLinks = product.ProductLinks.Where(x => x.LinkedProduct.IsPublished && (x.LinkType == ProductLinkType.Related || x.LinkType == ProductLinkType.CrossSell));
            foreach (var productLink in publishedProductLinks)
            {
                var linkedProduct = productLink.LinkedProduct;
                var productThumbnail = ProductThumbnail.FromProduct(linkedProduct);
                productThumbnail.Name = _contentLocalizationService.GetLocalizedProperty(nameof(Product), productThumbnail.Id, nameof(product.Name), productThumbnail.Name);
                productThumbnail.ThumbnailUrl = _mediaService.GetThumbnailUrl(linkedProduct.ThumbnailImage);
                productThumbnail.CalculatedProductPrice = _productPricingService.CalculateProductPrice(linkedProduct);

                if (productLink.LinkType == ProductLinkType.Related)
                {
                    model.RelatedProducts.Add(productThumbnail);
                }

                if (productLink.LinkType == ProductLinkType.CrossSell)
                {
                    model.CrossSellProducts.Add(productThumbnail);
                }
            }
        }
    }
}
