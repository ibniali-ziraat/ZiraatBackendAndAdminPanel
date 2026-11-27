using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Inventory.Models;

namespace ECommerce.Module.Inventory.Data
{
    public class InventoryCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Warehouse>().HasData(new Warehouse(1) { Name = "Default warehouse", AddressId = 1 });
        }
    }
}
