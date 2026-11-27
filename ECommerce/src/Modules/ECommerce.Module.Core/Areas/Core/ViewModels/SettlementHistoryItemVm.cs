using System;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class SettlementHistoryItemVm
    {
        public long SettlementId { get; set; }

        public long OrderId { get; set; }

        public DateTime OrderedDate { get; set; }

        public decimal AmountPaid { get; set; }

        public string PaymentMethod { get; set; }

        public decimal BalanceAfter { get; set; }

        public string TransactionId { get; set; }

        public string Note { get; set; }
    }
}
