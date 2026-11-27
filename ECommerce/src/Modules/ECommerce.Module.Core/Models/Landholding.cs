using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class Landholding: IEntityWithTypedId<long>
    {
        public long Id { get; set; }
        public long ProfileId { get; set; }
        public string SoilType { get; set; }
        public string IrrigationMethod { get; set; }
        public string EnvironmentalConditions { get; set; }
        public string LandTopography { get; set; }
    }
}
