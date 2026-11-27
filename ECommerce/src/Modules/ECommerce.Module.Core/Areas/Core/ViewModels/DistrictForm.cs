using System.ComponentModel.DataAnnotations;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class DistrictForm
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }

        [StringLength(450)]
        public string Type { get; set; }

        public string Location { get; set; }
    }
}
