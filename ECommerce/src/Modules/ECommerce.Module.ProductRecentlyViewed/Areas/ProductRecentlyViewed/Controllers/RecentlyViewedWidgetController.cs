using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.ProductRecentlyViewed.Areas.ProductRecentlyViewed.ViewModels;

namespace ECommerce.Module.ProductRecentlyViewed.Areas.ProductRecentlyViewed.Controllers
{
    [Area("ProductRecentlyViewed")]
    [Authorize(Roles = "admin")]
    [Route("api/recently-viewed-widgets")]
    public class RecentlyViewedWidgetApiController : ControllerBase
    {
        private readonly IRepository<WidgetInstance> _widgetInstanceRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IMediaService _mediaService;

        public RecentlyViewedWidgetApiController(
            IRepository<WidgetInstance> widgetInstanceRepository,
            IRepository<Product> productRepository,
            IMediaService mediaService)
        {
            _widgetInstanceRepository = widgetInstanceRepository;
            _productRepository = productRepository;
            _mediaService = mediaService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var widgetInstance = await _widgetInstanceRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (widgetInstance == null)
            {
                return NotFound();
            }

            int itemCount = 5;
            if (!string.IsNullOrEmpty(widgetInstance.Data))
            {
                int? deserialized = JsonConvert.DeserializeObject<int?>(widgetInstance.Data);
                if (deserialized.HasValue)
                {
                    itemCount = deserialized.Value;
                }
            }

            var model = new RecentlyViewedWidgetForm
            {
                Id = widgetInstance.Id,
                Name = widgetInstance.Name,
                WidgetZoneId = widgetInstance.WidgetZoneId,
                PublishStart = widgetInstance.PublishStart,
                PublishEnd = widgetInstance.PublishEnd,
                DisplayOrder = widgetInstance.DisplayOrder,
                ItemCount = itemCount
            };

            return new JsonResult(model);
        }

        [HttpGet("{id}/products")]
        public async Task<IActionResult> GetProducts(long id)
        {
            var widgetInstance = await _widgetInstanceRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (widgetInstance == null)
            {
                return NotFound(new { message = "Widget instance not found" });
            }

            int itemCount = 5;
            if (!string.IsNullOrEmpty(widgetInstance.Data))
            {
                int? deserialized = JsonConvert.DeserializeObject<int?>(widgetInstance.Data);
                if (deserialized.HasValue)
                {
                    itemCount = deserialized.Value;
                }
            }

            var products = await _productRepository.Query()
                .Where(x => x.IsPublished && x.IsVisibleIndividually)
                .OrderByDescending(x => x.LatestUpdatedOn)
                .Take(itemCount)
                .Include(x => x.ThumbnailImage)
                .Select(x => new ProductItemVm
                {
                    Id = x.Id,
                    Name = x.Name,
                    Slug = x.Slug,
                    Price = x.Price,
                    OldPrice = x.OldPrice,
                    SpecialPrice = x.SpecialPrice,
                    ShortDescription = x.ShortDescription,
                    ReviewsCount = x.ReviewsCount,
                    RatingAverage = x.RatingAverage,
                    ThumbnailImageUrl = x.ThumbnailImage != null ? _mediaService.GetMediaUrl(x.ThumbnailImage) : null
                })
                .ToListAsync();

            return new JsonResult(new { products });
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] RecentlyViewedWidgetForm model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var widgetInstance = new WidgetInstance
            {
                Name = model.Name,
                WidgetId = "RecentlyViewedWidget",
                WidgetZoneId = model.WidgetZoneId,
                Data = model.ItemCount.ToString(),
                PublishStart = model.PublishStart,
                PublishEnd = model.PublishEnd,
                DisplayOrder = model.DisplayOrder
            };

            _widgetInstanceRepository.Add(widgetInstance);
            await _widgetInstanceRepository.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = widgetInstance.Id }, null);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] RecentlyViewedWidgetForm model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var widgetInstance = await _widgetInstanceRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (widgetInstance == null)
            {
                return NotFound();
            }

            widgetInstance.Name = model.Name;
            widgetInstance.PublishStart = model.PublishStart;
            widgetInstance.PublishEnd = model.PublishEnd;
            widgetInstance.WidgetZoneId = model.WidgetZoneId;
            widgetInstance.DisplayOrder = model.DisplayOrder;
            widgetInstance.Data = model.ItemCount.ToString();

            await _widgetInstanceRepository.SaveChangesAsync();
            return Accepted();
        }
    }
}
