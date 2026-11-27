using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Module.Pricing.Areas.Pricing.ViewModels
{
    /// <summary>Available coupon summary for browsing</summary>
    public class AvailableCouponVm
    {
        public string Code { get; set; }
        public long? VendorId { get; set; }
        public string RuleName { get; set; }
        public string Description { get; set; }
        public decimal DiscountAmount { get; set; }
        public string RuleType { get; set; } // "cart_fixed" or "by_percent"
        public decimal? MaxDiscountAmount { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string AppliesTo { get; set; }
        public bool IsCouponRequired { get; set; }
    }

    /// <summary>Detailed coupon information</summary>
    public class CouponDetailVm
    {
        public string Code { get; set; }
        public string RuleName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public decimal DiscountAmount { get; set; }
        public string RuleType { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public int? UsageLimitPerCoupon { get; set; }
        public int? UsageLimitPerCustomer { get; set; }
        public List<long> ApplicableProductIds { get; set; } = new();
        public List<string> ApplicableProductNames { get; set; } = new();
        public List<long> ApplicableCategoryIds { get; set; } = new();
        public string DiscountDescription { get; set; }
    }

    /// <summary>Request model for validating a single coupon</summary>
    public class ValidateCouponRequestVm
    {
        [Required(ErrorMessage = "Coupon code is required")]
        [StringLength(450)]
        public string CouponCode { get; set; }
    }

    /// <summary>Request model for validating multiple coupons</summary>
    public class ValidateMultipleCouponsRequestVm
    {
        [Required(ErrorMessage = "At least one coupon code is required")]
        public List<string> CouponCodes { get; set; } = new();
    }

    /// <summary>Request model for checking coupon applicability to products</summary>
    public class CheckApplicabilityRequestVm
    {
        [Required(ErrorMessage = "Coupon code is required")]
        public string CouponCode { get; set; }

        [Required(ErrorMessage = "At least one product ID is required")]
        public List<long> ProductIds { get; set; } = new();
    }
}
