using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Payments.Models;

namespace ECommerce.Module.PaymentCoD.Data
{
    public class PaymentCoDCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentProvider>().HasData(
                new PaymentProvider("CoD") { Name = "Cash On Delivery", LandingViewComponentName = "CoDLanding", ConfigureUrl = "payments-cod-config", IsEnabled = true, AdditionalSettings = null }
            );
        }
    }
}
