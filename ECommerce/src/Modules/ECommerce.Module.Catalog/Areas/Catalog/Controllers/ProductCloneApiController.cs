using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Catalog.Areas.Catalog.ViewModels;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Catalog.Services;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Catalog.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    [ApiController]
    [Authorize(Roles = "admin, vendor")]
    [Route("api/product-clone")]
    public class ProductCloneApiController :ControllerBase
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IProductService _productService;
        private readonly IMediaService _mediaService;
        private readonly IWorkContext _workContext;

        public ProductCloneApiController(IRepository<Product> productRepository, IProductService productService, IMediaService mediaService, IWorkContext workContext)
        {
            _productRepository = productRepository;
            _productService = productService;
            _mediaService = mediaService;
            _workContext = workContext;
        }

        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            var product = _productRepository.Query().FirstOrDefault(x => x.Id == id);
            var model = new ProductCloneFormVm
            {
                Id = product.Id,
                Name = product.Name
            };

            return new JsonResult(model);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ProductCloneFormVm model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _workContext.GetCurrentUser();
                var product = _productRepository.Query()
                    .Include(x => x.AttributeValues)
                    .Include(x => x.Categories)
                    .FirstOrDefault(x => x.Id == model.Id);

                if (product == null)
                {
                    return NotFound();
                }

                var newProduct = product.Clone();
                newProduct.Name = model.Name;
                newProduct.Slug = model.Slug;
                newProduct.CreatedById = currentUser.Id;
                newProduct.LatestUpdatedById = currentUser.Id;
                newProduct.CreatedOn = DateTimeOffset.Now;
                newProduct.LatestUpdatedOn = DateTimeOffset.Now;

                var productPriceHistory = CreatePriceHistory(currentUser, newProduct);
                newProduct.PriceHistories.Add(productPriceHistory);

                _productService.Create(newProduct);
                return Created($"api/products/{newProduct.Id}", new { Id = newProduct.Id });
            }

            return BadRequest(ModelState);
        }

        private static ProductPriceHistory CreatePriceHistory(User loginUser, Product product)
        {
            return new ProductPriceHistory
            {
                CreatedBy = loginUser,
                Product = product,
                Price = product.Price,
                OldPrice = product.OldPrice,
                SpecialPrice = product.SpecialPrice,
                SpecialPriceStart = product.SpecialPriceStart,
                SpecialPriceEnd = product.SpecialPriceEnd
            };
        }
    }
}
