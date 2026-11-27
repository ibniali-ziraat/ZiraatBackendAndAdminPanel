using System;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class CustomerOrderVm
    {
        public long OrderId { get; set; }

        public string ItemName { get; set; }

        public int ItemQuantity { get; set; }

        public DateTime OrderedDate { get; set; }

        public decimal OrderAmount { get; set; }

        public string OrderStatus { get; set; }

        public string PaymentMethod { get; set; }
    }
}
