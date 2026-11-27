using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;

namespace ECommerce.Module.Core.Services
{
    public interface IVendorDashboardService
    {
        Task<VendorDashboardVm> GetDashboard(long vendorId);

        Task<VendorDashboardHomeVm> GetHome(long vendorId);

        Task<VendorInventoryDashboardVm> GetInventory(long vendorId);

        Task<VendorSalesDashboardVm> GetSales(long vendorId);

        Task<VendorCreditDashboardVm> GetCredit(long vendorId);

        Task<VendorFeeDashboardVm> GetFees(long vendorId);

        Task<VendorReportsDashboardVm> GetReports(long vendorId);

        Task<VendorOffersDashboardVm> GetOffers(long vendorId);

        Task<IList<VendorOfferDetailVm>> GetVendorOffers(long vendorId, string offerType = null, bool includeInactive = false);

        Task<VendorOfferDetailVm> GetVendorOffer(long vendorId, long offerId);

        Task<VendorOfferDetailVm> CreateVendorOffer(long vendorId, VendorOfferFormVm model, long currentUserId);

        Task<VendorOfferDetailVm> UpdateVendorOffer(long vendorId, long offerId, VendorOfferFormVm model, long currentUserId);

        Task DeleteVendorOffer(long vendorId, long offerId, long currentUserId);

        Task<VendorOfferDetailVm> SetVendorOfferStatus(long vendorId, long offerId, bool isActive, long currentUserId);

        IList<PlatformOfferDetailVm> GetPlatformOffers(long vendorId);

        PlatformOfferDetailVm GetPlatformOffer(long vendorId, string code);
    }
}
