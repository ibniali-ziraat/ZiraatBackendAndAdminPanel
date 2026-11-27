namespace ECommerce.Module.PaymentRazor.Models
{
    public enum VendorPaymentReleaseStatus
    {
        Pending = 1,
        Processing = 10,
        Released = 20,
        Failed = 30,
        Skipped = 40
    }
}
