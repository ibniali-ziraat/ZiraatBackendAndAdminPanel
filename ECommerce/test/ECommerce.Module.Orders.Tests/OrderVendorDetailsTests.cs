using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Orders.Areas.Orders.Controllers;
using ECommerce.Module.Orders.Areas.Orders.ViewModels;
using ECommerce.Module.Orders.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ECommerce.Module.Orders.Tests
{
    public class OrderVendorDetailsTests : IDisposable
    {
        private readonly VendorDb _db = new();
        private readonly Mock<IRepository<Vendor>> _vendors = new();
        private readonly Mock<IRepository<BusinessInformation>> _businesses = new();

        public OrderVendorDetailsTests()
        {
            _vendors.Setup(x => x.Query()).Returns(() => _db.Set<Vendor>());
            _businesses.Setup(x => x.Query()).Returns(() => _db.Set<BusinessInformation>());
        }

        [Fact]
        public async Task SingleVendorOrderReturnsNameAndBusinessAddress()
        {
            await SeedVendor(10014, "Vendor A", "Village A");
            var order = Order(10014, 10014);
            var model = await Read(order);
            Assert.Equal(10014, model.VendorId);
            Assert.Equal("Vendor A", model.Vendor.VendorName);
            Assert.Equal("Shop 10014", model.Vendor.ShopName);
            Assert.Equal("Village A", model.Vendor.Address.Village);
            Assert.Equal("Tehsil, District, State, 190001",
                new OrderVendorAddressVm { Tehsil = "Tehsil", District = "District", State = "State", Pincode = "190001" }.FormattedAddress);
            Assert.Same(model.Vendor, Assert.Single(model.OrderItems).Vendor);
            Assert.Equal("Customer address", model.ShippingAddress.AddressLine1);
        }

        [Fact]
        public async Task MultiVendorOrderMapsEachItemAndBatchesLookups()
        {
            await SeedVendor(10014, "Vendor A", "Village A");
            await SeedVendor(10015, "Vendor B", "Village B");
            var model = await Read(Order(null, 10014, 10015, 10014));
            Assert.Null(model.Vendor);
            Assert.Equal("Vendor A", model.OrderItems[0].Vendor.VendorName);
            Assert.Equal("Village B", model.OrderItems[1].Vendor.Address.Village);
            Assert.Same(model.OrderItems[0].Vendor, model.OrderItems[2].Vendor);
            _vendors.Verify(x => x.Query(), Times.Once);
            _businesses.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task LegacyItemWithoutVendorIdUsesOrderVendor()
        {
            await SeedVendor(10014, "Vendor A", "Village A");
            var model = await Read(Order(10014, (long?)null));
            Assert.Equal(10014, Assert.Single(model.OrderItems).Vendor.VendorId);
        }

        [Fact]
        public async Task MissingBusinessProfileReturnsNameAndNullAddress()
        {
            var vendor = new Vendor { Name = "Vendor A", Slug = "vendor-a" };
            _db.Entry(vendor).Property(x => x.Id).CurrentValue = 10014;
            _db.Add(vendor);
            await _db.SaveChangesAsync();
            var model = await Read(Order(10014, 10014));
            Assert.Equal("Vendor A", model.Vendor.VendorName);
            Assert.Null(model.Vendor.Address);
        }

        [Fact]
        public async Task MissingVendorDoesNotBreakHistoricalOrder()
        {
            var model = await Read(Order(10014, 10014));
            Assert.Equal(10014, model.VendorId);
            Assert.Null(model.Vendor);
            Assert.Null(Assert.Single(model.OrderItems).Vendor);
        }

        [Fact]
        public async Task SoftDeletedVendorStillIdentifiesHistoricalSeller()
        {
            await SeedVendor(10014, "Former vendor", "Village", isDeleted: true);
            var model = await Read(Order(10014, 10014));
            Assert.Equal("Former vendor", model.Vendor.VendorName);
        }

        [Fact]
        public async Task OrderWithoutVendorsSkipsBusinessLookups()
        {
            var model = await Read(Order(null, (long?)null));
            Assert.Null(model.Vendor);
            _vendors.Verify(x => x.Query(), Times.Never);
            _businesses.Verify(x => x.Query(), Times.Never);
        }

        [Fact]
        public async Task AnotherCustomersOrderDoesNotExposeVendorDetails()
        {
            var order = Order(10014, 10014);
            var result = await Controller(order, new User { Id = 88 }).OrderDetails(order.Id);
            Assert.IsType<BadRequestObjectResult>(result);
            _vendors.Verify(x => x.Query(), Times.Never);
            _businesses.Verify(x => x.Query(), Times.Never);
        }

        [Fact]
        public async Task MissingAuthenticatedAccountReturnsUnauthorized()
        {
            var order = Order(10014, 10014);
            Assert.IsType<UnauthorizedResult>(await Controller(order, null).OrderDetails(order.Id));
            _vendors.Verify(x => x.Query(), Times.Never);
            _businesses.Verify(x => x.Query(), Times.Never);
        }

        [Fact]
        public void FormattedAddressSkipsBlankParts()
        {
            var address = new OrderVendorAddressVm { Village = " Village ", Tehsil = " ", State = "State" };
            Assert.Equal("Village, State", address.FormattedAddress);
        }

        private async Task SeedVendor(long id, string name, string village, bool isDeleted = false)
        {
            var vendor = new Vendor { Name = name, Slug = "vendor-" + id, IsDeleted = isDeleted };
            _db.Entry(vendor).Property(x => x.Id).CurrentValue = id;
            _db.Add(vendor);
            _db.Add(new BusinessInformation
            {
                VendorId = id, ShopName = "Shop " + id, Village = village,
                Tehsil = "Tehsil", District = "District", State = "State", Pincode = "190001",
                OwnerName = "Not exposed", ContactNumberAlternate = "Not exposed"
            });
            await _db.SaveChangesAsync();
        }

        private static Order Order(long? vendorId, params long?[] itemVendors)
        {
            var order = new Order
            {
                CustomerId = 7, Customer = new User { Id = 7, FullName = "Customer" },
                VendorId = vendorId, IsMasterOrder = !vendorId.HasValue,
                ShippingAddress = new OrderAddress
                {
                    AddressLine1 = "Customer address",
                    StateOrProvince = new StateOrProvince { Name = "Customer state" }
                }
            };
            foreach (var id in itemVendors)
                order.AddOrderItem(new OrderItem { VendorId = id, Product = new Product { Name = "Product" }, Quantity = 1 });
            return order;
        }

        private async Task<OrderDetailVm> Read(Order order) =>
            Assert.IsType<OrderDetailVm>(Assert.IsType<OkObjectResult>(
                await Controller(order, new User { Id = 7 }).OrderDetails(order.Id)).Value);

        private OrderController Controller(Order order, User user)
        {
            var orders = new Mock<IRepository<Order>>();
            orders.Setup(x => x.Query()).Returns(new[] { order }.AsQueryable());
            var work = new Mock<IWorkContext>();
            work.Setup(x => x.GetCurrentUser()).ReturnsAsync(user);
            var products = new Mock<IRepository<VendorProduct>>();
            products.Setup(x => x.Query()).Returns(new List<VendorProduct>().AsQueryable());
            return new OrderController(orders.Object, work.Object, Mock.Of<IMediaService>(),
                Mock.Of<ICurrencyService>(), products.Object, _vendors.Object, _businesses.Object);
        }

        public void Dispose() => _db.Dispose();

        private sealed class VendorDb : DbContext
        {
            public VendorDb() : base(new DbContextOptionsBuilder<VendorDb>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options) { }

            protected override void OnModelCreating(ModelBuilder builder)
            {
                builder.Entity<Vendor>().Ignore(x => x.Users).Ignore(x => x.AssignedProducts);
                builder.Entity<BusinessInformation>();
            }
        }
    }
}
