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
    public class ProductPortfolioController : ControllerBase
    {
        private readonly IProductPortfolioService _service;

        public ProductPortfolioController(IProductPortfolioService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _service.GetAllAsync();
            var dtos = items.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        // Get by portfolio primary key (Id)
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(MapToDto(item));
        }

        // Get portfolio by supplier profile id
        [HttpGet("by-supplier/{supplierProfileId:long}")]
        public async Task<IActionResult> GetBySupplier(long supplierProfileId)
        {
            var item = await _service.GetBySupplierProfileIdAsync(supplierProfileId);
            if (item == null) return NotFound();
            return Ok(MapToDto(item));
        }

        // Create portfolio (supports child collections when supplied in VM)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductPortfolioViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var portfolio = new ProductPortfolio
            {
                SupplierProfileId = model.SupplierProfileId,
                StorageCapacityValue = model.StorageCapacity
            };

            var inputTypes = (model.InputTypes ?? Enumerable.Empty<string>()).Select(s => new ProductInputType { InputType = s }).ToList();
            var brands = (model.MajorBrands ?? Enumerable.Empty<string>()).Select(s => new ProductMajorBrand { BrandName = s }).ToList();
            var customs = (model.CustomInputs ?? Enumerable.Empty<ProductCustomInputDto>()).Select(d => new ProductCustomInput
            {
                ProductName = d.ProductName,
                StockQuantity = d.StockQuantity,
                Unit = d.Unit
            }).ToList();

            // Use SaveWithDetailsAsync to persist parent + children in one transaction
            await _service.SaveWithDetailsAsync(portfolio, inputTypes, brands, customs);

            var saved = await _service.GetByIdAsync(portfolio.Id);
            return Ok(new { id = saved.Id, mapped = MapToDto(saved) });
        }

        // Update portfolio (replaces child collections)
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] ProductPortfolioViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (model.Id.HasValue && id != model.SupplierProfileId) return BadRequest("Id mismatch.");

            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound();

            var portfolio = new ProductPortfolio
            {
                Id = existing.Id,
                SupplierProfileId = model.SupplierProfileId,
                StorageCapacityValue = model.StorageCapacity
            };

            var inputTypes = (model.InputTypes ?? Enumerable.Empty<string>()).Select(s => new ProductInputType { InputType = s }).ToList();
            var brands = (model.MajorBrands ?? Enumerable.Empty<string>()).Select(s => new ProductMajorBrand { BrandName = s }).ToList();
            var customs = (model.CustomInputs ?? Enumerable.Empty<ProductCustomInputDto>()).Select(d => new ProductCustomInput
            {
                Id = d.Id,
                ProductName = d.ProductName,
                StockQuantity = d.StockQuantity,
                Unit = d.Unit
            }).ToList();

            await _service.SaveWithDetailsAsync(portfolio, inputTypes, brands, customs);

            return Ok(new { success = true, status = StatusCodes.Status200OK });
        }


        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound();

            await _service.SaveWithDetailsAsync(existing, Enumerable.Empty<ProductInputType>(), Enumerable.Empty<ProductMajorBrand>(), Enumerable.Empty<ProductCustomInput>());

            await _service.DeleteAsync(id);
            return NoContent();
        }

        // Accept the same payload shape used elsewhere — useful when clients send a full portfolio object
        [HttpPost("save-productportfolio")]
        public async Task<IActionResult> SaveDetailed([FromBody] ProductPortfolioViewModel request)
        {
            var portfolio = new ProductPortfolio
            {
                Id = request.Id ?? 0,
                SupplierProfileId = request.SupplierProfileId,
                StorageCapacityValue = request.StorageCapacity
            };

            var inputTypes = (request.InputTypes ?? Enumerable.Empty<string>()).Select(s => new ProductInputType { InputType = s }).ToList();
            var brands = (request.MajorBrands ?? Enumerable.Empty<string>()).Select(s => new ProductMajorBrand { BrandName = s }).ToList();
            var customs = (request.CustomInputs ?? Enumerable.Empty<ProductCustomInputDto>()).Select(d => new ProductCustomInput
            {
                Id = d.Id,
                ProductName = d.ProductName,
                StockQuantity = d.StockQuantity,
                Unit = d.Unit
            }).ToList();

            await _service.SaveWithDetailsAsync(portfolio, inputTypes, brands, customs);

            var saved = await _service.GetByIdAsync(portfolio.SupplierProfileId);
            if (saved == null) return NotFound();

            return Ok(new { id = portfolio.Id, mappedObject = MapToDto(saved) });
        }

        // Map domain -> response DTO (no back-references)
        private static ProductPortfolioResponseVm MapToDto(ProductPortfolio p) =>
            new ProductPortfolioResponseVm
            {
                Id = p.Id,
                SupplierProfileId = p.SupplierProfileId,
                StorageCapacityValue = p.StorageCapacityValue,
                StorageCapacityUnit = p.StorageCapacityUnit,
                InputTypes = (p.InputTypes ?? Enumerable.Empty<ProductInputType>())
                    .Select(it => new ProductInputTypeDto { Id = it.Id, InputType = it.InputType })
                    .ToList(),
                MajorBrands = (p.MajorBrands ?? Enumerable.Empty<ProductMajorBrand>())
                    .Select(b => new ProductMajorBrandDto { Id = b.Id, BrandName = b.BrandName })
                    .ToList(),
                CustomInputs = (p.CustomInputs ?? Enumerable.Empty<ProductCustomInput>())
                    .Select(c => new ProductCustomInputDto { Id = c.Id, ProductName = c.ProductName, StockQuantity = c.StockQuantity, Unit = c.Unit })
                    .ToList()
            };
    }
}
