using System;
using System.Linq;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [ApiController]
    [Route("api/home")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IWidgetInstanceService _widgetInstanceService;
        private readonly IContentLocalizationService _contentLocalizationService;

        public HomeController(ILoggerFactory factory, IWidgetInstanceService widgetInstanceService, IContentLocalizationService contentLocalizationService)
        {
            _logger = factory.CreateLogger("Unhandled Error");
            _widgetInstanceService = widgetInstanceService;
            _contentLocalizationService = contentLocalizationService;
        }

      

        [HttpGet("[action]")]
        public IActionResult Index()
        {
            var model = new HomeViewModel();
            var getWidgetInstanceTranslations = _contentLocalizationService.GetLocalizationFunction<WidgetInstance>();

            model.WidgetInstances = _widgetInstanceService.GetPublished()
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new WidgetInstanceViewModel
            {
                Id = x.Id,
                Name = x.Name,
                ViewComponentName = x.Widget.ViewComponentName,
                WidgetId = x.WidgetId,
                WidgetZoneId = x.WidgetZoneId,
                Data = x.Data,
                HtmlData = x.HtmlData
            }).ToList();

            foreach(var item in model.WidgetInstances)
            {
                item.Name = getWidgetInstanceTranslations(item.Id, nameof(item.Name), item.Name);
                item.HtmlData = getWidgetInstanceTranslations(item.Id, nameof(item.HtmlData), item.HtmlData);
            }

            return Ok(model);
        }

        [HttpGet("[action]")]
        public IActionResult ErrorWithCode(int statusCode)
        {
            if (statusCode == 404)
            {
                return NotFound(new { statusCode, message = "Not Found" });
            }

            return StatusCode(statusCode, new { statusCode, message = "Error" });
        }

        [HttpGet("/Home/Error")]
        public IActionResult Error()
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var error = feature?.Error;

            if (error != null)
            {
                _logger.LogError(error.Message, error);
            }

            return Problem(detail: error?.Message);
        }
    }
}
