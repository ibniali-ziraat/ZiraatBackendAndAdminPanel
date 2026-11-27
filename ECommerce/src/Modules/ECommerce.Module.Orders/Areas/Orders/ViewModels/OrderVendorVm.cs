using System.Linq;

namespace ECommerce.Module.Orders.Areas.Orders.ViewModels
{
    public class OrderVendorVm
    {
        public long VendorId { get; set; }

        public string VendorName { get; set; }

        public string ShopName { get; set; }

        public OrderVendorAddressVm Address { get; set; }
    }

    public class OrderVendorAddressVm
    {
        public string Village { get; set; }

        public string Tehsil { get; set; }

        public string District { get; set; }

        public string State { get; set; }

        public string Pincode { get; set; }

        public string FormattedAddress => string.Join(", ",
            new[] { Village, Tehsil, District, State, Pincode }
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
    }
}
