using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Extensions;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [ApiController]
    [Route("api/vendors/{vendorId:long}/customers")]
    [Authorize(Roles = "admin, vendor")]
    public class VendorCustomerAssociationApiController : ControllerBase
    {
        private readonly IVendorCustomerAssociationService _vendorCustomerAssociationService;
        private readonly IWorkContext _workContext;

        public VendorCustomerAssociationApiController(
            IVendorCustomerAssociationService vendorCustomerAssociationService,
            IWorkContext workContext)
        {
            _vendorCustomerAssociationService = vendorCustomerAssociationService;
            _workContext = workContext;
        }

        /// <summary>
        /// Search customer by phone number
        /// </summary>
        [HttpPost("search")]
        public async Task<IActionResult> SearchCustomer(long vendorId, [FromBody] SearchCustomerVm model)
        
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return Forbid("You don't have permission to access this vendor's data");
                }

                var customer = await _vendorCustomerAssociationService.SearchCustomerByPhoneNumber(model.PhoneNumber);

                if (customer == null)
                {
                    return NotFound(new { message = "Customer not found" });
                }

                return Ok(new
                {
                    customerId = customer.Id,
                    name = customer.FullName,
                    email = customer.Email,
                    phoneNumber = customer.PhoneNumber
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Add a new customer association
        /// </summary>
        [HttpPost("add")]
        public async Task<IActionResult> AddCustomer(long vendorId, [FromBody] AddCustomerVm model)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return Forbid("You don't have permission to manage this vendor's customers");
                }

                await _vendorCustomerAssociationService.AddCustomer(vendorId, model, currentUser.Id);

                return Ok(new
                {
                    message = "Customer added successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        [HttpPost("update")]
        public async Task<IActionResult> UpdateCustomer(long vendorId, [FromBody] UpdateCustomerVm model)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return Forbid("You don't have permission to manage this vendor's customers");
                }

                await _vendorCustomerAssociationService.UpdateCustomer(vendorId, model, currentUser.Id);

                return Ok(new
                {
                    message = "Customer Updated successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("updateCustomerBalance")]
        public async Task<IActionResult> UpdateCustomerBalance(long vendorId, [FromBody] UpdateCustomerBalance model)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return Forbid("You don't have permission to manage this vendor's customers");
                }

                await _vendorCustomerAssociationService.UpdateCustomerBalance(vendorId, model);

                return Ok(new
                {
                    message = "Customer balance Updated successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        /// <summary>
        /// Delete a customer association
        /// </summary>
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteCustomer(long vendorId, [FromBody] DeleteCustomerVm model)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return Forbid("You don't have permission to manage this vendor's customers");
                }

                await _vendorCustomerAssociationService.DeleteCustomer(vendorId, model.CustomerId);

                return Ok(new { message = "Customer deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all customers for a vendor
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllCustomers(long vendorId)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return Forbid("You don't have permission to access this vendor's data");
                }

                var customers = await _vendorCustomerAssociationService.GetAllCustomers(vendorId);

                return Ok(new { customers });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Link or unlink a customer
        /// </summary>
        [HttpPost("link-unlink")]
        public async Task<IActionResult> LinkUnlinkCustomer(long vendorId, [FromBody] LinkUnlinkCustomerVm model)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return Forbid("You don't have permission to manage this vendor's customers");
                }

                await _vendorCustomerAssociationService.LinkUnlinkCustomer(
                    vendorId,
                    model.CustomerId,
                    model.LinkUnlink,
                    currentUser.Id);

                var status = model.LinkUnlink ? "linked" : "unlinked";
                return Ok(new { message = $"Customer {status} successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all customer orders for this vendor regardless of payment method
        /// </summary>
        [HttpGet("{customerId:long}/orders")]
        public async Task<IActionResult> GetCustomerOrders(long vendorId, long customerId)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return Forbid("You don't have permission to access this vendor's data");
                }

                var orders = await _vendorCustomerAssociationService.GetCustomerOrders(vendorId, customerId);

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get comprehensive BuyOnCredit order summary for customer including total orders, amounts, and settlements
        /// </summary>
        [HttpGet("{customerId:long}/BuyOnCredit/orders")]
        public async Task<IActionResult> GetCustomerBuyOnCreditOrders(long vendorId, long customerId)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return Forbid("You don't have permission to access this vendor's data");
                }

                // Get comprehensive BuyOnCredit order data with calculations
                var buyOnCreditOrders = await _vendorCustomerAssociationService.GetCustomerBuyOnCreditOrders(vendorId, customerId);

                return Ok(new
                {
                    vendorId = buyOnCreditOrders.VendorId,
                    customerId = buyOnCreditOrders.CustomerId,
                    totalCreditOrders = buyOnCreditOrders.TotalCreditOrders,
                    totalCreditOrderAmount = buyOnCreditOrders.TotalCreditOrderAmount,
                    totalSettledAmount = buyOnCreditOrders.TotalSettledAmount,
                    remainingBalance = buyOnCreditOrders.RemainingBalance,
                    orders = buyOnCreditOrders.Orders
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Add settlement payment
        /// </summary>
        [HttpPost("{customerId:long}/settlement/add")]
        public async Task<IActionResult> AddSettlement(long vendorId, long customerId, [FromBody] AddSettlementVm model)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return Forbid("You don't have permission to manage this vendor's settlements");
                }

                await _vendorCustomerAssociationService.AddSettlement(vendorId, customerId, model, currentUser.Id);

                return Ok(new
                {
                    message = "Settlement recorded successfully"

                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get settlement summary for BuyOnCredit orders
        /// </summary>
        [HttpGet("{customerId:long}/settlement/summary")]
        public async Task<IActionResult> GetSettlementSummary(long vendorId, long customerId)
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (!User.IsInRole("admin") && currentUser.VendorId != vendorId)
                {
                    return Forbid("You don't have permission to access this vendor's data");
                }

                var summary = await _vendorCustomerAssociationService.GetSettlementSummary(vendorId, customerId);

                return Ok(new { getSettlementSummary = summary });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
