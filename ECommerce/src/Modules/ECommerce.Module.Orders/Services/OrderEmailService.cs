using System.Linq;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Orders.Models;

namespace ECommerce.Module.Orders.Services
{
    public class OrderEmailService : IOrderEmailService
    {
        private readonly IEmailSender _emailSender;

        public OrderEmailService(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task SendEmailToUser(User user, Order order)
        {
            // Build a minimal HTML representation of the order for the email body
            var itemsHtml = string.Join("", order.OrderItems.Select(i =>
                $"<li>{i.Product?.Name ?? "Product"} - {i.Quantity} x {i.ProductPrice:C} = {(i.ProductPrice * i.Quantity):C}</li>"));

            var emailBody =
                $"<html><head><meta charset=\"utf-8\"/></head><body>" +
                $"<h1>Order #{order.Id}</h1>" +
                $"<p>Customer: {user?.FullName ?? ""} ({user?.Email ?? ""})</p>" +
                $"<p>Order Total: {order.OrderTotal:C}</p>" +
                "<h2>Items</h2><ul>" +
                itemsHtml +
                "</ul></body></html>";

            var emailSubject = $"Order information #{order.Id}";
            await _emailSender.SendEmailAsync(user.Email, emailSubject, emailBody, true);
        }
    }
}
