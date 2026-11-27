using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Services;


namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [Route("api/core/landholding")]
    [ApiController]
    public class LandholdingController : ControllerBase
    {
        private readonly ILandholdingService _service;
        public LandholdingController(ILandholdingService service) { _service = service; }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> Get(long id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpGet("by-profile/{profileId:long}")]
        public async Task<IActionResult> GetByProfileId(long profileId)
        {
            var item = await _service.GetByProfileIdAsync(profileId);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LandholdingViewModel model)
        {
            await _service.CreateAsync(model);
            return Ok();
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] LandholdingViewModel model)
        {
            await _service.UpdateAsync(id, model);
            return Accepted();
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
