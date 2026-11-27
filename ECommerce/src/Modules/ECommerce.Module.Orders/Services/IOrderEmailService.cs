using System.Threading.Tasks;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Orders.Models;

namespace ECommerce.Module.Orders.Services
{
    public interface IOrderEmailService
    {
        Task SendEmailToUser(User user, Order order);
    }
}
