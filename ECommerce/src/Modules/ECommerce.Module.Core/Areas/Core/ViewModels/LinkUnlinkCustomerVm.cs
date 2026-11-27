namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class LinkUnlinkCustomerVm
    {
        public long CustomerId { get; set; }

        public long VendorId { get; set; }

        public bool LinkUnlink { get; set; }
    }
}
