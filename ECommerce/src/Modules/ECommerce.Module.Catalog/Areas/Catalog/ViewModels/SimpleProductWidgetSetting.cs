using System.Collections.Generic;

namespace ECommerce.Module.Catalog.Areas.Catalog.ViewModels
{
    public class SimpleProductWidgetSetting
    {
        public IList<ProductLinkVm> Products { get; set; } = new List<ProductLinkVm>();
    }
}
