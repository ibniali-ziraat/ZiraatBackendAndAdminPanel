using System;
using System.Collections.Generic;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    // LICENSING & COMPLIANCE
    public class LicensingCompliance : EntityBase
    {
        
        public long SupplierProfileId { get; set; }
        public string GSTNumber { get; set; }
        public string PANNumber { get; set; }

        public ICollection<License> Licenses { get; set; } = new List<License>();
    }

    public class License : EntityBase
    {
        
        public long LicensingComplianceId { get; set; }
        public string LicenseType { get; set; }
        public string LicenseNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public LicenseDocument LicenseDocument { get; set; }
        public ICollection<AdditionalDocument> AdditionalDocuments { get; set; } = new List<AdditionalDocument>();
    }

    public class LicenseDocument : EntityBase
    {
        
        public long LicenseId { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
    }

    public class AdditionalDocument : EntityBase
    {
        
        public long LicenseId { get; set; }
        public string DocumentType { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
    }

    // PRODUCT PORTFOLIO
   


  
   
  
  
    

 
    // SUPPORT SERVICES
    // Changed to inherit EntityBase so EF automatically picks it up in the model registration
    public class SupportServices : EntityBase
    {
        // Id is inherited from EntityBase (long)
        public long Id { get; set; }

        public long SupplierProfileId { get; set; }

        // whether agronomy/advisory support is provided
        public bool AdvisorySupport { get; set; }

        // whether field staff are available
        public bool FieldStaffAvailable { get; set; }

        // number of staff available
        public int? FieldStaffCount { get; set; }

        // free-text/structured description of after-sales service (maps from incoming "afterSalesService")
        public string AfterSalesService { get; set; }

        // preferred delivery range in sq km
        public int? DeliveryRange { get; set; }

        // whether computer facility is available
        public bool ComputerAvailable { get; set; }

        // whether delivery vehicle is available
        public bool VehicleAvailable { get; set; }
    }

    // VERIFICATION & RATINGS
    public class VerificationRatings : IEntityWithTypedId<long>
    {
        public long  Id { get; set; }
        public long SupplierProfileId { get; set; }
        public bool VerifiedBadge { get; set; }
        public decimal? DealerRating { get; set; }

        public IList<CustomerReview> CustomerReviews { get; set; } = new List<CustomerReview>();
    }

    public class CustomerReview : IEntityWithTypedId<long>
    {
        public long  Id { get; set; }
        public long VerificationId { get; set; }
        public string FarmerName { get; set; }
        public decimal? Rating { get; set; }
        public string Comment { get; set; }
        public DateTime? ReviewDate { get; set; }
    }
}
