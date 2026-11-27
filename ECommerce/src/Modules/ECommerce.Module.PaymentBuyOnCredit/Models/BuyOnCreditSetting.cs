namespace ECommerce.Module.PaymentBuyOnCredit.Models
{
    public class BuyOnCreditSetting
    {
        public decimal? MinOrderValue { get; set; }

        public decimal? MaxOrderValue { get; set; }

        public decimal PaymentFee { get; set; }
    }
}
