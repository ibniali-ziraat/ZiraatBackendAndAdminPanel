using System.Threading.Tasks;
using ECommerce.Module.Cms.Models;

namespace ECommerce.Module.Cms.Services
{
    public interface IPageService
    {
        Task Create(Page page);

        Task Update(Page page);

        Task Delete(Page page);
    }
}
