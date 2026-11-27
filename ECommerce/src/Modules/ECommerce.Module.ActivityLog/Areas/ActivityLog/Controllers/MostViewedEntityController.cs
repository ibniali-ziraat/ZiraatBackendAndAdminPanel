using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Module.ActivityLog.Data;
using ECommerce.Module.ActivityLog.Models;

namespace ECommerce.Module.ActivityLog.Areas.ActivityLog.Controllers
{
    [Area("ActivityLog")]
    [ApiController]

    [Authorize(Roles = "admin")]
    [Route("api/activitylog")]
    public class MostViewedEntityController : ControllerBase
    {
        private readonly IActivityTypeRepository _activityTypeRepository;

        public MostViewedEntityController(IActivityTypeRepository activityTypeRepository)
        {
            _activityTypeRepository = activityTypeRepository;
        }

        [HttpGet("most-viewed-entities/{entityTypeId}")]
        public async Task<IList<MostViewEntityDto>> GetMostViewedEntities(string entityTypeId)
        {
            return await _activityTypeRepository.List().Where(x => x.EntityTypeId == entityTypeId).Take(10).ToListAsync();
        }
    }
}
