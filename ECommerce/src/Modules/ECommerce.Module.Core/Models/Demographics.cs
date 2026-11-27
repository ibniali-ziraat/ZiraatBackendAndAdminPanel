using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class Demographics : IEntityWithTypedId<long>
    {
        public long Id { get; set; }
        public long ProfileId { get; set; }

        public string FarmName { get; set; }
        public int FarmSize { get; set; }
        public string MainCrops { get; set; }
        public string OwnershipType { get; set; }
    }
}
