using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Vendors.Services
{
    public interface IVendorService
    {
        Task Create(Vendor brand);

        Task Update(Vendor brand);

        Task Delete(long id);

        Task Delete(Vendor brand);
    }
}
