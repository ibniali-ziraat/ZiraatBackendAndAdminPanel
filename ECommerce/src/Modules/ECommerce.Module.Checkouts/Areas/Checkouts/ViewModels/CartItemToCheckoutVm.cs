namespace ECommerce.Module.Checkouts.Areas.Checkouts.ViewModels
{
    public class CartItemToCheckoutVm
    {
        public long ProductId { get; set; }

        public long? VendorId { get; set; }

        public long? VendorProductId { get; set; }

        public long? VendorProductVariationId { get; set; }

        public decimal ProductPrice { get; set; }

        public int Quantity { get; set; }
    }
}
