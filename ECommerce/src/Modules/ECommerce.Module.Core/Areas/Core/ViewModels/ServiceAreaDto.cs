using System.Collections.Generic;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class ServiceAreaDto
    {
        public long Id { get; set; }
        public string PlaceName { get; set; }
    }

    public class CompanyTieupDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class OperationalDetailsResponseVm
    {
        public long Id { get; set; }
        public long SupplierProfileId { get; set; }
        public int? YearsInBusiness { get; set; }
        public bool DeliveryServiceAvailable { get; set; }
        public string AvgMonthlySalesVolume { get; set; }

        public IList<ServiceAreaDto> ServiceAreas { get; set; } = new List<ServiceAreaDto>();
        public IList<CompanyTieupDto> CompanyTieups { get; set; } = new List<CompanyTieupDto>();
    }
}