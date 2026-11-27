namespace ECommerce.Module.Pricing.Services
{
    public class CartItemForCoupon
    {
        public long ProductId { get; set; }

        public long? VendorId { get; set; }

        public long? VendorProductId { get; set; }

        public long? VendorProductVariationId { get; set; }

        public decimal? Price { get; set; }

        public int Quantity { get; set; }
    }
}
