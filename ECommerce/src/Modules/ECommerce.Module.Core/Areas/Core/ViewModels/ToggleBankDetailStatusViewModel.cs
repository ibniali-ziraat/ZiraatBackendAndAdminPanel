using System.Text.Json.Serialization;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class ToggleBankDetailStatusViewModel
    {
        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }
    }
}
