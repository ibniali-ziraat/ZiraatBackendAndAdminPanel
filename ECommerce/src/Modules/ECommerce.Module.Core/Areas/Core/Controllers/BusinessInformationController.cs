using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [ApiController]
    [Route("api/core/[controller]")]
    public class BusinessInformationController : ControllerBase
    {
        private readonly IBusinessInformationService _service;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IEntityService _entityService;
        private readonly IRepository<Role> _roleRepository;

        private const string VendorEntityTypeId = "Vendor";

        public BusinessInformationController(
            IBusinessInformationService service,
            IRepository<Vendor> vendorRepository,
            IRepository<User> userRepository,
            IEntityService entityService,
            IRepository<Role> roleRepository
        )
        {
            _service = service;
            _vendorRepository = vendorRepository;
            _userRepository = userRepository;
            _entityService = entityService;
            _roleRepository = roleRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetBusinessInformationByUserId(long userId)
        {
            var item = await _service.GetBusinessInformationByUserId(userId);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(BusinessInformationViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var entity = new BusinessInformation
            {
                UserId = model.UserId,
                ShopName = model.ShopName,
                OwnerName = model.OwnerName,
                ContactNumberPrimary = model.ContactNumberPrimary,
                ContactNumberAlternate = model.ContactNumberAlternate,
                Email = model.Email,
                Village = model.Village,
                Tehsil = model.Tehsil,
                District = model.District,
                State = model.State,
                Pincode = model.Pincode,
                Latitude = model.Latitude,
                Longitude = model.Longitude
            };

            await _service.AddAsync(entity);

            // Create vendor profile from business information
            using (var transaction = _vendorRepository.BeginTransaction())
            {
                var vendorSlug = _entityService.ToSafeSlug(model.ShopName.ToLower().Replace(" ", "-"), 0, VendorEntityTypeId);

                var vendor = new Vendor
                {
                    Name = model.ShopName,
                    Email = model.Email,
                    Slug = vendorSlug,
                    IsActive = true
                };

                _vendorRepository.Add(vendor);
                await _vendorRepository.SaveChangesAsync();

                _entityService.Add(vendor.Name, vendor.Slug, vendor.Id, VendorEntityTypeId);
                await _vendorRepository.SaveChangesAsync();

                // Update business information with vendor ID
                entity.VendorId = vendor.Id;
                await _service.UpdateAsync(entity);

                // Update user record with vendor ID and assign vendor role
                    var user = await _userRepository.Query()
                        .Include(x => x.Roles)
                        .FirstOrDefaultAsync(x => x.Id == model.UserId);

                    if (user != null)
                    {
                        user.VendorId = vendor.Id;

                        // Get or find the vendor role
                        var vendorRole = await _roleRepository.Query()
                            .FirstOrDefaultAsync(x => x.Name == "vendor");

                        if (vendorRole != null)
                        {
                            // Check if user already has the vendor role
                            var hasVendorRole = user.Roles.Any(x => x.RoleId == vendorRole.Id);
                            if (!hasVendorRole)
                            {
                                var userRole = new UserRole
                                {
                                    UserId = user.Id,
                                    RoleId = vendorRole.Id
                                };
                                user.Roles.Add(userRole);
                            }
                        }

                        await _userRepository.SaveChangesAsync();
                    }
                
                    transaction.Commit();

                return Ok(new { id = entity.Id, vendorId = vendor.Id, entity });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, BusinessInformationViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != model.Id) return BadRequest("Id mismatch.");

            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.UserId = model.UserId;
            existing.ShopName = model.ShopName;
            existing.OwnerName = model.OwnerName;
            existing.ContactNumberPrimary = model.ContactNumberPrimary;
            existing.ContactNumberAlternate = model.ContactNumberAlternate;
            existing.Email = model.Email;
            existing.Village = model.Village;
            existing.Tehsil = model.Tehsil;
            existing.District = model.District;
            existing.State = model.State;
            existing.Pincode = model.Pincode;
            existing.Latitude = model.Latitude;
            existing.Longitude = model.Longitude;

            await _service.UpdateAsync(existing);
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
    }
}
