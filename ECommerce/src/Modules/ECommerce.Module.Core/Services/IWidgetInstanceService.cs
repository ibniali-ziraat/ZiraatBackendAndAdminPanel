using System.Linq;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface IWidgetInstanceService
    {
        IQueryable<WidgetInstance> GetPublished();
    }
}
