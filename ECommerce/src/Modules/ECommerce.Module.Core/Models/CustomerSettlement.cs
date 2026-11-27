using System;
using System.Collections.Generic;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class CustomerSettlement : EntityBase
    {
        public long VendorId { get; set; }

        public long VendorCustomerAssociationId { get; set; }

        public long CustomerId { get; set; }

        public decimal AmountPaid { get; set; }

        public string PaymentMethod { get; set; }

        public string TransactionId { get; set; }

        public decimal BalanceAfter { get; set; }

        public DateTime SettlementDate { get; set; }

        public DateTime? SettlementReminderDate { get; set; }

        public string Note { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public long CreatedById { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }

        public long LatestUpdatedById { get; set; }

        public bool IsDeleted { get; set; }

        public virtual Vendor Vendor { get; set; }

        public virtual VendorCustomerAssociation VendorCustomerAssociation { get; set; }

        public virtual User CreatedBy { get; set; }

        public virtual User LatestUpdatedBy { get; set; }
    }
}
