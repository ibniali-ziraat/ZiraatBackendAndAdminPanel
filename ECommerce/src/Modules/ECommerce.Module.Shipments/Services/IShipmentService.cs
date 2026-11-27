using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Infrastructure;
using ECommerce.Module.Shipments.Models;

namespace ECommerce.Module.Shipments.Services
{
    public interface IShipmentService
    {
        Task<IList<ShipmentItemVm>> GetShipmentItem(long orderId);

        Task<IList<ShipmentItemVm>> GetItemToShip(long orderId, long warehouseId);

        Task<Result> CreateShipment(Shipment shipment);
    }
}
