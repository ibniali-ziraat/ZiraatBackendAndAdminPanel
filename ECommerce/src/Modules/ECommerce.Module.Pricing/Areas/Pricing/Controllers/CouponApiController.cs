using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Pricing.Models;
using ECommerce.Module.Pricing.Services;
using ECommerce.Module.Pricing.Areas.Pricing.ViewModels;
using ECommerce.Module.Core.Extensions;

namespace ECommerce.Module.Pricing.Areas.Pricing.Controllers
{
    /// <summary>
    /// Customer-facing API for coupon discovery, validation, and application
    /// </summary>
    [Area("Pricing")]
    [ApiController]
    [Route("api/coupons")]
    [Authorize]
    public class CouponApiController : ControllerBase
    {
        private readonly IRepository<Coupon> _couponRepository;
        private readonly IRepository<CartRule> _cartRuleRepository;
        private readonly ICouponService _couponService;
        private readonly IWorkContext _workContext;

        public CouponApiController(
            IRepository<Coupon> couponRepository,
            IRepository<CartRule> cartRuleRepository,
            ICouponService couponService,
            IWorkContext workContext)
        {
            _couponRepository = couponRepository;
            _cartRuleRepository = cartRuleRepository;
            _couponService = couponService;
            _workContext = workContext;
        }

        #region Discovery & Browse Endpoints

        /// <summary>
        /// Get all available active coupons (promotional discovery)
        /// </summary>
        /// <remarks>
        /// Returns a list of all currently active coupons that customers can potentially use.
        /// Does not perform detailed validation - just shows what's available.
        /// </remarks>
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableCoupons([FromQuery] long? vendorId = null)
        {
            try
            {
                var now = DateTimeOffset.Now;
                var effectiveVendorId = vendorId;
                if (User.IsInRole("vendor"))
                {
                    var currentUser = await _workContext.GetCurrentUser();
                    if (currentUser?.VendorId == null)
                    {
                        return BadRequest(new { message = "The logged in vendor account is not linked to a vendor." });
                    }

                   // effectiveVendorId = currentUser.VendorId;
                }

                IQueryable<Coupon> query = _couponRepository.Query()
                    .Where(x => x.CartRule.IsActive &&
                               (!x.CartRule.StartOn.HasValue || x.CartRule.StartOn <= now) &&
                               (!x.CartRule.EndOn.HasValue || x.CartRule.EndOn > now))
                    .Include(x => x.CartRule)
                    .ThenInclude(cr => cr.Products)
                    .Include(x => x.CartRule)
                    .ThenInclude(cr => cr.Categories);

                if (effectiveVendorId.HasValue)
                {
                    query = query.Where(x => x.CartRule.VendorId == effectiveVendorId.Value);
                }

                var coupons = await query
                    .Select(x => new AvailableCouponVm
                    {
                        Code = x.Code,
                        VendorId = x.CartRule.VendorId,
                        RuleName = x.CartRule.Name,
                        Description = x.CartRule.Description,
                        DiscountAmount = x.CartRule.DiscountAmount,
                        RuleType = x.CartRule.RuleToApply,
                        MaxDiscountAmount = x.CartRule.MaxDiscountAmount,
                        StartDate = x.CartRule.StartOn,
                        EndDate = x.CartRule.EndOn,
                        AppliesTo = x.CartRule.Products.Any() ? $"{x.CartRule.Products.Count()} products" :
                                   x.CartRule.Categories.Any() ? $"{x.CartRule.Categories.Count()} categories" :
                                   "All products",
                        IsCouponRequired = x.CartRule.IsCouponRequired
                    })
                    .ToListAsync();

                return Ok(new { count = coupons.Count, data = coupons });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Error retrieving available coupons", error = ex.Message });
            }
        }

        /// <summary>
        /// Get coupon details by code
        /// </summary>
        /// <remarks>
        /// Returns detailed information about a specific coupon including:
        /// - Discount details
        /// - Applicability rules
        /// - Usage limits
        /// - Validity dates
        /// </remarks>
        [HttpGet("{code}")]
        public async Task<IActionResult> GetCouponByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { message = "Coupon code is required" });
            }

            try
            {
                var coupon = await _couponRepository.Query()
                    .Where(x => x.Code == code)
                    .Include(x => x.CartRule)
                    .ThenInclude(cr => cr.Products)
                    .ThenInclude(p => p.Product)
                    .Include(x => x.CartRule)
                    .ThenInclude(cr => cr.Categories)
                    .FirstOrDefaultAsync();

                if (coupon == null)
                {
                    return BadRequest(new { message = $"Coupon code '{code}' not found" });
                }

                var couponDetailVm = new CouponDetailVm
                {
                    Code = coupon.Code,
                    RuleName = coupon.CartRule.Name,
                    Description = coupon.CartRule.Description,
                    IsActive = coupon.CartRule.IsActive,
                    DiscountAmount = coupon.CartRule.DiscountAmount,
                    RuleType = coupon.CartRule.RuleToApply,
                    MaxDiscountAmount = coupon.CartRule.MaxDiscountAmount,
                    StartDate = coupon.CartRule.StartOn,
                    EndDate = coupon.CartRule.EndOn,
                    UsageLimitPerCoupon = coupon.CartRule.UsageLimitPerCoupon,
                    UsageLimitPerCustomer = coupon.CartRule.UsageLimitPerCustomer,
                    ApplicableProductIds = coupon.CartRule.Products.Select(x => x.ProductId).ToList(),
                    ApplicableProductNames = coupon.CartRule.Products.Select(x => x.Product.Name).ToList(),
                    ApplicableCategoryIds = coupon.CartRule.Categories.Select(x => x.CategoryId).ToList(),
                    DiscountDescription = GetDiscountDescription(coupon.CartRule)
                };

                return Ok(couponDetailVm);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Error retrieving coupon details", error = ex.Message });
            }
        }

        /// <summary>
        /// Search coupons with filters (partial code, discount type, etc.)
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchCoupons(
            [FromQuery] string searchTerm = null,
            [FromQuery] string discountType = null,
            [FromQuery] bool? activeOnly = true)
        {
            try
            {
                var now = DateTimeOffset.Now;
                var query = _couponRepository.Query()
                    .Include(x => x.CartRule)
                    .AsQueryable();

                // Filter by active status
                if (activeOnly.HasValue && activeOnly.Value)
                {
                    query = query.Where(x => x.CartRule.IsActive &&
                                           (!x.CartRule.StartOn.HasValue || x.CartRule.StartOn <= now) &&
                                           (!x.CartRule.EndOn.HasValue || x.CartRule.EndOn > now));
                }

                // Filter by search term (code or rule name)
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var lowerSearchTerm = searchTerm.ToLower();
                    query = query.Where(x => x.Code.ToLower().Contains(lowerSearchTerm) ||
                                           x.CartRule.Name.ToLower().Contains(lowerSearchTerm));
                }

                // Filter by discount type
                if (!string.IsNullOrWhiteSpace(discountType))
                {
                    query = query.Where(x => x.CartRule.RuleToApply == discountType);
                }

                var coupons = await query
                    .Select(x => new AvailableCouponVm
                    {
                        Code = x.Code,
                        RuleName = x.CartRule.Name,
                        Description = x.CartRule.Description,
                        DiscountAmount = x.CartRule.DiscountAmount,
                        RuleType = x.CartRule.RuleToApply,
                        MaxDiscountAmount = x.CartRule.MaxDiscountAmount,
                        StartDate = x.CartRule.StartOn,
                        EndDate = x.CartRule.EndOn,
                        IsCouponRequired = x.CartRule.IsCouponRequired
                    })
                    .ToListAsync();

                return Ok(new { count = coupons.Count, data = coupons });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Error searching coupons", error = ex.Message });
            }
        }

        #endregion

        #region Validation Endpoints

        /// <summary>
        /// Validate coupon without applying it
        /// </summary>
        /// <remarks>
        /// Performs comprehensive validation of a coupon code including:
        /// - Existence and active status
        /// - Date validity
        /// - Usage limits
        /// - Cart applicability
        /// Returns validation result with discount details if valid
        /// </remarks>
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequestVm model)
        {
            if (!ModelState.IsValid || model == null || string.IsNullOrWhiteSpace(model.CouponCode))
            {
                return BadRequest(new { message = "Coupon code is required" });
            }

            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                // Note: Cart validation is handled by the CouponService
                // It will check if the coupon applies to items in the customer's cart
                var cartInfo = new CartInfoForCoupon
                {
                    Items = new List<CartItemForCoupon>()
                };

                // Validate coupon
                var validationResult = await _couponService.Validate(currentUser.Id, model.CouponCode, cartInfo);

                if (validationResult.Succeeded)
                {
                    return Ok(new
                    {
                        succeeded = true,
                        couponCode = validationResult.CouponCode,
                        couponRuleName = validationResult.CouponRuleName,
                        discountAmount = validationResult.DiscountAmount,
                        discountedProducts = validationResult.DiscountedProducts.Select(x => new
                        {
                            productId = x.Id,
                            productName = x.Name,
                            originalPrice = x.Price,
                            quantity = x.Quantity,
                            discountAmount = x.DiscountAmount
                        }).ToList(),
                        message = "Coupon is valid and can be applied"
                    });
                }
                else
                {
                    return BadRequest(validationResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Error validating coupon", error = ex.Message });
            }
        }

        /// <summary>
        /// Validate multiple coupon codes and return results for all
        /// </summary>
        /// <remarks>
        /// Useful for checking which coupons from a list are valid for the customer's cart.
        /// Returns validation status for each coupon code provided.
        /// </remarks>
        [HttpPost("validate-batch")]
        public async Task<IActionResult> ValidateMultipleCoupons([FromBody] ValidateMultipleCouponsRequestVm model)
        {
            if (model?.CouponCodes == null || !model.CouponCodes.Any())
            {
                return BadRequest(new { message = "At least one coupon code is required" });
            }

            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                // Note: Cart validation is handled by the CouponService
                var cartInfo = new CartInfoForCoupon
                {
                    Items = new List<CartItemForCoupon>()
                };

                var results = new List<object>();

                foreach (var couponCode in model.CouponCodes.Distinct())
                {
                    var validationResult = await _couponService.Validate(currentUser.Id, couponCode, cartInfo);

                    results.Add(new
                    {
                        couponCode = couponCode,
                        isValid = validationResult.Succeeded,
                        discountAmount = validationResult.DiscountAmount,
                        message = validationResult.ErrorMessage ?? "Valid"
                    });
                }

                var validCount = results.Count(x => (bool)((dynamic)x).isValid);
                return Ok(new { totalCoupons = results.Count, validCoupons = validCount, results });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Error validating coupons", error = ex.Message });
            }
        }

        /// <summary>
        /// Check coupon applicability to specific products
        /// </summary>
        /// <remarks>
        /// Determines if a coupon applies to a specific set of products.
        /// Useful for showing which items in cart will get discount.
        /// </remarks>
        [HttpPost("check-applicability")]
        public async Task<IActionResult> CheckApplicability([FromBody] CheckApplicabilityRequestVm model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.CouponCode) || 
                model.ProductIds == null || !model.ProductIds.Any())
            {
                return BadRequest(new { message = "Coupon code and product IDs are required" });
            }

            try
            {
                var coupon = await _couponRepository.Query()
                    .Where(x => x.Code == model.CouponCode)
                    .Include(x => x.CartRule)
                    .ThenInclude(cr => cr.Products)
                    .Include(x => x.CartRule)
                    .ThenInclude(cr => cr.Categories)
                    .FirstOrDefaultAsync();

                if (coupon == null)
                {
                    return NotFound(new { message = "Coupon not found" });
                }

                var applicableProducts = new List<object>();

                foreach (var productId in model.ProductIds)
                {
                    var isApplicable = coupon.CartRule.Products.Any(x => x.ProductId == productId);

                    applicableProducts.Add(new
                    {
                        productId = productId,
                        isApplicable = isApplicable
                    });
                }

                var applicableCount = applicableProducts.Count(x => (bool)((dynamic)x).isApplicable);

                return Ok(new
                {
                    couponCode = model.CouponCode,
                    totalProducts = model.ProductIds.Count,
                    applicableProducts = applicableCount,
                    details = applicableProducts
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Error checking applicability", error = ex.Message });
            }
        }

        #endregion

        #region Coupon Details Endpoints

        /// <summary>
        /// Get customer's remaining coupon usage
        /// </summary>
        /// <remarks>
        /// Returns how many times the customer has already used a coupon
        /// and how many more times they can use it (if there's a limit).
        /// </remarks>
        [HttpGet("{code}/usage-info")]
        public async Task<IActionResult> GetCouponUsageInfo(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { message = "Coupon code is required" });
            }

            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var coupon = await _couponRepository.Query()
                    .Where(x => x.Code == code)
                    .Include(x => x.CartRule)
                    .FirstOrDefaultAsync();

                if (coupon == null)
                {
                    return NotFound(new { message = "Coupon not found" });
                }

                // Get usage counts from CartRuleUsage through the CartRule
                var cartRule = coupon.CartRule;
                if (cartRule == null)
                {
                    return NotFound(new { message = "Cart rule not found" });
                }

                var totalUsageCount = await _couponRepository.Query()
                    .Where(x => x.Id == coupon.Id)
                    .SelectMany(x => x.CartRule.Coupons
                        .SelectMany(c => c.CartRuleId == x.CartRuleId ? new List<CartRuleUsage>() : new List<CartRuleUsage>()))
                    .CountAsync();

                var customerUsageCount = await _couponRepository.Query()
                    .Where(x => x.Id == coupon.Id)
                    .SelectMany(x => x.CartRule.Coupons
                        .SelectMany(c => c.CartRuleId == x.CartRuleId ? new List<CartRuleUsage>() : new List<CartRuleUsage>()))
                    .Where(x => x.UserId == currentUser.Id)
                    .CountAsync();

                var usageLimitPerCustomer = cartRule.UsageLimitPerCustomer;
                int? remainingForCustomer = usageLimitPerCustomer.HasValue 
                    ? Math.Max(0, usageLimitPerCustomer.Value - customerUsageCount)
                    : (int?)null;

                return Ok(new
                {
                    couponCode = code,
                    totalUsageCount = totalUsageCount,
                    customerUsageCount = customerUsageCount,
                    usageLimitPerCustomer = usageLimitPerCustomer,
                    remainingForCustomer = remainingForCustomer,
                    canUse = remainingForCustomer == null || remainingForCustomer > 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Error retrieving usage info", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all coupons valid for current customer's cart
        /// </summary>
        /// <remarks>
        /// Returns list of all coupons that can be applied to customer's current cart.
        /// Performs basic validation (not expired, active, etc.)
        /// </remarks>
        [HttpGet("applicable")]
        public async Task<IActionResult> GetApplicableCoupons()
        {
            try
            {
                var currentUser = await _workContext.GetCurrentUser();
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var now = DateTimeOffset.Now;
                var applicableCoupons = new List<dynamic>();

                var allCoupons = await _couponRepository.Query()
                    .Where(x => x.CartRule.IsActive &&
                               (!x.CartRule.StartOn.HasValue || x.CartRule.StartOn <= now) &&
                               (!x.CartRule.EndOn.HasValue || x.CartRule.EndOn > now))
                    .Include(x => x.CartRule)
                    .ThenInclude(cr => cr.Products)
                    .ToListAsync();

                var cartInfo = new CartInfoForCoupon
                {
                    Items = new List<CartItemForCoupon>()
                };

                foreach (var coupon in allCoupons)
                {
                    var validationResult = await _couponService.Validate(currentUser.Id, coupon.Code, cartInfo);
                    
                    if (validationResult.Succeeded)
                    {
                        applicableCoupons.Add(new
                        {
                            code = coupon.Code,
                            ruleName = coupon.CartRule.Name,
                            description = coupon.CartRule.Description,
                            discountAmount = validationResult.DiscountAmount,
                            ruleType = coupon.CartRule.RuleToApply
                        });
                    }
                }

                return Ok(new { count = applicableCoupons.Count, coupons = applicableCoupons });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Error retrieving applicable coupons", error = ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        private string GetDiscountDescription(CartRule cartRule)
        {
            if (cartRule == null)
                return string.Empty;

            return cartRule.RuleToApply switch
            {
                "cart_fixed" => $"Fixed discount of {cartRule.DiscountAmount:C}",
                "by_percent" => $"{cartRule.DiscountAmount}% off" +
                               (cartRule.MaxDiscountAmount.HasValue ? $" (max {cartRule.MaxDiscountAmount:C})" : ""),
                _ => "Special discount"
            };
        }

        #endregion
    }
}
