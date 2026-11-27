namespace ECommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels
{
    public class AddToCartModel
    {
        public long ProductId { get; set; }

        public long? VendorId { get; set; }

        public long? VendorProductId { get; set; }

        public long? VendorProductVariationId { get; set; }

        public string VariationName { get; set; }

        public int Quantity { get; set; }
    }
}
