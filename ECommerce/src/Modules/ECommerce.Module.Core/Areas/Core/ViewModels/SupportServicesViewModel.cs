using System.Text.Json.Serialization;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class SupportServicesViewModel
    {
        public long? Id { get; set; }

        public long SupplierProfileId { get; set; }

        public bool AdvisorySupport { get; set; }

        public bool FieldStaffAvailable { get; set; }

        public int? FieldStaffCount { get; set; }

        public string AfterSalesService { get; set; }

        public string DeliveryRange { get; set; }

        public bool ComputerAvailable { get; set; }

        public bool VehicleAvailable { get; set; }
    }
 
}
