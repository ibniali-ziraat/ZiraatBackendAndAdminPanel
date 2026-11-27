using System.Threading.Tasks;
using ECommerce.Module.Catalog.Models;

namespace ECommerce.Module.Catalog.Services
{
    public interface IProductService
    {
        void Create(Product product);

        void Update(Product product);

        Task Delete(Product product);
    }
}
