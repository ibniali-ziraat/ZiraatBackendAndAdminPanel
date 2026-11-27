using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Payments.Models;

namespace ECommerce.Module.Payments.Data
{
    public class PaymentsCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentProvider>().ToTable("Payments_PaymentProvider");
        }
    }
}
