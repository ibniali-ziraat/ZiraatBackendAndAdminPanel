namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class SettlementPaymentMethodVm
    {
        public decimal? Cash { get; set; }

        public decimal? Online { get; set; }

        public decimal? Cheque { get; set; }

        public string TransactionId { get; set; }
    }
}
