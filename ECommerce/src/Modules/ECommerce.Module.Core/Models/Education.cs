using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class Education : EntityBase
    {
        public long ProfileId { get; set; }
        public string HighestQualification { get; set; }
        public string InstituteName { get; set; }
        public int PassingYear { get; set; }
        public string Specialization { get; set; }
    }
}
