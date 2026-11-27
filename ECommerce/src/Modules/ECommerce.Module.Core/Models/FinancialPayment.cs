using System.Collections.Generic;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models;

public class FinancialPayment : EntityBase
{
    public long Id { get; set; }

    public long SupplierProfileId { get; set; }

    public bool UpiAvailable { get; set; }

    public bool CreditFacility { get; set; }

    public bool SellingViaZiraat { get; set; }

    // One-to-many relation to bank details
    public ICollection<BankDetails> BankDetails { get; set; } = new List<BankDetails>();
}
