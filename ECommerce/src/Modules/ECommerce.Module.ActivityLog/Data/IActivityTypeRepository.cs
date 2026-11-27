using System.Linq;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.ActivityLog.Models;

namespace ECommerce.Module.ActivityLog.Data
{
    public interface IActivityTypeRepository : IRepository<Activity>
    {
        IQueryable<MostViewEntityDto> List();
    }
}
