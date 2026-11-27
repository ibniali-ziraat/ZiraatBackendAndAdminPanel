using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Data
{
    public class CoreCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppSetting>().ToTable("Core_AppSetting");

            modelBuilder.Entity<BlobFileReference>(b =>
            {
                b.ToTable("Core_BlobFileReference");
                b.HasKey(x => x.Id);
                b.Property(x => x.BlobName).HasMaxLength(1024).IsRequired();
                b.Property(x => x.BlobUrl).HasMaxLength(2048);
                b.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
                b.HasIndex(x => new { x.EntityType, x.EntityId });
            });

            modelBuilder.Entity<User>()
                .ToTable("Core_User");

            modelBuilder.Entity<Role>()
                .ToTable("Core_Role");

            modelBuilder.Entity<IdentityUserClaim<long>>(b =>
            {
                b.HasKey(uc => uc.Id);
                b.ToTable("Core_UserClaim");
            });

            modelBuilder.Entity<IdentityRoleClaim<long>>(b =>
            {
                b.HasKey(rc => rc.Id);
                b.ToTable("Core_RoleClaim");
            });

            modelBuilder.Entity<UserRole>(b =>
            {
                b.HasKey(ur => new { ur.UserId, ur.RoleId });
                b.HasOne(ur => ur.Role).WithMany(x => x.Users).HasForeignKey(r => r.RoleId);
                b.HasOne(ur => ur.User).WithMany(x => x.Roles).HasForeignKey(u => u.UserId);
                b.ToTable("Core_UserRole");
            });

            modelBuilder.Entity<IdentityUserLogin<long>>(b =>
            {
                b.ToTable("Core_UserLogin");
            });

            modelBuilder.Entity<IdentityUserToken<long>>(b =>
            {
                b.ToTable("Core_UserToken");
            });

            modelBuilder.Entity<Entity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.EntityId);
            });

            modelBuilder.Entity<User>(u =>
            {
                u.HasOne(x => x.DefaultShippingAddress)
                   .WithMany()
                   .HasForeignKey(x => x.DefaultShippingAddressId)
                   .OnDelete(DeleteBehavior.Restrict);

                u.HasOne(x => x.DefaultBillingAddress)
                    .WithMany()
                    .HasForeignKey(x => x.DefaultBillingAddressId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserAddress>()
                .HasOne(x => x.User)
                .WithMany(a => a.UserAddresses)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Address>(x =>
            {
                x.HasOne(d => d.District)
                   .WithMany()
                   .OnDelete(DeleteBehavior.Restrict);

                x.HasOne(d => d.StateOrProvince)
                    .WithMany()
                    .OnDelete(DeleteBehavior.Restrict);

                x.HasOne(d => d.Country)
                    .WithMany()
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CustomerGroup>()
                .HasIndex(d => d.Name)
                .IsUnique();

            modelBuilder.Entity<CustomerGroupUser>(b =>
            {
                b.HasKey(ur => new { ur.UserId, ur.CustomerGroupId });
                b.HasOne(ur => ur.User).WithMany(r => r.CustomerGroups).HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(ur => ur.CustomerGroup).WithMany(u => u.Users).HasForeignKey(u => u.CustomerGroupId).OnDelete(DeleteBehavior.Cascade);
                b.ToTable("Core_CustomerGroupUser");
            });

            modelBuilder.Entity<VendorOffer>(b =>
            {
                b.ToTable("Core_VendorOffer");
                b.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.LatestUpdatedBy).WithMany().HasForeignKey(x => x.LatestUpdatedById).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VendorProduct>(b =>
            {
                b.ToTable("Core_VendorProduct");
                b.HasOne(x => x.Vendor).WithMany(x => x.AssignedProducts).HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.LatestUpdatedBy).WithMany().HasForeignKey(x => x.LatestUpdatedById).OnDelete(DeleteBehavior.Restrict);
                b.HasMany(x => x.Variations).WithOne(x => x.VendorProduct).HasForeignKey(x => x.VendorProductId).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(x => new { x.VendorId, x.ProductId });
            });

            modelBuilder.Entity<VendorProductVariation>(b =>
            {
                b.ToTable("Core_VendorProductVariation");
                b.HasMany(x => x.OptionCombinations).WithOne(x => x.VendorProductVariation).HasForeignKey(x => x.VendorProductVariationId).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(x => new { x.VendorProductId, x.Sku });
            });

            modelBuilder.Entity<VendorProductVariationOption>(b =>
            {
                b.ToTable("Core_VendorProductVariationOption");
                b.HasIndex(x => new { x.VendorProductVariationId, x.OptionId, x.Value });
            });

            modelBuilder.Entity<FarmActivity>(b =>
            {
                b.ToTable("Core_FarmActivity");
                b.HasOne(x => x.OrchardProfile).WithMany().HasForeignKey(x => x.OrchardProfileId).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(x => new { x.OrchardProfileId, x.OccurredOn });
                b.Property(x => x.Quantity).HasPrecision(18, 4);
                b.Property(x => x.Amount).HasPrecision(18, 2);
            });
            modelBuilder.Entity<FarmAssessment>(b =>
            {
                b.ToTable("Core_FarmAssessment");
                b.HasOne(x => x.OrchardProfile).WithMany().HasForeignKey(x => x.OrchardProfileId).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(x => new { x.OrchardProfileId, x.ObservedOn });
                foreach (var property in b.Metadata.GetProperties())
                {
                    if (property.ClrType == typeof(decimal?))
                    {
                        property.SetPrecision(18);
                        property.SetScale(4);
                    }
                }
            });

            CoreSeedData.SeedData(modelBuilder);
        }
    }
}
