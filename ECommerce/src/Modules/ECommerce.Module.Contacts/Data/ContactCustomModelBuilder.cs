using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Contacts.Data
{
    public class ContactCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppSetting>().HasData(
                new AppSetting("GoogleAppKey") { Module = "Contact", IsVisibleInCommonSettingPage = false, Value = "" }
            );
        }
    }
}
