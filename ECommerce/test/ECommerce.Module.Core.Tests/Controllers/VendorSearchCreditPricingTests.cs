using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Localization;
using ECommerce.Infrastructure.Models;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Catalog.Services;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Search.Areas.Search.Controllers;
using ECommerce.Module.Search.Areas.Search.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ECommerce.Module.Core.Tests.Controllers
{
    public class VendorSearchCreditPricingTests
    {
        [Theory]
        [InlineData(true, 150, 150)]
        [InlineData(true, null, 121)]
        [InlineData(false, 150, null)]
        public async Task SearchReturnsVendorCreditPriceWithoutChangingRegularPrice(
            bool creditEnabled,
            int? storedPrice,
            int? expected
        )
        {
            var product = new Product
            {
                Name = "Apple",
                IsPublished = true,
                IsVisibleIndividually = true,
                Price = 999,
            };
            var vendor = new Vendor { Name = "Vendor", IsActive = true };
            var assignment = new VendorProduct
            {
                ProductId = product.Id,
                VendorId = vendor.Id,
                AssignedPrice = 100,
                TaxRate = 10,
                StockQuantity = 5,
                IsActive = true,
                IsAvailableForCreditBuyers = creditEnabled,
                CreditSaleHikePercentage = 10,
                SalePriceForCreditor = storedPrice,
            };
            var controller = new SearchController(
                Repository(product),
                null,
                null,
                Repository<LocalizedContentProperty>(),
                Mock.Of<IMediaService>(),
                null,
                Mock.Of<IProductPricingService>(),
                Mock.Of<IContentLocalizationService>(),
                null,
                Repository(
                    new VendorCustomerAssociation
                    {
                        CustomerId = 7,
                        VendorId = vendor.Id,
                        IsActive = true,
                    }
                ),
                Repository(vendor),
                Repository(assignment),
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        new Dictionary<string, string> { ["Catalog.ProductPageSize"] = "20" }
                    )
                    .Build()
            );
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, "7") },
                            "test"
                        )
                    ),
                },
            };

            var response = Assert.IsType<OkObjectResult>(
                await controller.SearchByVendorsForCustomer("Apple")
            );
            var item = Assert.Single(Assert.IsType<SearchResult>(response.Value).Products);
            Assert.Equal(100m, item.Price);
            Assert.Equal(110m, item.NetSalePriceForZiraat);
            Assert.Equal(creditEnabled, item.IsAvailableForCreditBuyers);
            Assert.Equal((decimal?)expected, item.SalePriceForCreditor);
            Assert.Equal(10m, item.CreditSaleHikePercentage);
            Assert.Null(item.OldPrice);
            Assert.Equal(assignment.Id, item.VendorProductId);
        }

        [Fact]
        public async Task SearchExcludesExpiredVendorAssignments()
        {
            var vendor = new Vendor { Name = "Vendor", IsActive = true };
            var product = new Product
            {
                Name = "Apple fresh",
                IsPublished = true,
                IsVisibleIndividually = true,
                Price = 100,
            };
            var assignments = new[]
            {
                new VendorProduct
                {
                    ProductId = product.Id,
                    VendorId = vendor.Id,
                    AssignedPrice = 90,
                    StockQuantity = 5,
                    IsActive = true,
                    ExpiryDate = null,
                },
                new VendorProduct
                {
                    ProductId = product.Id,
                    VendorId = vendor.Id,
                    AssignedPrice = 80,
                    StockQuantity = 5,
                    IsActive = true,
                    ExpiryDate = DateTime.UtcNow.Date.AddDays(-1),
                },
            };
            var controller = new SearchController(
                Repository(product),
                null,
                null,
                Repository<LocalizedContentProperty>(),
                Mock.Of<IMediaService>(),
                null,
                Mock.Of<IProductPricingService>(),
                Mock.Of<IContentLocalizationService>(),
                null,
                Repository(
                    new VendorCustomerAssociation
                    {
                        CustomerId = 7,
                        VendorId = vendor.Id,
                        IsActive = true,
                    }
                ),
                Repository(vendor),
                Repository(assignments),
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        new Dictionary<string, string> { ["Catalog.ProductPageSize"] = "20" }
                    )
                    .Build()
            );
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, "7") },
                            "test"
                        )
                    ),
                },
            };

            var response = Assert.IsType<OkObjectResult>(
                await controller.SearchByVendorsForCustomer("Apple")
            );
            var results = Assert.IsType<SearchResult>(response.Value);

            Assert.Equal(1, results.TotalProduct);
            Assert.Equal(90m, Assert.Single(results.Products).Price);
        }

        private static IRepository<T> Repository<T>(params T[] items)
            where T : class, IEntityWithTypedId<long>
        {
            var mock = new Mock<IRepository<T>>();
            mock.Setup(x => x.Query()).Returns(items.AsQueryable());
            return mock.Object;
        }
    }
}
