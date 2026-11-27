using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [Authorize(Roles = "admin")]
    [Route("api/roles")]
    public class RoleApiController :ControllerBase
    {
        private readonly IRepository<Role> _roleRepository;

        public RoleApiController(IRepository<Role> roleRepository)
        {
            _roleRepository = roleRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var roles = await _roleRepository.Query().Select(x => new
            {
                x.Id,
                x.Name
            }).ToListAsync();

            return new JsonResult(roles);
        }
    }
}
