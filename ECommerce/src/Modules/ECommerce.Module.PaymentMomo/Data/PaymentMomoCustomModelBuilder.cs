using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Payments.Models;

namespace ECommerce.Module.PaymentMomo.Data
{
    public class PaymentMomoCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentProvider>().HasData(
                new PaymentProvider("MomoPayment") { Name = "Momo Payment", LandingViewComponentName = "MomoLanding", ConfigureUrl = "payments-momo-config", IsEnabled = true, AdditionalSettings = "{\"IsSandbox\":true,\"PartnerCode\":\"MOMOIQA420180417\",\"AccessKey\":\"SvDmj2cOTYZmQQ3H\",\"SecretKey\":\"PPuDXq1KowPT1ftR8DvlQTHhC03aul17\",\"PaymentFee\":0.0}" }
            );
        }
    }
}
