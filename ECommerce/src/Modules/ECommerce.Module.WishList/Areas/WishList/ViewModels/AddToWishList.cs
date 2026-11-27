namespace ECommerce.Module.WishList.Areas.WishList.ViewModels
{
    public class AddToWishList
    {
        public long ProductId { get; set; }

        public long? VendorId { get; set; }

        public long? VendorProductId { get; set; }

        public int Quantity { get; set; }
    }
}
