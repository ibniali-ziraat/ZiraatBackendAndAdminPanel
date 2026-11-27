using System;
using System.Collections.Generic;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class VendorCustomerAssociation : EntityBase
    {
        public long VendorId { get; set; }

        public long CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string CustomerEmail { get; set; }

        public string CustomerPhoneNumber { get; set; }

        public bool AllowCredit { get; set; }

        public decimal MaxCreditAmount { get; set; }

        public decimal CurrentBalance { get; set; }

        public DateTime StartDate { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public long CreatedById { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }

        public long LatestUpdatedById { get; set; }

        public bool IsDeleted { get; set; }

        public virtual Vendor Vendor { get; set; }

        public virtual User Customer { get; set; }

        public virtual ICollection<CustomerSettlement> CustomerSettlements { get; set; } = new List<CustomerSettlement>();
    }
}
