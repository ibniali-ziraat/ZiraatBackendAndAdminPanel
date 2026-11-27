using System.Threading.Tasks;
using ECommerce.Module.News.Models;

namespace ECommerce.Module.News.Services
{
    public interface INewsItemService
    {
        void Create(NewsItem newsItem);

        void Update(NewsItem newsItem);

        Task Delete(long id);

        Task Delete(NewsItem newsItem);
    }
}
