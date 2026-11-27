using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Services;


namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [Route("api/core/professional-profile")]
    [ApiController]
    public class ProfessionalUserProfileController : ControllerBase
    {
        private readonly IProfessionService _service;
        public ProfessionalUserProfileController(IProfessionService service) { _service = service; }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> Get(long id)
        {
            var profession = await _service.GetByIdAsync(id);
            if (profession == null) return NotFound();
            return Ok(profession);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetByProfileIdAsync(long profileId)
        {
            var profession = await _service.GetByProfileIdAsync(profileId);
            if (profession == null) return NotFound();
            return Ok(profession);
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Create([FromBody] ProfessionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _service.CreateAsync(model);
            return Ok(new { success = true, StatusCode = StatusCodes.Status200OK });

        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] ProfessionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _service.UpdateAsync(id, model);
            return Ok(new { success = true, StatusCode = StatusCodes.Status200OK });

        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            await _service.DeleteAsync(id);
            return Ok(new { success = true, StatusCode = StatusCodes.Status200OK });

        }
    }
}
