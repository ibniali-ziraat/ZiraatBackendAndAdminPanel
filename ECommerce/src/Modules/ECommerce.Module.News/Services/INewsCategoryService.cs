using System.Threading.Tasks;
using ECommerce.Module.News.Models;

namespace ECommerce.Module.News.Services
{
    public interface INewsCategoryService
    {
        Task Create(NewsCategory category);

        Task Update(NewsCategory category);

        Task Delete(long id);

        Task Delete(NewsCategory category);
    }
}
