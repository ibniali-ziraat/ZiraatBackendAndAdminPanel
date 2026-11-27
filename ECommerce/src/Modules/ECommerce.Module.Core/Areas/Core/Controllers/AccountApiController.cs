using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ECommerce.Module.Core.Areas.Core.ViewModels.Account;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Areas.Core.Controllers;

[Area("Core")]
[Route("api/core/account")]
[ApiController]
public class AccountApiController : ControllerBase
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    public AccountApiController(SignInManager<User> signInManager, UserManager<User> userManager, IConfiguration configuration, ILogger<AccountController> logger, ILoggerFactory loggerFactory)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<AccountController>();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginViewModel request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var users = await _userManager.Users
            .Where(u => !u.IsDeleted && u.PhoneNumber == request.MobileNumber)
            .Take(2)
            .ToListAsync();

        // An ambiguous active account must not be selected arbitrarily.
        if (users.Count != 1)
            return Unauthorized(new { Message = "Invalid credentials" });

        var user = users[0];
        var passwordResult = await _signInManager
            .CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

        if (!passwordResult.Succeeded)
            return Unauthorized(new { Message = "Invalid credentials" });

        var tokenResult = await GenerateJwtToken(user);
        var userRoles = await _userManager.GetRolesAsync(user);

        _logger.LogInformation("User logged in.");

        return Ok(new
        {
            success = true,
            token = tokenResult.Token,
            tokenExpiry = tokenResult.ExpiresAtUtc,
            user = user.Id,
            roles = userRoles.ToList(),
            vendorId = user.VendorId
        });
    }

    private async Task<JwtTokenResult> GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "SuperSecretDevelopmentKey12345!";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "ECommerceAPI";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "ECommerceUsers";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddDays(7);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(expires).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var userRoles = await _userManager.GetRolesAsync(user);
        foreach (var userRole in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole));
        }

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtTokenResult
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expires
        };
    }

    /// <summary>
    /// Logs off the current user.
    /// For JWT-based authentication, this primarily clears client-side tokens.
    /// The JWT token remains valid until expiration (server cannot invalidate it).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> LogOff()
    {
        try
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (user == null)
            {
                _logger.LogWarning("Logout attempt with no authenticated user found.");
                return Unauthorized(new { error = "User not found or not authenticated." });
            }

            // For JWT: SignOutAsync clears Identity cookies (if any exist)
            // Note: JWT tokens remain valid until expiration
            await _signInManager.SignOutAsync();

            _logger.LogInformation(4, "User {UserId} ({UserName}) logged out successfully.", user.Id, user.UserName);

            return Ok(new
            {
                success = true,
                message = "User logged out successfully.",
                userId = user.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during user logout.");
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while logging out. Please try again."
            });
        }
    }
}

//Token DTO
public class JwtTokenResult
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

