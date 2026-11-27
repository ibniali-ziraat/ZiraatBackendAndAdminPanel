using System.ComponentModel.DataAnnotations.Schema;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class CompanyTieup : EntityBase
    {
        // Id is inherited from EntityBase

        public long Id { get; set; }

        public long OperationalDetailsId { get; set; }

        public string Name { get; set; }

        public OperationalDetails OperationalDetails { get; set; }
    }
}
