using System;
using System.ComponentModel.DataAnnotations;
using ECommerce.Infrastructure.Models;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Orders.Models;

namespace ECommerce.Module.PaymentRazor.Models
{
    public class VendorPaymentRelease : EntityBase
    {
        public long OrderId { get; set; }

        public Order Order { get; set; }

        public long VendorId { get; set; }

        public Vendor Vendor { get; set; }

        public long? FinancialPaymentId { get; set; }

        public FinancialPayment FinancialPayment { get; set; }

        public long? BankDetailId { get; set; }

        public BankDetails BankDetail { get; set; }

        public decimal Amount { get; set; }

        [StringLength(10)]
        public string Currency { get; set; }

        [StringLength(100)]
        public string PaymentMethod { get; set; }

        public VendorPaymentReleaseStatus Status { get; set; }

        [StringLength(450)]
        public string RazorpayContactId { get; set; }

        [StringLength(450)]
        public string RazorpayFundAccountId { get; set; }

        [StringLength(450)]
        public string RazorpayPayoutId { get; set; }

        [StringLength(450)]
        public string RazorpayPayoutStatus { get; set; }

        [StringLength(450)]
        public string ReferenceId { get; set; }

        public string FailureMessage { get; set; }

        public string RequestPayload { get; set; }

        public string ResponsePayload { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }

        public DateTimeOffset? ReleasedOn { get; set; }
    }
}
