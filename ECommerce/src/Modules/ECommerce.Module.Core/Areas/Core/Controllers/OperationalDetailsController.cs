using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [ApiController]
    [Route("api/core/[controller]")]
    public class OperationalDetailsController : ControllerBase
    {
        private readonly IOperationalDetailsService _service;
        public OperationalDetailsController(IOperationalDetailsService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _service.GetAllAsync();
            var dtos = items.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<OperationalDetailsResponseVm>> GetById(long id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(MapToDto(item));
        }

        [HttpPost]
        public async Task<IActionResult> Create(OperationalDetailsViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var entity = new OperationalDetails
            {
                SupplierProfileId = model.SupplierProfileId,
                YearsInBusiness = model.YearsInBusiness,
                DeliveryServiceAvailable = model.DeliveryServiceAvailable,
                AvgMonthlySalesVolume = model.AvgMonthlySalesVolume
            };

            await _service.AddAsync(entity);

            var saved = await _service.GetByIdAsync(entity.Id);
            return Ok(new { success = true, id = saved.Id, mappedObject = MapToDto(saved), status = StatusCodes.Status200OK });
        }

        // Updated: accept body, handle lists and replace child collections using service's SaveWithDetailsAsync
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] OperationalDetailsViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (model.OperationalId.HasValue && id != model.SupplierProfileId) return BadRequest("Id mismatch.");

            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound();

            // Map incoming VM to domain parent
            var details = new OperationalDetails
            {
                Id = existing.Id,
                SupplierProfileId = model.SupplierProfileId,
                YearsInBusiness = model.YearsInBusiness,
                DeliveryServiceAvailable = model.DeliveryServiceAvailable,
                AvgMonthlySalesVolume = model.AvgMonthlySalesVolume
            };

            // Map lists to child entities (will replace existing children in SaveWithDetailsAsync)
            var serviceAreas = (model.ServiceArea ?? Enumerable.Empty<string>()).Select(s => new ServiceArea { PlaceName = s }).ToList();
            var tieups = (model.CompanyTieups ?? Enumerable.Empty<string>()).Select(s => new CompanyTieup { Name = s }).ToList();

            await _service.SaveWithDetailsAsync(details, serviceAreas, tieups);

            return Ok(new { success = true, status = StatusCodes.Status200OK });
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound();
            await _service.SaveWithDetailsAsync(existing, Enumerable.Empty<ServiceArea>(), Enumerable.Empty<CompanyTieup>());

            await _service.DeleteAsync(id);
            return NoContent();
        }

        // New endpoint to accept the requested payload structure and save details + child collections
        [HttpPost("save-operationalDetails")]
        public async Task<IActionResult> SaveDetailed([FromBody] OperationalDetailsViewModel request)
        {
            var vm = request;

            var details = new OperationalDetails
            {
                Id = vm.OperationalId ?? 0,
                SupplierProfileId = vm.SupplierProfileId,
                YearsInBusiness = vm.YearsInBusiness,
                DeliveryServiceAvailable = vm.DeliveryServiceAvailable,
                AvgMonthlySalesVolume = vm.AvgMonthlySalesVolume
            };

            var serviceAreas = (vm.ServiceArea ?? Enumerable.Empty<string>()).Select(s => new ServiceArea { PlaceName = s }).ToList();
            var tieups = (vm.CompanyTieups ?? Enumerable.Empty<string>()).Select(s => new CompanyTieup { Name = s }).ToList();

            await _service.SaveWithDetailsAsync(details, serviceAreas, tieups);

            // Fetch saved entity and project to a DTO to avoid self-referencing loop during JSON serialization
            var saved = await _service.GetByIdAsync(details.SupplierProfileId);
            if (saved == null) return NotFound();

            var response = MapToDto(saved);

            return Ok(new { success = true, id = response.Id, data = response, status = StatusCodes.Status200OK });
        }

        // Map domain -> response DTO (no back-references)
        private static OperationalDetailsResponseVm MapToDto(OperationalDetails d) =>
            new OperationalDetailsResponseVm
            {
                Id = d.Id,
                SupplierProfileId = d.SupplierProfileId,
                YearsInBusiness = d.YearsInBusiness,
                DeliveryServiceAvailable = d.DeliveryServiceAvailable,
                AvgMonthlySalesVolume = d.AvgMonthlySalesVolume,
                ServiceAreas = (d.ServiceAreas ?? Enumerable.Empty<ServiceArea>())
                    .Select(sa => new ServiceAreaDto { Id = sa.Id, PlaceName = sa.PlaceName })
                    .ToList(),
                CompanyTieups = (d.CompanyTieups ?? Enumerable.Empty<CompanyTieup>())
                    .Select(ct => new CompanyTieupDto { Id = ct.Id, Name = ct.Name })
                    .ToList()
            };
    }
}
