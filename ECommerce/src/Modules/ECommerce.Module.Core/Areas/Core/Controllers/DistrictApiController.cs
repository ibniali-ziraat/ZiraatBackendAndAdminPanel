using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [Route("api/districts")]
    public class DistrictApiController : ControllerBase
    {
        private readonly IRepository<District> _districtRepository;

        public DistrictApiController(IRepository<District> districtRepository)
        {
            _districtRepository = districtRepository;
        }

        [HttpGet("/api/states-provinces/{stateOrProvinceId}/districts")]
        public async Task<IActionResult> GetDistricts(long stateOrProvinceId)
        {
            var districts = await _districtRepository
                .Query()
                .Where(x => x.StateOrProvinceId == stateOrProvinceId)
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Type,
                    x.Location
                }).ToListAsync();

            return new JsonResult(districts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var district = await _districtRepository
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (district == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                district.Id,
                district.Name,
                district.Type,
                district.StateOrProvinceId,
                district.Location
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DistrictRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var district = new District
            {
                Name = request.Name,
                Type = request.Type,
                StateOrProvinceId = request.StateOrProvinceId,
                Location = request.Location
            };

            _districtRepository.Add(district);
            await _districtRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = district.Id }, new
            {
                district.Id,
                district.Name,
                district.Type,
                district.StateOrProvinceId,
                district.Location
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] DistrictRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var district = await _districtRepository
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (district == null)
            {
                return NotFound();
            }

            district.Name = request.Name;
            district.Type = request.Type;
            district.StateOrProvinceId = request.StateOrProvinceId;
            district.Location = request.Location;

            await _districtRepository.SaveChangesAsync();

            return Ok(new
            {
                district.Id,
                district.Name,
                district.Type,
                district.StateOrProvinceId,
                district.Location
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var district = await _districtRepository
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (district == null)
            {
                return NotFound();
            }

            _districtRepository.Remove(district);
            await _districtRepository.SaveChangesAsync();

            return NoContent();
        }
    }

    public class DistrictRequest
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public long StateOrProvinceId { get; set; }
        public string Location { get; set; }
    }
}
