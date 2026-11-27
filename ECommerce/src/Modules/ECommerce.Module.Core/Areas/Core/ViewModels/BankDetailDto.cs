using System;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class BankDetailDto
    {
        public long Id { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }
        public string BankName { get; set; }
        public string BranchName { get; set; }
    }

    public class FinancialPaymentResponseVm
    {
        public long Id { get; set; }
        public long SupplierProfileId { get; set; }
        public bool UpiAvailable { get; set; }
        public bool CreditFacility { get; set; }
        public bool SellingViaZiraat { get; set; }
        public BankDetailDto BankDetails { get; set; }
    }
}