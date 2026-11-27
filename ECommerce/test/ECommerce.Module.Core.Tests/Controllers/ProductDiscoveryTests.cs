using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Models;
using ECommerce.Module.Catalog.Areas.Catalog.Controllers;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ECommerce.Module.Core.Tests.Controllers
{
    public class ProductDiscoveryTests : IDisposable
    {
        private readonly DiscoveryDb _db = new();

        [Theory]
        [InlineData("trending")]
        [InlineData("search")]
        [InlineData("compare")]
        [InlineData("deals")]
        public async Task EndpointsExcludeUnassignedInactiveDeletedAndUnpublishedProducts(
            string endpoint
        )
        {
            await Seed();
            var controller = Controller();
            var result = endpoint switch
            {
                "trending" => await controller.GetTrendingProducts(limit: 1),
                "search" => await controller.SearchWithVendors(
                    new ProductSearchRequest { SearchTerm = "Product" }
                ),
                "compare" => await controller.CompareProducts("1,2,3,4,5,6"),
                _ => await controller.GetBestDeals(),
            };
            var json = JsonSerializer.SerializeToElement(
                Assert.IsType<OkObjectResult>(result).Value
            );
            var rows = json.GetProperty(
                endpoint switch
                {
                    "trending" => "products",
                    "search" => "results",
                    "compare" => "comparison",
                    _ => "deals",
                }
            );
            Assert.Equal(1, rows.GetArrayLength());
            Assert.Equal(1, rows[0].GetProperty("productId").GetInt64());
            if (endpoint != "deals")
                Assert.Equal(1, rows[0].GetProperty("vendorCount").GetInt32());
        }

        [Fact]
        public async Task SearchPricesComeFromEligibleVendorOffers()
        {
            await Seed();
            var controller = Controller();
            var included = Assert.IsType<OkObjectResult>(
                await controller.SearchWithVendors(
                    new ProductSearchRequest
                    {
                        SearchTerm = "Product",
                        MinPrice = 70,
                        MaxPrice = 90,
                    }
                )
            );
            Assert.Equal(
                1,
                JsonSerializer
                    .SerializeToElement(included.Value)
                    .GetProperty("resultsCount")
                    .GetInt32()
            );
            var excluded = Assert.IsType<OkObjectResult>(
                await controller.SearchWithVendors(
                    new ProductSearchRequest { SearchTerm = "Product", MaxPrice = 10 }
                )
            );
            Assert.Equal(
                0,
                JsonSerializer
                    .SerializeToElement(excluded.Value)
                    .GetProperty("resultsCount")
                    .GetInt32()
            );
        }

        [Fact]
        public async Task MultipleBatchesDoNotCountAsMultipleVendors()
        {
            await Seed();
            _db.Add(
                new VendorProduct
                {
                    ProductId = 1,
                    VendorId = 1,
                    ProductName = "Product",
                    AssignedPrice = 95,
                    StockQuantity = 2,
                }
            );
            await _db.SaveChangesAsync();
            var result = Assert.IsType<OkObjectResult>(
                await Controller().GetTrendingProducts(minVendors: 2)
            );
            Assert.Equal(
                0,
                JsonSerializer.SerializeToElement(result.Value).GetProperty("count").GetInt32()
            );
            var aggregate = await Service().GetAggregateProductData(1);
            Assert.Equal(1, aggregate.VendorCount);
            Assert.Equal(7, aggregate.TotalAggregatedStock);
        }

        [Fact]
        public async Task TrendingAndDealsOnlyUseVendorsAssociatedWithCurrentCustomer()
        {
            await Seed();
            var secondVendor = new Vendor { Name = "Second Vendor", Slug = "second-vendor" };
            _db.Entry(secondVendor).Property(x => x.Id).CurrentValue = 3;
            _db.Add(secondVendor);
            _db.Add(
                new VendorCustomerAssociation
                {
                    CustomerId = 43,
                    VendorId = 3,
                    IsActive = true,
                    IsDeleted = false,
                }
            );
            _db.Add(
                new VendorProduct
                {
                    ProductId = 1,
                    VendorId = 3,
                    ProductName = "Product",
                    AssignedPrice = 70,
                    DiscountPrice = 55,
                    StockQuantity = 3,
                }
            );
            await _db.SaveChangesAsync();
            var controller = Controller(43);

            var trending = Assert.IsType<OkObjectResult>(await controller.GetTrendingProducts());
            var trendingProducts = JsonSerializer
                .SerializeToElement(trending.Value)
                .GetProperty("products");
            Assert.Single(trendingProducts.EnumerateArray());
            Assert.Equal(3, trendingProducts[0].GetProperty("vendorId").GetInt64());
            Assert.Equal(70, trendingProducts[0].GetProperty("originalPrice").GetDecimal());
            Assert.Equal(55, trendingProducts[0].GetProperty("lowestPrice").GetDecimal());
            Assert.Equal(3, trendingProducts[0].GetProperty("totalStock").GetInt32());

            var deals = Assert.IsType<OkObjectResult>(await controller.GetBestDeals());
            var vendorDeals = JsonSerializer.SerializeToElement(deals.Value).GetProperty("deals");
            Assert.Single(vendorDeals.EnumerateArray());
            Assert.Equal(3, vendorDeals[0].GetProperty("vendorId").GetInt64());
            Assert.Equal(70, vendorDeals[0].GetProperty("originalPrice").GetDecimal());
            Assert.Equal(55, vendorDeals[0].GetProperty("discountedPrice").GetDecimal());
        }

        private async Task Seed()
        {
            var vendor = new Vendor { Name = "Vendor", Slug = "vendor" };
            var deleted = new Vendor
            {
                Name = "Deleted",
                Slug = "deleted",
                IsDeleted = true,
            };
            _db.Entry(vendor).Property(x => x.Id).CurrentValue = 1;
            _db.Entry(deleted).Property(x => x.Id).CurrentValue = 2;
            _db.AddRange(vendor, deleted);
            _db.Add(
                new VendorCustomerAssociation
                {
                    CustomerId = 42,
                    VendorId = 1,
                    IsActive = true,
                    IsDeleted = false,
                }
            );
            for (var id = 1; id <= 7; id++)
            {
                var product = new Product
                {
                    Name = "Product",
                    Slug = "product-" + id,
                    IsPublished = id != 5,
                    IsDeleted = id == 6,
                    Price = 999,
                    CreatedOn = DateTimeOffset.UtcNow.AddDays(id),
                };
                _db.Entry(product).Property(x => x.Id).CurrentValue = id;
                _db.Add(product);
                if (id != 2)
                    _db.Add(
                        new VendorProduct
                        {
                            ProductId = id,
                            VendorId = id == 4 ? 2 : 1,
                            ProductName = "Product",
                            AssignedPrice = 100,
                            DiscountPrice = 80,
                            StockQuantity = 5,
                            IsActive = id != 3,
                            ExpiryDate = id == 7 ? DateTime.UtcNow.Date.AddDays(-1) : null,
                        }
                    );
            }
            // A deleted vendor must not influence an otherwise eligible product's aggregate.
            _db.Add(
                new VendorProduct
                {
                    ProductId = 1,
                    VendorId = 2,
                    ProductName = "Product",
                    AssignedPrice = 1,
                    StockQuantity = 100,
                }
            );
            await _db.SaveChangesAsync();
        }

        private IRepository<T> Repository<T>()
            where T : class, IEntityWithTypedId<long>
        {
            var mock = new Mock<IRepository<T>>();
            mock.Setup(x => x.Query()).Returns(() => _db.Set<T>());
            return mock.Object;
        }

        private VendorProductService Service() =>
            new(Repository<VendorProduct>(), null, null, Repository<Vendor>(), null);

        private ProductDiscoveryController Controller(long customerId = 42)
        {
            var controller = new ProductDiscoveryController(
                Repository<Product>(),
                Repository<VendorProduct>(),
                Repository<VendorCustomerAssociation>(),
                Mock.Of<IRepository<Category>>(),
                Service(),
                Mock.Of<IMediaService>()
            );
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, customerId.ToString()) },
                            "test"
                        )
                    ),
                },
            };
            return controller;
        }

        public void Dispose() => _db.Dispose();

        private sealed class DiscoveryDb : DbContext
        {
            public DiscoveryDb()
                : base(
                    new DbContextOptionsBuilder<DiscoveryDb>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .Options
                ) { }

            protected override void OnModelCreating(ModelBuilder builder)
            {
                Map<Product>(builder, "Medias", "ThumbnailImage");
                Map<VendorProduct>(builder, "Vendor");
                Map<Vendor>(builder);
                Map<VendorCustomerAssociation>(builder);
                Map<ProductMedia>(builder, "Product", "Media");
                Map<Media>(builder);
                builder
                    .Entity<Product>()
                    .HasMany(x => x.Medias)
                    .WithOne(x => x.Product)
                    .HasForeignKey(x => x.ProductId);
                builder.Entity<Product>().HasOne(x => x.ThumbnailImage).WithMany();
                builder
                    .Entity<ProductMedia>()
                    .HasOne(x => x.Media)
                    .WithMany()
                    .HasForeignKey(x => x.MediaId);
                builder
                    .Entity<VendorProduct>()
                    .HasOne(x => x.Vendor)
                    .WithMany()
                    .HasForeignKey(x => x.VendorId);
            }

            private static void Map<T>(ModelBuilder builder, params string[] navigations)
                where T : class
            {
                var entity = builder.Entity<T>();
                foreach (var property in typeof(T).GetProperties())
                {
                    if (
                        !property.PropertyType.IsValueType
                        && property.PropertyType != typeof(string)
                        && !navigations.Contains(property.Name)
                    )
                        entity.Ignore(property.Name);
                }
            }
        }
    }
}
