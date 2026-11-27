using System.ComponentModel.DataAnnotations.Schema;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class ServiceArea : EntityBase
    {
        // Id is inherited from EntityBase
        public long Id { get; set; }

        public long OperationalDetailsId { get; set; }

        public string PlaceName { get; set; }

        public OperationalDetails OperationalDetails { get; set; }
    }
}
