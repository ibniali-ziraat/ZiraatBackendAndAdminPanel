using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Web.SmartTable;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Vendors")]
    [ApiController]
    [Route("api/vendors")]
    [Authorize(Roles = "admin, vendor")]
    public class VendorProductAssignementController : ControllerBase
    {
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IVendorProductService _vendorProductService;
        private readonly IProductPriceProvider _productPriceProvider;
        private readonly IWorkContext _workContext;

        public VendorProductAssignementController(
            IRepository<Vendor> vendorRepository,
            IRepository<User> userRepository,
            IRepository<Role> roleRepository,
            IRepository<VendorProduct> vendorProductRepository,
            IVendorProductService vendorProductService,
            IProductPriceProvider productPriceProvider,
            IWorkContext workContext
        )
        {
            _vendorRepository = vendorRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _vendorProductRepository = vendorProductRepository;
            _vendorProductService = vendorProductService;
            _productPriceProvider = productPriceProvider;
            _workContext = workContext;
        }

        #region Vendor Products

        [HttpPost("{vendorId:long}/products/assign")]
        public async Task<IActionResult> AssignProduct(
            long vendorId,
            [FromBody] VendorProductForm model
        )
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();

                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return BadRequest(
                        new { error = "You don't have permission to manage this vendor's products" }
                    );
                }

                var vendorProduct = await _vendorProductService.AssignProductToVendor(
                    vendorId,
                    model.ProductId,
                    model.AssignedPrice,
                    model.DiscountPrice,
                    model.StockQuantity,
                    model.IsActive,
                    model.BatchNumber,
                    model.ManufacturingDate,
                    model.MaximumRetailPrice,
                    model.TaxRate,
                    model.CreditSaleHikePercentage,
                    model.IsAvailableForCreditBuyers,
                    model.NetSalePriceForZiraat,
                    model.SalePriceForCreditor,
                    model.Variations
                );

                vendorProduct.ExpiryDate = model.ExpiryDate;
                vendorProduct.CreatedById = currentUser.Id;
                vendorProduct.LatestUpdatedById = currentUser.Id;
                await _vendorProductRepository.SaveChangesAsync();

                var productVm = await BuildVendorProductVmAsync(vendorProduct);

                return CreatedAtAction(
                    nameof(GetAssignedProduct),
                    new { vendorId, vendorProductId = vendorProduct.Id },
                    productVm
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{vendorId:long}/products/expired")]
        public async Task<IActionResult> ListExpiredProducts(long vendorId)
        {
            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
            {
                return BadRequest(
                    new { error = "You don't have permission to access this resource" }
                );
            }

            var today = DateTime.UtcNow.Date;
            var products = await _vendorProductRepository
                .Query()
                .Where(x =>
                    x.VendorId == vendorId && x.ExpiryDate.HasValue && x.ExpiryDate.Value < today
                )
                .Include(x => x.Variations)
                .ThenInclude(x => x.OptionCombinations)
                .OrderBy(x => x.ExpiryDate)
                .ThenBy(x => x.ProductName)
                .ToListAsync();
            var result = new List<VendorProductVm>();
            foreach (var product in products)
            {
                result.Add(await BuildVendorProductVmAsync(product));
            }

            return Ok(result);
        }

        [HttpGet("{vendorId:long}/products/{vendorProductId:long}")]
        public async Task<IActionResult> GetAssignedProduct(long vendorId, long vendorProductId)
        {
            var vendorProducts = _vendorProductRepository
                .Query()
                .Include(x => x.Variations)
                .ThenInclude(x => x.OptionCombinations);
            var vendorProduct = await vendorProducts.FirstOrDefaultAsync(x =>
                x.Id == vendorProductId && x.VendorId == vendorId
            );
            vendorProduct ??= await vendorProducts
                .Where(x => x.ProductId == vendorProductId && x.VendorId == vendorId)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            if (vendorProduct == null)
            {
                return NotFound();
            }

            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
            {
                return BadRequest(
                    new { error = "You don't have permission to access this resource" }
                );
            }

            var productVm = await BuildVendorProductVmAsync(vendorProduct);

            return Ok(productVm);
        }

        [HttpPost("{vendorId:long}/products/grid")]
        public async Task<IActionResult> ListAssignedProducts(
            long vendorId,
            [FromBody] SmartTableParam param
        )
        {
            var currentUser = await _workContext.GetCurrentUser();

            if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
            {
                return BadRequest(
                    new { error = "You don't have permission to access this resource" }
                );
            }

            var query = _vendorProductRepository
                .Query()
                .Include(x => x.Variations)
                .ThenInclude(x => x.OptionCombinations)
                .Where(x => x.VendorId == vendorId);

            if (param?.Search?.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;

                if (search.Name != null)
                {
                    string productName = search.Name;
                    query = query.Where(x => x.ProductName.Contains(productName));
                }

                if (search.Sku != null)
                {
                    string sku = search.Sku;
                    query = query.Where(x => x.ProductSku.Contains(sku));
                }

                if (search.IsActive != null)
                {
                    bool isActive = search.IsActive;
                    query = query.Where(x => x.IsActive == isActive);
                }

                if (search.CreatedOn != null)
                {
                    if (search.CreatedOn.before != null)
                    {
                        DateTimeOffset before = search.CreatedOn.before;
                        query = query.Where(x => x.CreatedOn <= before);
                    }

                    if (search.CreatedOn.after != null)
                    {
                        DateTimeOffset after = search.CreatedOn.after;
                        query = query.Where(x => x.CreatedOn >= after);
                    }
                }
            }

            var gridData = query.ToSmartTableResultNoProjection(
                param,
                x => BuildVendorProductListItem(x)
            );
            gridData.Items = gridData.Items.ToList();

            await PopulateVariationOptionsAsync(gridData.Items);

            return Ok(gridData);
        }

        [HttpPut("{vendorId:long}/products/{vendorProductId:long}")]
        public async Task<IActionResult> UpdateAssignedProduct(
            long vendorId,
            long vendorProductId,
            [FromBody] VendorProductForm model
        )
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();

                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return BadRequest(
                        new { error = "You don't have permission to manage this vendor's products" }
                    );
                }

                var vendorProduct = await _vendorProductRepository
                    .Query()
                    .FirstOrDefaultAsync(x => x.Id == vendorProductId && x.VendorId == vendorId);

                if (vendorProduct == null)
                {
                    return NotFound();
                }

                vendorProduct = await _vendorProductService.UpdateVendorProduct(
                    vendorProductId,
                    model.AssignedPrice,
                    model.DiscountPrice,
                    model.StockQuantity,
                    model.IsActive,
                    model.BatchNumber,
                    model.ManufacturingDate,
                    model.MaximumRetailPrice,
                    model.TaxRate,
                    model.CreditSaleHikePercentage,
                    model.IsAvailableForCreditBuyers,
                    model.NetSalePriceForZiraat,
                    model.SalePriceForCreditor,
                    model.Variations,
                    syncToMainProduct: false
                );

                vendorProduct.ExpiryDate = model.ExpiryDate;
                vendorProduct.LatestUpdatedById = currentUser.Id;
                await _vendorProductRepository.SaveChangesAsync();

                var productVm = await BuildVendorProductVmAsync(vendorProduct);

                return Ok(productVm);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{vendorId:long}/products/{vendorProductId:long}")]
        public async Task<IActionResult> DeleteAssignedProduct(long vendorId, long vendorProductId)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();

                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return BadRequest(
                        new { error = "You don't have permission to manage this vendor's products" }
                    );
                }

                var vendorProduct = await _vendorProductRepository
                    .Query()
                    .FirstOrDefaultAsync(x => x.Id == vendorProductId && x.VendorId == vendorId);

                if (vendorProduct == null)
                {
                    return NotFound();
                }

                await _vendorProductService.DeleteVendorProduct(vendorProductId);

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{vendorId:long}/products/search")]
        public async Task<IActionResult> SearchProductsToAssign(
            long vendorId,
            [FromQuery] string name
        )
        {
            var currentUser = await _workContext.GetCurrentUser();

            if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
            {
                return BadRequest(
                    new { error = "You don't have permission to access this resource" }
                );
            }

            var assignedProductIds = await _vendorProductRepository
                .Query()
                .Where(x => x.VendorId == vendorId)
                .Select(x => x.ProductId)
                .ToListAsync();

            var products = await _productPriceProvider.SearchProductsAsync(
                name,
                assignedProductIds,
                20
            );

            return Ok(
                products.Select(x => new
                {
                    productId = x.Id,
                    productName = x.Name,
                    sku = x.Sku,
                    price = x.Price,
                    isVariation = x.IsVariation,
                    parentProductId = x.ParentProductId,
                    variationOptions = x.VariationOptions,
                    availableOptions = x.Options,
                })
            );
        }

        #endregion

        #region Helpers

        private async Task<VendorProductVm> BuildVendorProductVmAsync(VendorProduct vendorProduct)
        {
            var productInfo = await _productPriceProvider.GetProductInfoAsync(
                vendorProduct.ProductId
            );

            return new VendorProductVm
            {
                Id = vendorProduct.Id,
                ProductId = vendorProduct.ProductId,
                ProductName = productInfo?.Name ?? vendorProduct.ProductName,
                Sku = productInfo?.Sku ?? vendorProduct.ProductSku,
                BatchNumber = vendorProduct.BatchNumber,
                ManufacturingDate = vendorProduct.ManufacturingDate,
                ExpiryDate = vendorProduct.ExpiryDate,
                MaximumRetailPrice = vendorProduct.MaximumRetailPrice,
                AssignedPrice = vendorProduct.AssignedPrice,
                OriginalPrice = vendorProduct.OriginalPrice,
                DiscountPrice = vendorProduct.DiscountPrice,
                TaxRate = vendorProduct.TaxRate,
                NetSalePriceForZiraat = vendorProduct.NetSalePriceForZiraat,
                CreditSaleHikePercentage = vendorProduct.CreditSaleHikePercentage,
                SalePriceForCreditor = vendorProduct.SalePriceForCreditor,
                IsAvailableForCreditBuyers = vendorProduct.IsAvailableForCreditBuyers,
                StockQuantity = vendorProduct.StockQuantity,
                IsActive = vendorProduct.IsActive,
                CreatedOn = vendorProduct.CreatedOn,
                IsVariation = productInfo?.IsVariation ?? false,
                ParentProductId = productInfo?.ParentProductId,
                VariationOptions =
                    productInfo?.VariationOptions ?? new List<ProductVariationOptionInfoDto>(),
                AvailableOptions = productInfo?.Options ?? new List<ProductOptionInfoDto>(),
                Variations = vendorProduct
                    .Variations.OrderBy(x => x.SortIndex)
                    .Select(MapVendorProductVariation)
                    .ToList(),
            };
        }

        private VendorProductListItem BuildVendorProductListItem(VendorProduct vendorProduct)
        {
            return new VendorProductListItem
            {
                Id = vendorProduct.Id,
                ProductId = vendorProduct.ProductId,
                ProductName = vendorProduct.ProductName,
                Sku = vendorProduct.ProductSku,
                BatchNumber = vendorProduct.BatchNumber,
                ManufacturingDate = vendorProduct.ManufacturingDate,
                ExpiryDate = vendorProduct.ExpiryDate,
                MaximumRetailPrice = vendorProduct.MaximumRetailPrice,
                AssignedPrice = vendorProduct.AssignedPrice,
                OriginalPrice = vendorProduct.OriginalPrice,
                DiscountPrice = vendorProduct.DiscountPrice,
                TaxRate = vendorProduct.TaxRate,
                NetSalePriceForZiraat = vendorProduct.NetSalePriceForZiraat,
                CreditSaleHikePercentage = vendorProduct.CreditSaleHikePercentage,
                SalePriceForCreditor = vendorProduct.SalePriceForCreditor,
                IsAvailableForCreditBuyers = vendorProduct.IsAvailableForCreditBuyers,
                StockQuantity = vendorProduct.StockQuantity,
                IsActive = vendorProduct.IsActive,
                CreatedOn = vendorProduct.CreatedOn,
                Variations = vendorProduct
                    .Variations.OrderBy(x => x.SortIndex)
                    .Select(MapVendorProductVariation)
                    .ToList(),
            };
        }

        private static VendorProductVariationVm MapVendorProductVariation(
            VendorProductVariation variation
        )
        {
            return new VendorProductVariationVm
            {
                Id = variation.Id,
                Name = variation.Name,
                NormalizedName = variation.NormalizedName,
                Sku = variation.Sku,
                Gtin = variation.Gtin,
                MaximumRetailPrice = variation.MaximumRetailPrice,
                AssignedPrice = variation.AssignedPrice,
                OriginalPrice = variation.OriginalPrice,
                DiscountPrice = variation.DiscountPrice,
                TaxRate = variation.TaxRate,
                NetSalePriceForZiraat = variation.NetSalePriceForZiraat,
                CreditSaleHikePercentage = variation.CreditSaleHikePercentage,
                SalePriceForCreditor = variation.SalePriceForCreditor,
                IsAvailableForCreditBuyers = variation.IsAvailableForCreditBuyers,
                StockQuantity = variation.StockQuantity,
                IsActive = variation.IsActive,
                SortIndex = variation.SortIndex,
                OptionCombinations = variation
                    .OptionCombinations.OrderBy(option => option.SortIndex)
                    .Select(option => new VendorProductVariationOptionVm
                    {
                        Id = option.Id,
                        OptionId = option.OptionId,
                        OptionName = option.OptionName,
                        Value = option.Value,
                        StockQuantity = option.StockQuantity,
                        SortIndex = option.SortIndex,
                    })
                    .ToList(),
            };
        }

        private async Task PopulateVariationOptionsAsync(
            IEnumerable<VendorProductListItem> vendorProducts
        )
        {
            foreach (var vendorProduct in vendorProducts)
            {
                var productInfo = await _productPriceProvider.GetProductInfoAsync(
                    vendorProduct.ProductId
                );

                if (productInfo == null)
                {
                    continue;
                }

                vendorProduct.ProductName = productInfo.Name;
                vendorProduct.Sku = productInfo.Sku;
                vendorProduct.IsVariation = productInfo.IsVariation;
                vendorProduct.ParentProductId = productInfo.ParentProductId;
                vendorProduct.VariationOptions = productInfo.VariationOptions;
                vendorProduct.AvailableOptions = productInfo.Options;
            }
        }

        #endregion
    }
}
