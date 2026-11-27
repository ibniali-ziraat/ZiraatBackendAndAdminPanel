using System.Collections.Generic;
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
    public class FinancialPaymentController : ControllerBase
    {
        private readonly IFinancialPaymentService _service;

        public FinancialPaymentController(IFinancialPaymentService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _service.GetAllAsync();
            var dtos = items
                .Where(x => x != null)  // Filter out null items
                .Select(MapToDto)
                .ToList();
            return Ok(dtos);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(MapToDto(item));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FinancialPaymentViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var entity = new FinancialPayment
            {
                SupplierProfileId = model.SupplierProfileId,
                UpiAvailable = model.UpiAvailable,
                CreditFacility = model.CreditFacility,
                SellingViaZiraat = model.SellingViaZiraat
            };

            await _service.AddAsync(entity);

            var saved = await _service.GetByIdAsync(entity.Id);
            if (saved == null) return BadRequest("Failed to retrieve saved payment.");

            return Ok(new
            {
                success = true,
                id = saved.Id,
                mappedObject = MapToDto(saved),
                status = StatusCodes.Status200OK
            });
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] FinancialPaymentViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (model.Id.HasValue && id != model.Id) return BadRequest("Id mismatch.");

            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound();

            var payment = new FinancialPayment
            {
                Id = existing.Id,
                SupplierProfileId = model.SupplierProfileId,
                UpiAvailable = model.UpiAvailable,
                CreditFacility = model.CreditFacility,
                SellingViaZiraat = model.SellingViaZiraat
            };

            await _service.UpdateAsync(payment);

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

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] FinancialPaymentViewModel model)
        {
            if (model == null) return BadRequest("Missing payload.");

            var payment = new FinancialPayment
            {
                Id = model.Id ?? 0,
                SupplierProfileId = model.SupplierProfileId,
                UpiAvailable = model.UpiAvailable,
                CreditFacility = model.CreditFacility,
                SellingViaZiraat = model.SellingViaZiraat
            };

            await _service.SaveWithBankDetailsAsync(payment);
            return Ok(new { success = true, status = StatusCodes.Status200OK });

            //var saved = await _service.GetByIdAsync(payment.Id);
            //if (saved == null) return BadRequest("Failed to retrieve saved payment.");

            //return Ok(new
            //{
            //    success = true,
            //    id = payment.Id,
            //    mappedObject = MapToDto(saved),
            //    status = StatusCodes.Status200OK
            //});
        }

        [HttpPost("{id:long}/bank-details")]
        public async Task<IActionResult> AddBankDetail(long id, [FromBody] BankDetailVm model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // var existing = await _service.GetByIdAsync(id);
            // if (existing == null) return NotFound();

            var bankDetail = new BankDetails
            {
                FinancialPaymentId = id,
                AccountHolderName = model.AccountHolder,
                AccountNumber = model.AccountNumber,
                IFSCCode = model.Ifsc,
                BankName = model.BankName,
                BranchName = model.BranchName,
                IsActive = model.IsActive
            };

            await _service.AddBankDetailAsync(bankDetail);

            return Ok(new
            {
                success = true,
                id = bankDetail.Id,
                status = StatusCodes.Status200OK
            });
        }


        [HttpPut("{id:long}/bank-details/{bankDetailId:long}")]
        public async Task<IActionResult> UpdateBankDetail(long id, long bankDetailId, [FromBody] BankDetailVm model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var existing = await _service.GetBankDetailsByIdAsync(bankDetailId);
                if (existing != null)
                {

                    var bankDetail = new BankDetails
                    {
                        FinancialPaymentId = id,
                        AccountHolderName = model.AccountHolder,
                        AccountNumber = model.AccountNumber,
                        IFSCCode = model.Ifsc,
                        BankName = model.BankName,
                        BranchName = model.BranchName,
                        IsActive = model.IsActive
                    };
                    await _service.UpdateBankDetailAsync(bankDetail, bankDetailId);
                    return Ok(new { success = true, status = StatusCodes.Status200OK });

                }
                return Ok(new { success = true, status = StatusCodes.Status404NotFound });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id:long}/bank-details/{bankDetailId:long}")]
        public async Task<IActionResult> DeleteBankDetail(long id, long bankDetailId)
        {
            var existing = await _service.GetBankDetailsByIdAsync(bankDetailId);
            if (existing == null) return NotFound();

            await _service.DeleteBankDetailAsync(bankDetailId);

            return NoContent();
        }

        [HttpPatch("{id:long}/bank-details/{bankDetailId:long}/toggle-status")]
        public async Task<IActionResult> ToggleBankDetailStatus(long id, long bankDetailId, [FromBody] ToggleBankDetailStatusViewModel model)
        {
            var existing = await _service.GetBankDetailsByIdAsync(bankDetailId);
            if (existing == null) return NotFound();

            if (model == null || !model.IsActive.HasValue) return BadRequest("IsActive status is required.");

            await _service.ToggleBankDetailStatusAsync(bankDetailId, model.IsActive.Value);

            return Ok(new { success = true, status = StatusCodes.Status200OK });
        }

        private static object MapToDto(FinancialPayment p)
        {
            // Guard against null - though this should now be handled by callers
            if (p == null) throw new ArgumentNullException(nameof(p), "FinancialPayment cannot be null");

            return new
            {
                Id = p.Id,
                SupplierProfileId = p.SupplierProfileId,
                UpiAvailable = p.UpiAvailable,
                CreditFacility = p.CreditFacility,
                SellingViaZiraat = p.SellingViaZiraat,
                BankDetails = (p.BankDetails ?? Enumerable.Empty<BankDetails>())
                    .Select(bd => new
                    {
                        Id = bd.Id,
                        FinancialPaymentId = bd.FinancialPaymentId,
                        AccountHolderName = bd.AccountHolderName,
                        AccountNumber = bd.AccountNumber,
                        IFSCCode = bd.IFSCCode,
                        BankName = bd.BankName,
                        BranchName = bd.BranchName,
                        IsActive = bd.IsActive
                    }).ToList()
            };
        }
    }
}
