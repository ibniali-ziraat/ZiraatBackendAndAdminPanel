using System;
using System.Collections.Generic;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class LicensingComplianceViewModel
    {
        public long LicensingId { get; set; }
        public long SupplierProfileId { get; set; }
        public string GSTNumber { get; set; }
        public string PANNumber { get; set; }

        public ICollection<LicenseViewModel> Licenses { get; set; } = new List<LicenseViewModel>();
    }

    public class LicenseViewModel
    {
        public long LicenseId { get; set; }
        public string LicenseType { get; set; }
        public string LicenseNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public LicenseDocumentViewModel LicenseDocument { get; set; }
        // Omitted/null preserves documents; an explicit array replaces the selection.
        public ICollection<AdditionalDocumentViewModel> AdditionalDocuments { get; set; }
    }

    public class LicenseDocumentViewModel
    {
        public string FileName { get; set; }
        public string FileUrl { get; set; }
    }

    public class AdditionalDocumentViewModel
    {
        public long Id { get; set; }

        public string DocumentType { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
    }

    public class ProductInputTypeViewModel
    {
        public long InputTypeId { get; set; }
        public long ProductPortfolioId { get; set; }
        public string InputType { get; set; }
    }

    public class ProductMajorBrandViewModel
    {
        public long BrandId { get; set; }
        public long ProductPortfolioId { get; set; }
        public string BrandName { get; set; }
    }

    public class ProductCustomInputViewModel
    {
        public long CustomInputId { get; set; }
        public long ProductPortfolioId { get; set; }
        public string ProductName { get; set; }
        public string StockQuantity { get; set; }
        public string Unit { get; set; }
    }

  
    public class ServiceAreaViewModel
    {
        public long ServiceAreaId { get; set; }
        public long OperationalId { get; set; }
        public string PlaceName { get; set; }
    }

   
    public class BankDetailsViewModel
    {
        public long BankId { get; set; }
        public long FinancialId { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }
        public string BankName { get; set; }
    }

 
    public class VerificationRatingsViewModel
    {
        public long VerificationId { get; set; }
        public long SupplierProfileId { get; set; }
        public bool VerifiedBadge { get; set; }
        public decimal? DealerRating { get; set; }
    }

    public class CustomerReviewViewModel
    {
        public long ReviewId { get; set; }
        public long VerificationId { get; set; }
        public string FarmerName { get; set; }
        public decimal? Rating { get; set; }
        public string Comment { get; set; }
        public DateTime? ReviewDate { get; set; }
    }
}
