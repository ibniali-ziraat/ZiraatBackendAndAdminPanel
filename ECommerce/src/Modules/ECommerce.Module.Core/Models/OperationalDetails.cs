using System.Collections.Generic;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class OperationalDetails : EntityBase
    {
        // Id is inherited from EntityBase
        public long Id { get; set; }
        public long SupplierProfileId { get; set; }

        public int? YearsInBusiness { get; set; }

        public bool DeliveryServiceAvailable { get; set; }

        public string AvgMonthlySalesVolume { get; set; }

        public IList<ServiceArea> ServiceAreas { get; set; } = new List<ServiceArea>();

        public IList<CompanyTieup> CompanyTieups { get; set; } = new List<CompanyTieup>();
    }
}
