using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.ProductRecentlyViewed.Areas.ProductRecentlyViewed.ViewModels;

namespace ECommerce.Module.ProductRecentlyViewed.Areas.ProductRecentlyViewed.Components
{
    public class RecentlyViewedWidgetViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(WidgetInstanceViewModel widgetInstance)
        {
            var model = new RecentlyViewedWidgetViewComponentVm
            {
                Id = widgetInstance.Id,
                ItemCount = JsonConvert.DeserializeObject<int>(widgetInstance.Data)
            };

            return View(model);
        }
    }
}
