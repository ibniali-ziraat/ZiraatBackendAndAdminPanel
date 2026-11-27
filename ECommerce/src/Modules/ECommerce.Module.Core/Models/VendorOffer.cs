using System;
using System.ComponentModel.DataAnnotations;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class VendorOffer : EntityBase
    {
        public long VendorId { get; set; }

        [Required]
        [StringLength(100)]
        public string OfferType { get; set; }

        [Required]
        [StringLength(450)]
        public string Title { get; set; }

        public string Description { get; set; }

        public string TermsAndConditions { get; set; }

        [StringLength(50)]
        public string DiscountType { get; set; }

        public decimal DiscountValue { get; set; }

        public decimal? MaxDiscountAmount { get; set; }

        public decimal? MinimumOrderAmount { get; set; }

        public int? MinimumQuantity { get; set; }

        [StringLength(100)]
        public string CouponCode { get; set; }

        public DateTimeOffset? StartOn { get; set; }

        public DateTimeOffset? EndOn { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

        public long CreatedById { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; } = DateTimeOffset.UtcNow;

        public long LatestUpdatedById { get; set; }

        public virtual Vendor Vendor { get; set; }

        public virtual User CreatedBy { get; set; }

        public virtual User LatestUpdatedBy { get; set; }
    }
}
