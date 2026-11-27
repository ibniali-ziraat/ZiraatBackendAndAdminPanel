using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class OperationalDetailsViewModel
    {
        public long? OperationalId { get; set; }

        public long SupplierProfileId { get; set; }

        public int? YearsInBusiness { get; set; }

        public IList<string> CompanyTieups { get; set; } = new List<string>();

        public bool DeliveryServiceAvailable { get; set; }

        public IList<string> ServiceArea { get; set; } = new List<string>();

        public string AvgMonthlySalesVolume { get; set; }
    }

    public class SaveOperationalDetailsRequest
    {
        // keep the JSON property name exactly as incoming payload
        public OperationalDetailsViewModel OperationalDetails { get; set; }
    }
}
