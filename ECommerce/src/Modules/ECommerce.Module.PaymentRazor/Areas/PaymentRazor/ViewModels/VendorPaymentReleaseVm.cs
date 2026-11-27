using System;

namespace ECommerce.Module.PaymentRazor.Areas.PaymentRazor.ViewModels
{
    public class VendorPaymentReleaseVm
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public long VendorId { get; set; }

        public long? FinancialPaymentId { get; set; }

        public long? BankDetailId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; }

        public string PaymentMethod { get; set; }

        public string Status { get; set; }

        public string RazorpayContactId { get; set; }

        public string RazorpayFundAccountId { get; set; }

        public string RazorpayPayoutId { get; set; }

        public string RazorpayPayoutStatus { get; set; }

        public string ReferenceId { get; set; }

        public string FailureMessage { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }

        public DateTimeOffset? ReleasedOn { get; set; }
    }
}
