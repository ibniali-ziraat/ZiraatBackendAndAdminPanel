using System;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models;

public class BankDetails : EntityBase
{
    // Id inherited from EntityBase

    public long FinancialPaymentId { get; set; }

    public string AccountHolderName { get; set; }

    public string AccountNumber { get; set; }

    public string IFSCCode { get; set; }

    public string BankName { get; set; }

    public string BranchName { get; set; }
    public Boolean  IsActive { get; set; }
    public FinancialPayment FinancialPayment { get; set; }
}
