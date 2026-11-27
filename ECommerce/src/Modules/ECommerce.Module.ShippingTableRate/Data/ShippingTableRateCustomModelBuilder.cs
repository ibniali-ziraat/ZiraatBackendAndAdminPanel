using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Shipping.Models;

namespace ECommerce.Module.ShippingTableRate.Data
{
    public class ShippingTableRateCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ShippingProvider>().HasData(new ShippingProvider("TableRate") { Name = "Table Rate", IsEnabled = true, ConfigureUrl = "shipping-table-rate-config", ShippingPriceServiceTypeName = "ECommerce.Module.ShippingTableRate.Services.TableRateShippingServiceProvider,ECommerce.Module.ShippingTableRate", AdditionalSettings = null, ToAllShippingEnabledCountries = true, ToAllShippingEnabledStatesOrProvinces = true });
        }
    }
}
