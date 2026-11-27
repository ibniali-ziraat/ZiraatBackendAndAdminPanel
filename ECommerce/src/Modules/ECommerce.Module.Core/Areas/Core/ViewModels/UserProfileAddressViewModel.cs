namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class UserProfileAddressViewModel
    {
        public UserProfileAddressViewModel() { }
        public long ProfileId { get; set; }
        public string Village { get; set; }
        public string Tehsil { get; set; }
        public string District { get; set; }
        public string State { get; set; }
        public string Pincode { get; set; }
        public string Gps { get; set; }
    }
}
