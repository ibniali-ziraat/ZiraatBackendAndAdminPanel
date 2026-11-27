using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Comments.Data
{
    public class CommentCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppSetting>().HasData(
                new AppSetting("Catalog.IsCommentsRequireApproval") { Module = "Catalog", IsVisibleInCommonSettingPage = true, Value = "true" }
            );
        }
    }
}
