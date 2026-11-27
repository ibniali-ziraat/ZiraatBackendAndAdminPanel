using System;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class GetAllCustomersVm
    {
        public long UserId { get; set; }
        public long CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhoneNumber { get; set; }

        public string CustomerEmail { get; set; }

        public decimal Credit { get; set; }

        public decimal Balance { get; set; }

        public decimal TotalOrderAmount { get; set; }

        public DateTime StartDate { get; set; }

        public bool ActiveInactive { get; set; }
    }
}
