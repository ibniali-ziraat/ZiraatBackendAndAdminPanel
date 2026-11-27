using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Core.Areas.Core.Controllers;

[Area("Core")]
[Route("api/core/userProfile")]
[ApiController]
public class UserProfileController : ControllerBase
{
    private readonly IProfileService _userProfileService;
    private readonly ILogger<UserProfileController> _logger;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public UserProfileController(
        IProfileService userProfileService,
        ILogger<UserProfileController> logger,
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        _userProfileService = userProfileService;
        _logger = logger;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // Create profile + identity user
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateProfile([FromBody] UserProfile model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(model.Password) || model.Password != model.Confirmpassword)
        {
            return BadRequest(new { Message = "Password is required and must match ConfirmPassword." });
        }
        var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == model.Phone && u.IsDeleted != true);
        if (phoneExists)
            return BadRequest(new { Message = "Duplicate phone number found." });

        var emailExists = await _userManager.Users.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.IsDeleted != true);
        if (emailExists)
            return BadRequest(new { Message = "Email already exists." });
        try
        {
            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.Fullname,
                PhoneNumber = model.Phone,
                LatestUpdatedOn = DateTimeOffset.Now
            };

            var identityResult = await _userManager.CreateAsync(user, model.Password);
            if (!identityResult.Succeeded)
            {
                return BadRequest(new { Message = "Failed to create user", errors = identityResult.Errors });
            }

            await _userManager.AddToRoleAsync(user, "customer");

            var createdUser = await _userManager.FindByEmailAsync(model.Email);
            if (createdUser == null)
            {
                return StatusCode(500, new { Message = "Failed to retrieve created user." });
            }

            var result = await _userProfileService.CreateAsync(model, createdUser.Id);
            if (!result.Success)
            {
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("New user created (Id: {UserId}) with email {Email} and profile saved.", createdUser.Id, createdUser.Email);

            return Ok(new { success = true, userId = result.Value, email = createdUser.Email, StatusCode = StatusCodes.Status200OK });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating profile");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    //Check for duplicate email and phone number
    [HttpPost("[action]")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckExistingEmailandPhoneNumber([FromBody] ExistingEmailPhoneNumberViewModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var Exists = await _userManager.Users.AnyAsync(u => (u.PhoneNumber == model.Phone || u.Email.ToLower() == model.Email.ToLower()) && u.IsDeleted != true);
        if (Exists)
            return BadRequest(new { Message = "Duplicate email or phone number found." });


        return Ok(new { success = true, StatusCode = StatusCodes.Status200OK });

    }

    // Get current user's profile
    [HttpGet("GetProfileByUserId")]
    [Authorize]
    public async Task<IActionResult> GetProfileByUserId(long userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return Unauthorized();

        var profile = await _userProfileService.GetByUserIdAsync(user.Id);
        if (profile == null) return NotFound();

        return Ok(profile);
    }

    // Get profile by profile id
    [HttpGet("{id:long}")]
    [Authorize]
    public async Task<IActionResult> GetById(long id)
    {
        var profile = await _userProfileService.GetByIdAsync(id);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    // List all profiles (admin)
    [HttpGet("all")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll()
    {
        var profiles = await _userProfileService.GetAllAsync();
        return Ok(profiles);
    }

    // Update profile
    [HttpPut("{id:long}")]
    [Authorize]
    public async Task<IActionResult> Update(long id, [FromBody] UserProfile model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existing = await _userProfileService.GetByIdAsync(id);
        if (existing == null) return NotFound(new { Message = "Profile not found" });

        try
        {
            // Get the associated user
            var user = await _userManager.FindByIdAsync(existing.CoreUserId.ToString());
            if (user == null) return NotFound(new { Message = "Associated user not found" });



            // Update User entity fields
            user.FullName = model.Fullname;
            user.Email = model.Email;
            user.UserName = model.Email;  // Keep username in sync with email
            user.PhoneNumber = model.Phone;
            user.LatestUpdatedOn = DateTimeOffset.Now;

            // Save changes to User entity
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogWarning("Failed to update user {UserId}: {Errors}", user.Id, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                var emailExists = await _userManager.Users.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower());
                if (emailExists)
                    return BadRequest(new { Message = "Email already in use." });
                return BadRequest(new { Message = "Failed to update user" });
            }

            // Update the profile service data
            await _userProfileService.UpdateAsync(id, model);

            _logger.LogInformation("Profile {ProfileId} and User {UserId} updated successfully.", id, user.Id);
            return Ok(new { success = true, message = "Profile and user information updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating profile id {ProfileId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update user password separately
    /// </summary>
    [HttpPost("{id:long}/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(long id, [FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { Message = "Current password and new password are required" });
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest(new { Message = "New password and confirm password do not match" });
        }

        if (request.NewPassword.Length < 8)
        {
            return BadRequest(new { Message = "New password must be at least 8 characters long" });
        }

        try
        {
            var existing = await _userProfileService.GetByIdAsync(id);
            if (existing == null) return NotFound(new { Message = "Profile not found" });

            var user = await _userManager.FindByIdAsync(existing.CoreUserId.ToString());
            if (user == null) return NotFound(new { Message = "Associated user not found" });

            // Change password using UserManager (which handles hashing)
            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to change password for user {UserId}: {Errors}", user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(new { Message = "Failed to change password", errors = result.Errors });
            }

            _logger.LogInformation("Password changed successfully for User {UserId}", user.Id);
            return Ok(new { success = true, message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while changing password for profile id {ProfileId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    // Delete profile
    [HttpDelete("{id:long}")]
    [Authorize]
    public async Task<IActionResult> Delete(long id)
    {
        var existing = await _userProfileService.GetByIdAsync(id);
        if (existing == null) return NotFound();

        try
        {
            await _userProfileService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting profile id {ProfileId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }
    

}
