using System.Collections.Generic;
using ECommerce.Module.Core.Areas.Core.ViewModels;

namespace ECommerce.Module.Cms.Areas.Cms.ViewModels
{
    public class SpaceBarWidgetForm : WidgetFormBase
    {
        public IList<SpaceBarWidgetSetting> Items = new List<SpaceBarWidgetSetting>();
    }
}
