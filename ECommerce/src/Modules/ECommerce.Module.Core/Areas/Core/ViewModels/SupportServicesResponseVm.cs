using System;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class SupportServicesResponseVm
    {
        public long Id { get; set; }
        public long SupplierProfileId { get; set; }
        public bool AgronomyAdvisorySupport { get; set; }
        public bool FieldStaffAvailable { get; set; }
        public int? NumberOfStaff { get; set; }
        public string AfterSalesService { get; set; }
        public int? PreferredDeliveryAreaRangeSqKm { get; set; }
        public bool ComputerFacilityAvailable { get; set; }
        public bool DeliveryVehicleAvailable { get; set; }
    }
}