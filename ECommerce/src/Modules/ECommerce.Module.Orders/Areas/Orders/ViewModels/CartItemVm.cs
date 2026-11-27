namespace ECommerce.Module.Orders.Areas.Orders.ViewModels
{
    public class CartItemVm
    {
        public int Quantity;

        public decimal Price;

        public long? TaxClassId;

        public decimal? TaxPercent;

        public decimal? TaxAmount;
    }
}
