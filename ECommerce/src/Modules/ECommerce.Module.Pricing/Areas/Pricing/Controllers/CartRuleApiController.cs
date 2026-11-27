using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Web.SmartTable;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Pricing.Areas.Pricing.ViewModels;
using ECommerce.Module.Pricing.Models;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Module.Pricing.Areas.Pricing.Controllers
{
    [Area("Pricing")]
    [Authorize(Roles = "admin, vendor")]
    [Route("api/cart-rules")]
    public class CartRuleApiController : ControllerBase
    {
        private readonly IRepository<CartRule> _cartRuleRepository;
        private readonly IWorkContext _workContext;

        public CartRuleApiController(IRepository<CartRule> cartRuleRepository, IWorkContext workContext)
        {
            _cartRuleRepository = cartRuleRepository;
            _workContext = workContext;
        }

        [HttpPost("grid")]
        public async Task<IActionResult> List([FromBody] SmartTableParam param)
        {
            IQueryable<CartRule> query = _cartRuleRepository
                .Query();
            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin"))
            {
                var vendorId = GetRequiredVendorId(currentUser);
                if (!vendorId.HasValue)
                {
                    return BadRequest(new { error = "The logged in vendor account is not linked to a vendor." });
                }

                query = query.Where(x => x.VendorId == vendorId.Value);
            }

            var cartRules = query.ToSmartTableResult(
                param,
                cartRule => new
                {
                    cartRule.Id,
                    cartRule.Name,
                    cartRule.StartOn,
                    cartRule.EndOn,
                    cartRule.IsActive,
                    cartRule.VendorId
                });

            return new JsonResult(cartRules);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var catrtRule = await _cartRuleRepository.Query()
                .Include(x => x.Coupons)
                .Include(x => x.Products).ThenInclude(p => p.Product)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (catrtRule == null)
            {
                return NotFound();
            }

            var currentUser = await _workContext.GetCurrentUser();
            if (!CanManageCartRule(catrtRule, currentUser))
            {
                return BadRequest(new { error = "You don't have permission to manage this cart rule" });
            }

            var model = new CartRuleForm
            {
                Id = catrtRule.Id,
                Name = catrtRule.Name,
                Description = catrtRule.Description,
                IsActive = catrtRule.IsActive,
                StartOn = catrtRule.StartOn,
                EndOn = catrtRule.EndOn,
                IsCouponRequired = catrtRule.IsCouponRequired,
                RuleToApply = catrtRule.RuleToApply,
                DiscountAmount = catrtRule.DiscountAmount,
                DiscountStep = catrtRule.DiscountStep,
                MaxDiscountAmount = catrtRule.MaxDiscountAmount,
                UsageLimitPerCoupon = catrtRule.UsageLimitPerCoupon,
                UsageLimitPerCustomer = catrtRule.UsageLimitPerCustomer,
                VendorId = catrtRule.VendorId,
                Products = catrtRule.Products.Select(x => new CartRuleProductVm { Id = x.ProductId, Name = x.Product.Name, IsPublished = x.Product.IsPublished }).ToList()
            };

            if (catrtRule.IsCouponRequired)
            {
                var coupon = catrtRule.Coupons.FirstOrDefault();
                if (coupon != null)
                {
                    model.CouponCode = coupon.Code;
                }
            }

            return new JsonResult(model);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CartRuleForm model)
        {
            if (model == null || model.Products == null)
            {
                return BadRequest(new { error = "Send products as an array of selected catalog product objects, for example [{ \"id\": 123 }]. Use [] only for an unrestricted product selection." });
            }

            if (model.Products.Any(x => x == null || x.Id <= 0))
            {
                return BadRequest(new { error = "Each selected product must contain a valid catalog product id." });
            }

            if (ModelState.IsValid)
            {
                var currentUser = await _workContext.GetCurrentUser();
                var vendorId = ResolveVendorIdForSave(currentUser, model.VendorId);
                if (!User.IsInRole("admin") && !vendorId.HasValue)
                {
                    return BadRequest(new { error = "The logged in vendor account is not linked to a vendor." });
                }

                var cartRule = new CartRule
                {
                    Name = model.Name,
                    Description = model.Description,
                    IsActive = model.IsActive,
                    StartOn = model.StartOn,
                    EndOn = model.EndOn,
                    IsCouponRequired = model.IsCouponRequired,
                    RuleToApply = model.RuleToApply,
                    DiscountAmount = model.DiscountAmount,
                    DiscountStep = model.DiscountStep,
                    MaxDiscountAmount = model.MaxDiscountAmount,
                    UsageLimitPerCoupon = model.UsageLimitPerCoupon,
                    UsageLimitPerCustomer = model.UsageLimitPerCustomer,
                    VendorId = vendorId
                };

                if (model.IsCouponRequired && !string.IsNullOrWhiteSpace(model.CouponCode))
                {
                    var coupon = new Coupon
                    {
                        CartRule = cartRule,
                        Code = model.CouponCode
                    };

                    cartRule.Coupons.Add(coupon);
                }

                foreach (var productId in model.Products.Select(x => x.Id).Distinct())
                {
                    var cartRuleProduct = new CartRuleProduct
                    {
                        CartRule = cartRule,
                        ProductId = productId
                    };
                    cartRule.Products.Add(cartRuleProduct);
                }

                _cartRuleRepository.Add(cartRule);
                await _cartRuleRepository.SaveChangesAsync();

                return Ok(new { id = cartRule.Id, message = "success", StatusCodes.Status200OK });
            }
            return BadRequest(ModelState);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] CartRuleForm model)
        {
            if (model == null)
            {
                return BadRequest(new { error = "Cart rule details are required." });
            }

            if (model.Products?.Any(x => x == null || x.Id <= 0) == true)
            {
                return BadRequest(new { error = "Each selected product must contain a valid catalog product id." });
            }

            if (ModelState.IsValid)
            {
                var cartRule = await _cartRuleRepository.Query()
                    .Include(x => x.Coupons)
                    .Include(x => x.Products)
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (cartRule == null)
                {
                    return NotFound();
                }

                var currentUser = await _workContext.GetCurrentUser();
                if (!CanManageCartRule(cartRule, currentUser))
                {
                    return BadRequest(new { error = "You don't have permission to manage this cart rule" });
                }

                cartRule.Name = model.Name;
                cartRule.Description = model.Description;
                cartRule.StartOn = model.StartOn;
                cartRule.EndOn = model.EndOn;
                cartRule.IsActive = model.IsActive;
                cartRule.IsCouponRequired = model.IsCouponRequired;
                cartRule.RuleToApply = model.RuleToApply;
                cartRule.DiscountAmount = model.DiscountAmount;
                cartRule.DiscountStep = model.DiscountStep;
                cartRule.MaxDiscountAmount = model.MaxDiscountAmount;
                cartRule.UsageLimitPerCoupon = model.UsageLimitPerCoupon;
                cartRule.UsageLimitPerCustomer = model.UsageLimitPerCustomer;
                cartRule.VendorId = ResolveVendorIdForSave(currentUser, model.VendorId);

                if (model.IsCouponRequired && !string.IsNullOrWhiteSpace(model.CouponCode))
                {
                    var coupon = cartRule.Coupons.FirstOrDefault();
                    if (coupon == null)
                    {
                        coupon = new Coupon
                        {
                            CartRule = cartRule,
                            Code = model.CouponCode
                        };

                        cartRule.Coupons.Add(coupon);
                    }
                    else
                    {
                        coupon.Code = model.CouponCode;
                    }
                }

                // Metadata-only updates must not remove the saved product selection.
                if (model.Products != null)
                {
                    var productIds = model.Products.Select(x => x.Id).Distinct().ToHashSet();
                    foreach (var productId in productIds)
                    {
                        if (!cartRule.Products.Any(x => x.ProductId == productId))
                        {
                            cartRule.Products.Add(new CartRuleProduct
                            {
                                CartRule = cartRule,
                                ProductId = productId
                            });
                        }
                    }

                    var deletedProducts = cartRule.Products.Where(x => !productIds.Contains(x.ProductId)).ToList();
                    foreach (var item in deletedProducts)
                    {
                        item.CartRule = null;
                        cartRule.Products.Remove(item);
                    }
                }

                await _cartRuleRepository.SaveChangesAsync();
                return Ok(new { message = "success", StatusCodes.Status200OK });
            }
            return BadRequest(ModelState);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var cartRule = await _cartRuleRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (cartRule == null)
            {
                return NotFound();
            }

            var currentUser = await _workContext.GetCurrentUser();
            if (!CanManageCartRule(cartRule, currentUser))
            {
                return BadRequest(new { error = "You don't have permission to manage this cart rule" });
            }

            try
            {
                _cartRuleRepository.Remove(cartRule);
                await _cartRuleRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { Error = $"The cart rule {cartRule.Name} can't not be deleted because it has been used" });
            }

            return Ok(new { message = "success", StatusCodes.Status200OK });
        }

        private bool CanManageCartRule(CartRule cartRule, User currentUser)
        {
            return User.IsInRole("admin") || (currentUser?.VendorId.HasValue == true && cartRule.VendorId == currentUser.VendorId);
        }

        private long? ResolveVendorIdForSave(User currentUser, long? requestedVendorId)
        {
            return User.IsInRole("admin") ? requestedVendorId : GetRequiredVendorId(currentUser);
        }

        private static long? GetRequiredVendorId(User currentUser)
        {
            return currentUser?.VendorId;
        }
    }
}
