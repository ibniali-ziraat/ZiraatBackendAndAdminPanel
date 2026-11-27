using System;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class AddCustomerVm
    {
        public long CustomerId { get; set; }
        public string Name { get; set; }

        public string Email { get; set; }

        public string Mobile { get; set; }

        public bool AllowCredit { get; set; }

        public decimal MaxCreditAmount { get; set; }

        public DateTime StartDate { get; set; }
    }
    public class UpdateCustomerVm
    {
        public long CustomerId { get; set; }

        public bool AllowCredit { get; set; }

        public decimal MaxCreditAmount { get; set; }

    }
    public class UpdateCustomerBalance
    {
        public long CustomerId { get; set; }
        public decimal CurrentBalance { get; set; }

    }
}
