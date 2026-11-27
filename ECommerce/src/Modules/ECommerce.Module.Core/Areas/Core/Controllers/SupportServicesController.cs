using System;
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
    public class SupportServicesController : ControllerBase
    {
        private readonly ISupportServicesService _service;

        public SupportServicesController(ISupportServicesService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _service.GetAllAsync();
            var dtos = items.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SupportServicesViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var entity = MapVmToEntity(model);
            await _service.AddAsync(entity);

            var saved = await _service.GetByIdAsync(entity.Id);
            return Ok(new { success = true, id = saved.Id, saved, status = StatusCodes.Status200OK });
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] SupportServicesViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (model.Id.HasValue && id != model.SupplierProfileId) return BadRequest("Id mismatch.");

            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound();

            var updated = MapVmToEntity(model, existing);
            await _service.UpdateAsync(updated);
            return Ok(new { success = true, status = StatusCodes.Status200OK });
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound();
            await _service.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("save-detailed")]
        public async Task<IActionResult> SaveDetailed([FromBody] SupportServicesViewModel vm)
        {
            if (vm == null) return BadRequest("Missing payload.");

            var entity = MapVmToEntity(vm);
            if (entity.Id == 0)
            {
                await _service.AddAsync(entity);
            }
            else
            {
                var existing = await _service.GetByIdAsync(entity.Id);
                if (existing == null) return NotFound();
                entity = MapVmToEntity(vm, existing);
                await _service.UpdateAsync(entity);
            }

            var saved = await _service.GetByIdAsync(entity.SupplierProfileId);
            return Ok(new { success = true, id = entity.Id, saved, status = StatusCodes.Status200OK });
        }

        // Helper: map view model -> domain entity. If `target` supplied, update it; otherwise create new.
        private static SupportServices MapVmToEntity(SupportServicesViewModel vm, SupportServices target = null)
        {
            var entity = target ?? new SupportServices();

            entity.Id = vm.Id ?? entity.Id;
            entity.SupplierProfileId = vm.SupplierProfileId;

            entity.AdvisorySupport = vm.AdvisorySupport;
            entity.FieldStaffAvailable = vm.FieldStaffAvailable;
            entity.FieldStaffCount = vm.FieldStaffCount;

            entity.AfterSalesService = vm.AfterSalesService;

            if (!string.IsNullOrWhiteSpace(vm.DeliveryRange) && int.TryParse(vm.DeliveryRange, out var dr))
            {
                entity.DeliveryRange = dr;
            }
            else
            {
                if (target == null) entity.DeliveryRange = null;
            }

            entity.ComputerAvailable = vm.ComputerAvailable;
            entity.VehicleAvailable = vm.VehicleAvailable;

            return entity;
        }

        // Map domain -> response DTO (no back-references)
        private static SupportServicesResponseVm MapToDto(SupportServices s) =>
            new SupportServicesResponseVm
            {
                Id = s.Id,
                SupplierProfileId = s.SupplierProfileId,
                AgronomyAdvisorySupport = s.AdvisorySupport,
                FieldStaffAvailable = s.FieldStaffAvailable,
                NumberOfStaff = s.FieldStaffCount,
                AfterSalesService = s.AfterSalesService,
                PreferredDeliveryAreaRangeSqKm = s.DeliveryRange,
                ComputerFacilityAvailable = s.ComputerAvailable,
                DeliveryVehicleAvailable = s.VehicleAvailable
            };
    }
}
