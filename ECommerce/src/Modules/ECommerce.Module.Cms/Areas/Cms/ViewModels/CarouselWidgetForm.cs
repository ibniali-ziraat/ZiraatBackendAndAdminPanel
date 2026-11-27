using System.Collections.Generic;
using ECommerce.Module.Core.Areas.Core.ViewModels;

namespace ECommerce.Module.Cms.Areas.Cms.ViewModels
{
    public class CarouselWidgetForm : WidgetFormBase
    {
        public IList<CarouselWidgetItemForm> Items { get; set; } = new List<CarouselWidgetItemForm>();
    }
}
