using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Tax.Models;

namespace ECommerce.Module.Tax.Data
{
    public class TaxCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaxClass>().HasData(new TaxClass(1) { Name = "Standard VAT" });

            modelBuilder.Entity<AppSetting>().HasData(
                new AppSetting("Tax.DefaultTaxClassId") { Module = "Tax", IsVisibleInCommonSettingPage = true, Value = "1" }
            );
        }
    }
}
