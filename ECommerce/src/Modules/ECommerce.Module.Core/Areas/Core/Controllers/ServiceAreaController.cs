using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [ApiController]
    [Route("api/core/[controller]")]
    public class ServiceAreaController : ControllerBase
    {
        private readonly IServiceAreaService _service;
        public ServiceAreaController(IServiceAreaService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ServiceArea>> GetById(long id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ServiceAreaViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var entity = new ServiceArea
            {
                OperationalDetailsId = model.OperationalId,
                PlaceName = model.PlaceName
            };

            await _service.AddAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, ServiceAreaViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != model.ServiceAreaId) return BadRequest("Id mismatch.");

            var entity = await _service.GetByIdAsync(id);
            if (entity == null) return NotFound();

            entity.PlaceName = model.PlaceName;
            await _service.UpdateAsync(entity);
            return NoContent();
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound();
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
