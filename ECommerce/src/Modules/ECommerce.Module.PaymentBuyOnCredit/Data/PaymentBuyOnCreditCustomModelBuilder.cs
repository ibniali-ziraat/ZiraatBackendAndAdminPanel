using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Payments.Models;

namespace ECommerce.Module.PaymentCoD.Data
{
    public class PaymentBuyOnCreditCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentProvider>().HasData(
                new PaymentProvider("BuyOnCredit") { Name = "Buy On Credit", LandingViewComponentName = "BuyOnCreditLanding", ConfigureUrl = "payments-buyoncredit-config", IsEnabled = true, AdditionalSettings = null }
            );
        }
    }
}
