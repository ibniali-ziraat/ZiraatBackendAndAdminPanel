using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Services;


namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [Route("api/core/demographics")]
    [ApiController]
    public class DemographicsController : ControllerBase
    {
        private readonly IDemographicsService _service;
        public DemographicsController(IDemographicsService service) { _service = service; }

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
        public async Task<IActionResult> Create([FromBody] DemographicsViewModel model)
        {
            await _service.CreateAsync(model);
            return Ok();
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] DemographicsViewModel model)
        {
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
