using ECommerce.Module.Core.ViewModels;

namespace ECommerce.Module.WishList.Areas.WishList.ViewModels
{
    public class WishListItemVm
    {
        public long Id { get; set; }

        public long WishListId { get; set; }

        public long ProductId { get; set; }

        public long? VendorId { get; set; }

        public long? VendorProductId { get; set; }

        public VendorProductAssignmentPricingVm VendorAssignment { get; set; }

        public string ProductName { get; set; }

        public string ProductImage { get; set; }

        public decimal ProductPrice { get; set; }

        public int ProductStockQuantity { get; set; }

        public string Description { get; set; }

        public int Quantity { get; set; }
    }
}
