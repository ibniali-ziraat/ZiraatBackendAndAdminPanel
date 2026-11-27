using System.Collections.Generic;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class GetSettlementSummaryVm
    {
        public int TotalOrders { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal RemainingAmount { get; set; }

        public List<SettlementHistoryItemVm> SettlementHistory { get; set; }
    }
}
