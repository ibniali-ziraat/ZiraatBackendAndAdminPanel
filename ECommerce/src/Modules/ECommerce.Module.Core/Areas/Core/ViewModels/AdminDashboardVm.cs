using System.Collections.Generic;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class AdminDashboardVm
    {
        public string CurrentUserName { get; set; }

        public string CurrentUserInitials { get; set; }

        public IList<AdminDashboardMetricVm> Metrics { get; set; } = new List<AdminDashboardMetricVm>();

        public IList<AdminDashboardListItemVm> CustomerRegistrations { get; set; } = new List<AdminDashboardListItemVm>();

        public IList<AdminDashboardListItemVm> CustomerVerificationRates { get; set; } = new List<AdminDashboardListItemVm>();

        public IList<AdminDashboardListItemVm> OrdersByRegion { get; set; } = new List<AdminDashboardListItemVm>();

        public IList<AdminDashboardListItemVm> CustomersByRegion { get; set; } = new List<AdminDashboardListItemVm>();

        public IList<AdminDashboardCustomerActivityVm> CustomerActivity { get; set; } = new List<AdminDashboardCustomerActivityVm>();
    }

    public class AdminDashboardMetricVm
    {
        public string Label { get; set; }

        public string Value { get; set; }

        public string IconCssClass { get; set; }
    }

    public class AdminDashboardListItemVm
    {
        public string Label { get; set; }

        public string Value { get; set; }
    }

    public class AdminDashboardCustomerActivityVm
    {
        public string CustomerCode { get; set; }

        public string CustomerName { get; set; }

        public string Region { get; set; }

        public int Orders { get; set; }

        public int SupportStaff { get; set; }

        public int Transactions { get; set; }

        public string VerificationStatus { get; set; }
    }
}
