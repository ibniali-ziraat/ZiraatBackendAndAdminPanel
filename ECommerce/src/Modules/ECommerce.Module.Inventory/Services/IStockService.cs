using ECommerce.Module.Inventory.Models;
using System.Threading.Tasks;

namespace ECommerce.Module.Inventory.Services
{
    public interface IStockService
    {
        Task AddAllProduct(Warehouse warehouse);

        Task UpdateStock(StockUpdateRequest stockUpdateRequest);
    }
}
