using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Shipping.Models;

namespace ECommerce.Module.ShippingFree.Data
{
    public class ShippingFreeCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ShippingProvider>().HasData(new ShippingProvider("FreeShip") { Name = "Free Ship", IsEnabled = true, ConfigureUrl = "", ShippingPriceServiceTypeName = "ECommerce.Module.ShippingFree.Services.FreeShippingServiceProvider,ECommerce.Module.ShippingFree", AdditionalSettings = "{MinimumOrderAmount : 1}", ToAllShippingEnabledCountries = true, ToAllShippingEnabledStatesOrProvinces = true });
        }
    }
}
