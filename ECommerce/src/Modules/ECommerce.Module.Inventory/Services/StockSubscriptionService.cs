using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Web;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Inventory.Models;

namespace ECommerce.Module.Inventory.Services
{
    public class StockSubscriptionService : IStockSubscriptionService
    {
        private readonly IRepository<ProductBackInStockSubscription> _productBackInStockSubscriptionRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IEmailSender _emailSender;


        public StockSubscriptionService(IRepository<ProductBackInStockSubscription> backInStockSubscriptionRepository, IEmailSender emailSender, IRepository<Product> productRepository)
        {
            _productBackInStockSubscriptionRepository = backInStockSubscriptionRepository;
            _emailSender = emailSender;

            _productRepository = productRepository;
        }

        public async Task ProductBackInStockSendNotificationsAsync(long productId)
        {
            var subscriptions = await _productBackInStockSubscriptionRepository
                .Query()
                .Where(o => o.ProductId == productId)
                .ToListAsync();

            var product = await _productRepository
                .Query()
                .Where(o => o.Id == productId)
                .Include(o => o.ThumbnailImage)
                .FirstOrDefaultAsync();


            var emailSubject = $"Back in stock";
            var emailBody = $"The product {product.Name} is back in stock.";

            foreach (var subscription in subscriptions)
            {
                await _emailSender.SendEmailAsync(subscription.CustomerEmail, emailSubject, emailBody, true);
                
                _productBackInStockSubscriptionRepository.Remove(subscription);
            }

            await _productBackInStockSubscriptionRepository.SaveChangesAsync();
        }

        public async Task ProductBackInStockSubscribeAsync(long productId, string customerEmail)
        {
            var subscription = new ProductBackInStockSubscription
            {
                ProductId = productId,
                CustomerEmail = customerEmail
            };

            _productBackInStockSubscriptionRepository.Add(subscription);

            await _productBackInStockSubscriptionRepository.SaveChangesAsync();
        }
    }
}
