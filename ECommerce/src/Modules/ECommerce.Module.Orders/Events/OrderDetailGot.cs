using MediatR;
using ECommerce.Module.Orders.Areas.Orders.ViewModels;

namespace ECommerce.Module.Orders.Events
{
    public class OrderDetailGot : INotification
    {
        public OrderDetailVm OrderDetailVm { get; set; }
    }
}
