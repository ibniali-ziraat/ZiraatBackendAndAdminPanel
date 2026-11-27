using System;
using System.Text.Json.Serialization;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class BankDetailVm
    {
        [JsonPropertyName("accountHolder")]
        public string AccountHolder { get; set; }

        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; }

        [JsonPropertyName("ifsc")]
        public string Ifsc { get; set; }

        [JsonPropertyName("bankName")]
        public string BankName { get; set; }

        [JsonPropertyName("branchName")]
        public string BranchName { get; set; }

        [JsonPropertyName("IsActive")]
        public Boolean IsActive { get; set; }
    }

    public class FinancialPaymentViewModel
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("supplierProfileId")]
        public long SupplierProfileId { get; set; }

        [JsonPropertyName("upiAvailable")]
        public bool UpiAvailable { get; set; }

        [JsonPropertyName("creditFacility")]
        public bool CreditFacility { get; set; }

        [JsonPropertyName("sellingViaZiraat")]
        public bool SellingViaZiraat { get; set; }

        [JsonPropertyName("bankDetails")]
        public BankDetailVm BankDetails { get; set; }
    }
}
