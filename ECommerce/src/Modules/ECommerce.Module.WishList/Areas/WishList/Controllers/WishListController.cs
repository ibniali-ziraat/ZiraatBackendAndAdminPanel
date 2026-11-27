using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Core.ViewModels;
using ECommerce.Module.WishList.Areas.WishList.ViewModels;
using ECommerce.Module.WishList.Models;
using ECommerce.Module.WishList.Services;

namespace ECommerce.Module.WishList.Areas.WishList.Controllers
{
    [Area("WishList")]
    [ApiController]
    [Route("api/wishList")]
    public class WishListController :ControllerBase
    {
        private const int DefaultPageSize = 25;

        private readonly IRepository<Models.WishList> _wishListRepository;
        private readonly IRepository<WishListItem> _wishListItemRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IWishListService _wishListService;
        private readonly IMediaService _mediaService;
        private readonly IEmailSender _emailSender;
        private readonly IWorkContext _workContext;
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IRepository<VendorCustomerAssociation> _vendorCustomerAssociationRepository;

        public WishListController(
            IRepository<Models.WishList> wishListRepository,
            IRepository<WishListItem> wishListItemRepository,
            IRepository<Product> productRepository,
            IWishListService wishListService,
            IMediaService mediaService,
            IEmailSender emailSender,
            IWorkContext workContext,
            IRepository<VendorProduct> vendorProductRepository,
            IRepository<VendorCustomerAssociation> vendorCustomerAssociationRepository)
        {
            _wishListRepository = wishListRepository;
            _wishListItemRepository = wishListItemRepository;
            _productRepository = productRepository;
            _wishListService = wishListService;
            _mediaService = mediaService;
            _emailSender = emailSender;
            _workContext = workContext;
            _vendorProductRepository = vendorProductRepository;
            _vendorCustomerAssociationRepository = vendorCustomerAssociationRepository;
        }

        
        [HttpPost("[action]")]
        public async Task<IActionResult> Share(ShareWishListForm model)
        {
            if (ModelState.IsValid)
            {
                var user = await _workContext.GetCurrentUser();
                if (user == null) return Unauthorized();

                var wishList = await _wishListRepository
                    .Query()
                    .SingleOrDefaultAsync(x => x.UserId == user.Id);

                string sharingCode = String.Empty;

                if (wishList.SharingCode == null)
                {
                    sharingCode = _wishListService.GenerateSharingCode(wishList.Id);
                    wishList.SharingCode = sharingCode;
                    await _wishListRepository.SaveChangesAsync();
                }
                else
                {
                    sharingCode = wishList.SharingCode;
                }

                var wishListUrl = Url.Action("PublicList", "WishList", new { id = sharingCode }, protocol: HttpContext.Request.Scheme);
                string emailBody = $"{user.Email} would like to share their wish list with you: {wishListUrl}"
                    + Environment.NewLine
                    + Environment.NewLine
                    + model.Message;

                await _emailSender.SendEmailAsync(model.EmailAddress, "Wish List Sharing", emailBody);

                return RedirectToAction("PrivateList");
            }

            return Ok(model);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> PrivateList(int? pageNumber, int? pageSize)
        {
            var user = await _workContext.GetCurrentUser();
            if (user == null) return Unauthorized();

            var wishList = await _wishListRepository
                .Query()
                .SingleOrDefaultAsync(x => x.UserId == user.Id);

            if (wishList == null)
            {
                wishList = new Models.WishList()
                {
                    UserId = user.Id
                };

                _wishListRepository.Add(wishList);
                await _wishListRepository.SaveChangesAsync();
            }

            var wishlistVm = await List(wishList, pageNumber, pageSize);

            return Ok(wishlistVm);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> PublicList(string id, int? pageNumber, int? pageSize)
        {
            var wishList = await _wishListRepository
                .Query()
                .SingleOrDefaultAsync(x => x.SharingCode == id);

            if (wishList == null)
            {
                return NotFound();
            }

            var wishlistVm = await List(wishList, pageNumber, pageSize);

            return Ok(wishlistVm);
        }

        private async Task<WishListVm> List(Models.WishList wishList, int? pageNumber, int? pageSize)
        {
            var itemsPerPage = pageSize.HasValue ? pageSize.Value : DefaultPageSize;
            var currentPageNum = pageNumber.HasValue ? pageNumber.Value : 1;
            var offset = (itemsPerPage * currentPageNum) - itemsPerPage;

            var wishlistVm = new WishListVm()
            {
                Id = wishList.Id,
                SharingCode = wishList.SharingCode
            };

            var wishListItems = await _wishListItemRepository
                .Query()
                .Include(x => x.Product).ThenInclude(x => x.ThumbnailImage)
                .Where(x => x.WishListId == wishList.Id)
                .Skip(offset)
                .Take(itemsPerPage)
                .ToListAsync();

            wishlistVm.Items.Data = wishListItems
                .Select(x =>
                {
                    var vendorProduct = GetVendorProduct(x);
                    return new WishListItemVm()
                    {
                        Id = x.Id,
                        WishListId = x.WishListId,
                        ProductId = x.Product.Id,
                        VendorId = x.VendorId,
                        VendorProductId = x.VendorProductId,
                        VendorAssignment = VendorProductAssignmentPricingVm.From(vendorProduct, x.Product.Price),
                        ProductName = x.Product.Name,
                        Description = x.Description,
                        ProductImage = _mediaService.GetThumbnailUrl(x.Product.ThumbnailImage),
                        ProductPrice = VendorProductPricing.GetEffectivePrice(vendorProduct, x.Product.Price),
                        ProductStockQuantity = vendorProduct?.StockQuantity ?? x.Product.StockQuantity,
                        Quantity = x.Quantity
                    };
                })
                .ToList();

            wishlistVm.Items.PageNumber = currentPageNum;
            wishlistVm.Items.PageSize = itemsPerPage;
            wishlistVm.Items.TotalItems = await _wishListItemRepository
                .Query()
                .CountAsync(x => x.WishListId == wishList.Id);

            return wishlistVm;
        }

        [Authorize]
        [HttpPost("[action]")]
        public async Task<IActionResult> AddItem([FromBody] AddToWishList model)
        {
            if (ModelState.IsValid)
            {
                var user = await _workContext.GetCurrentUser();
                var resultModel = new AddToWishListResult();

                var product = await _productRepository
                    .Query()
                    .Include(x => x.ThumbnailImage)
                    .SingleOrDefaultAsync(x => x.Id == model.ProductId);

                if (product == null)
                {
                    return NotFound();
                }

                var wishList = await _wishListRepository
                    .Query()
                    .Include(x => x.Items)
                    .SingleOrDefaultAsync(x => x.UserId == user.Id);

                if (wishList == null)
                {
                    wishList = new Models.WishList()
                    {
                        UserId = user.Id
                    };

                    _wishListRepository.Add(wishList);
                    await _wishListRepository.SaveChangesAsync();
                }

                var vendorProduct = GetVendorProduct(user.Id, product, model.VendorProductId, model.VendorId);
                if (vendorProduct == null)
                {
                    return BadRequest(new { message = "No active vendor association is available for this product." });
                }

                var existingWishlistItem = wishList
                    .Items
                    .SingleOrDefault(x =>
                        x.ProductId == model.ProductId
                        && x.VendorId == (vendorProduct?.VendorId ?? model.VendorId)
                        && x.VendorProductId == (vendorProduct?.Id ?? model.VendorProductId));

                if (existingWishlistItem != null)
                {
                    resultModel.Message = "The product already exists in your wish list";
                    resultModel.Item = new WishListItemVm()
                    {
                        Id = existingWishlistItem.Id,
                        WishListId = wishList.Id,
                        VendorId = existingWishlistItem.VendorId,
                        VendorProductId = existingWishlistItem.VendorProductId,
                        VendorAssignment = VendorProductAssignmentPricingVm.From(vendorProduct, product.Price),
                        ProductName = product.Name,
                        ProductImage = _mediaService.GetThumbnailUrl(product.ThumbnailImage),
                        ProductPrice = VendorProductPricing.GetEffectivePrice(vendorProduct, product.Price),
                        ProductStockQuantity = vendorProduct?.StockQuantity ?? product.StockQuantity,
                        Quantity = existingWishlistItem.Quantity,
                    };
                }
                else
                {
                    var wishListItem = new WishListItem()
                    {
                        WishListId = wishList.Id,
                        ProductId = model.ProductId,
                        VendorId = vendorProduct?.VendorId ?? model.VendorId,
                        VendorProductId = vendorProduct?.Id ?? model.VendorProductId,
                        Quantity = model.Quantity
                    };

                    _wishListItemRepository.Add(wishListItem);

                    wishList.LatestUpdatedOn = DateTimeOffset.Now;
                    await _wishListRepository.SaveChangesAsync();

                    resultModel.Message = "The product has been added to your wish list";
                    resultModel.Item = new WishListItemVm()
                    {
                        Id = wishListItem.Id,
                        WishListId = wishList.Id,
                        VendorId = wishListItem.VendorId,
                        VendorProductId = wishListItem.VendorProductId,
                        VendorAssignment = VendorProductAssignmentPricingVm.From(vendorProduct, product.Price),
                        ProductName = product.Name,
                        ProductImage = _mediaService.GetThumbnailUrl(product.ThumbnailImage),
                        ProductPrice = VendorProductPricing.GetEffectivePrice(vendorProduct, product.Price),
                        ProductStockQuantity = vendorProduct?.StockQuantity ?? product.StockQuantity,
                        Quantity = model.Quantity,
                    };
                }

                return Ok(resultModel);
            }

            return NotFound();
        }

        [Authorize]
        [HttpDelete("[action]")]
        public async Task<IActionResult> RemoveItem(long id)
        {
            var user = await _workContext.GetCurrentUser();

            var wishList = await _wishListRepository
                .Query()
                .Include(x => x.Items)
                .SingleOrDefaultAsync(x => x.UserId == user.Id);

            if (wishList == null)
            {
                return NotFound();
            }

            var wishListItem = wishList
                .Items
                .SingleOrDefault(x => x.Id == id);

            if (wishListItem == null)
            {
                return NotFound();
            }

            _wishListItemRepository.Remove(wishListItem);

            wishList.LatestUpdatedOn = DateTimeOffset.Now;
            await _wishListRepository.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        [HttpPatch("[action]")]
        public async Task<IActionResult> UpdateItem([FromBody] UpdateWishListItem model)
        {
            if (ModelState.IsValid)
            {
                var user = await _workContext.GetCurrentUser();
                var returnModel = new AddToWishListResult();

                var wishList = await _wishListRepository.Query()
                    .Include(x => x.Items)
                    .SingleOrDefaultAsync(x => x.UserId == user.Id);

                if (wishList == null)
                {
                    return NotFound();
                }

                var wishListItem = wishList
                    .Items
                    .SingleOrDefault(x => x.Id == model.ItemId);

                if (wishListItem == null)
                {
                    return NotFound();
                }

                wishListItem.Description = String.IsNullOrWhiteSpace(model.Description) ? null : model.Description;
                wishListItem.Quantity = model.Quantity;
                wishListItem.LatestUpdatedOn = DateTimeOffset.Now;

                wishList.LatestUpdatedOn = DateTimeOffset.Now;
                await _wishListRepository.SaveChangesAsync();

                return Ok(returnModel);
            }

            return NotFound();
        }

        private VendorProduct GetVendorProduct(WishListItem item)
        {
            return GetVendorProduct(item.ProductId, item.VendorProductId, item.VendorId);
        }

        private VendorProduct GetVendorProduct(long customerId, Product product, long? vendorProductId, long? vendorId)
        {
            var query = _vendorProductRepository.Query()
                .Where(x => x.ProductId == product.Id && x.IsActive);

            if (vendorProductId.HasValue)
            {
                var vendorProduct = query.FirstOrDefault(x => x.Id == vendorProductId.Value);
                return vendorProduct != null && IsVendorAssociated(customerId, vendorProduct.VendorId)
                    ? vendorProduct
                    : null;
            }

            if (vendorId.HasValue)
            {
                if (!IsVendorAssociated(customerId, vendorId.Value))
                {
                    return null;
                }

                return query.FirstOrDefault(x => x.VendorId == vendorId.Value);
            }

            var associatedVendorIds = _vendorCustomerAssociationRepository.Query()
                .Where(x => x.CustomerId == customerId && x.IsActive && !x.IsDeleted)
                .Select(x => x.VendorId)
                .ToList();

            return query
                .Where(x => associatedVendorIds.Contains(x.VendorId) && x.StockQuantity > 0)
                .ToList()
                .OrderBy(x => VendorProductPricing.GetEffectivePrice(x, product.Price))
                .FirstOrDefault();
        }

        private VendorProduct GetVendorProduct(long productId, long? vendorProductId, long? vendorId)
        {
            if (vendorProductId.HasValue)
            {
                return _vendorProductRepository.Query()
                    .FirstOrDefault(x => x.Id == vendorProductId.Value && x.ProductId == productId && x.IsActive);
            }

            if (vendorId.HasValue)
            {
                return _vendorProductRepository.Query()
                    .FirstOrDefault(x => x.VendorId == vendorId.Value && x.ProductId == productId && x.IsActive);
            }

            return null;
        }

        private bool IsVendorAssociated(long customerId, long vendorId)
        {
            return _vendorCustomerAssociationRepository.Query()
                .Any(x => x.CustomerId == customerId && x.VendorId == vendorId && x.IsActive && !x.IsDeleted);
        }
    }
}
