using System;
using System.Collections.Generic;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class AddSettlementVm
    {
        public long VendorId { get; set; }

        public long CustomerId { get; set; }

        public decimal AmountPaid { get; set; }

        public List<SettlementPaymentMethodVm> PaymentMethod { get; set; }

        public DateTime Date { get; set; }

        public DateTime? SettlementReminderDate { get; set; }

        public string Note { get; set; }
    }
}
