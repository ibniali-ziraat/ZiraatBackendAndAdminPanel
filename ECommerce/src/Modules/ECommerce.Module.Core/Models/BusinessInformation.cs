using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class BusinessInformation : EntityBase
    {
        // Id is inherited from EntityBase (long)

        public long UserId { get; set; }
        public long VendorId { get; set; }

        public string ShopName { get; set; }
        public string OwnerName { get; set; }
        public string ContactNumberPrimary { get; set; }
        public string ContactNumberAlternate { get; set; }
        public string Email { get; set; }
        public string Village { get; set; }
        public string Tehsil { get; set; }
        public string District { get; set; }
        public string State { get; set; }
        public string Pincode { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
