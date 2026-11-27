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
    public class CustomerReviewController : ControllerBase
    {
        private readonly ICustomerReviewService _service;
        public CustomerReviewController(ICustomerReviewService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id:long}")]
        public async Task<ActionResult<CustomerReview>> GetById(long id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CustomerReviewViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var entity = new CustomerReview
            {
                VerificationId = model.VerificationId,
                FarmerName = model.FarmerName,
                Rating = model.Rating,
                Comment = model.Comment,
                ReviewDate = model.ReviewDate
            };

            await _service.AddAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, CustomerReviewViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != model.ReviewId) return BadRequest("Id mismatch.");

            var entity = await _service.GetByIdAsync(id);
            if (entity == null) return NotFound();

            entity.FarmerName = model.FarmerName;
            entity.Rating = model.Rating;
            entity.Comment = model.Comment;
            entity.ReviewDate = model.ReviewDate;

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
