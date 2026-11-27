using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Core.Areas.Core.Controllers;

[Area("Core")]
[Route("api/core/otpverification")]
[ApiController]
public class OtpVerificationController : ControllerBase
{
    private readonly IOtpVerificationService _service;
    public OtpVerificationController(IOtpVerificationService service) { _service = service; }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    // GET api/core/otpverification/by-phone/{phoneNumber}
    [HttpGet("by-phone/{phoneNumber}")]
    public async Task<IActionResult> GetByPhoneNumber(string phoneNumber)
    {
        var item = await _service.GetByPhoneNumberAsync(phoneNumber);
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
    public async Task<IActionResult> Create([FromBody] OtpVerificationViewModel model)
    {
        await _service.CreateAsync(model);
        return Ok();
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] OtpVerificationViewModel model)
    {
        await _service.UpdateAsync(id, model);
        return Accepted();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    // POST api/core/otpverification/send
    [HttpPost("send")]
    public async Task<IActionResult> SendOtp([FromBody] OtpVerificationViewModel model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.PhoneNumber))
        {
            return BadRequest(new ApiResponse<object>(
                success: false,
                message: "Invalid request. PhoneNumber is required.",
                statusCode: StatusCodes.Status400BadRequest
            ));
        }

        var result = await _service.SendOtpAsync(model.PhoneNumber);

        if (string.IsNullOrWhiteSpace(result)) // if service failed or returned empty
        {
            return BadRequest(new ApiResponse<object>(
                success: false,
                message: "Failed to send OTP.",
                statusCode: StatusCodes.Status500InternalServerError
            ));
        }

        return Ok(new ApiResponse<object>(
            success: true,
            message: "OTP sent successfully. Check SMS provider delivery.",
            statusCode: StatusCodes.Status200OK,
            data: new { fast2smsResponse = result }
        ));
    }


    // POST api/core/otpverification/verify
    [HttpPost("verify")]
    public IActionResult VerifyOtp([FromBody] OtpVerificationViewModel model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.PhoneNumber) || string.IsNullOrWhiteSpace(model.Otp))
        {
            return BadRequest(new ApiResponse<object>(
                success: false,
                message: "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest
            ));
        }

        var result = _service.VerifyOtp(model.PhoneNumber, model.Otp);

        if (result == "OTP verified successfully")
        {
            return Ok(new ApiResponse<object>(
                success: true,
                message: result,
                statusCode: StatusCodes.Status200OK
            ));
        }
        else if (result == "OTP expired")
        {
            return BadRequest(new ApiResponse<object>(
                success: false,
                message: result,
                statusCode: StatusCodes.Status410Gone
            ));
        }
        else
        {
            return BadRequest(new ApiResponse<object>(
                success: false,
                message: result,
                statusCode: StatusCodes.Status400BadRequest
            ));
        }
    }


}
