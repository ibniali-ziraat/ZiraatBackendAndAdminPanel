using ECommerce.Infrastructure.Data;
using ECommerce.Module.PaymentRazor.Models;
using ECommerce.Module.Payments.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.PaymentRazor.Data
{
    public class PaymentRazorCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentProvider>().HasData(
                new PaymentProvider("Razorpay")
                {
                    Name = "Razorpay",
                    LandingViewComponentName = "RazorpayLanding",
                    ConfigureUrl = "payments-razor-config",
                    IsEnabled = true,
                    AdditionalSettings = "{\"KeyId\":\"\",\"KeySecret\":\"\",\"WebhookSecret\":\"\",\"Currency\":\"\",\"PayoutAccountNumber\":\"\",\"PayoutMode\":\"\",\"PayoutUrl\":\"\"}"
                }
            );


        }
    }
}
