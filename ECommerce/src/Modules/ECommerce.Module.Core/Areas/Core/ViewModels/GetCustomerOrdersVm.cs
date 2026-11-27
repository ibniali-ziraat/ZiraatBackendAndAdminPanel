using System.Collections.Generic;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class GetCustomerOrdersVm
    {
        public long VendorId { get; set; }

        public long CustomerId { get; set; }

        public List<CustomerOrderVm> Orders { get; set; } = new List<CustomerOrderVm>();

        /// <summary>
        /// Total number of BuyOnCredit orders for this customer
        /// </summary>
        public int TotalCreditOrders { get; set; }

        /// <summary>
        /// Total amount of all BuyOnCredit orders
        /// </summary>
        public decimal TotalCreditOrderAmount { get; set; }

        /// <summary>
        /// Total settled/paid amount for BuyOnCredit orders
        /// </summary>
        public decimal TotalSettledAmount { get; set; }

        /// <summary>
        /// Remaining balance to be paid (TotalCreditOrderAmount - TotalSettledAmount)
        /// </summary>
        public decimal RemainingBalance { get; set; }
    }
}
