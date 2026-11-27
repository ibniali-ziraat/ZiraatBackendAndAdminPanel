using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Pricing.Areas.Pricing.Controllers;
using ECommerce.Module.Pricing.Areas.Pricing.ViewModels;
using ECommerce.Module.Pricing.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace ECommerce.Module.Pricing.Tests.Controllers
{
    public class CartRuleProductPersistenceTests : IDisposable
    {
        private readonly OfferDb _db = new();

        [Fact]
        public async Task CreateAndReadReturnsExactlySelectedProducts()
        {
            await SeedProducts();
            var form = Form(11, 22, 11);
            Assert.IsType<OkObjectResult>(await Controller().Post(form));
            var ruleId = await _db.Set<CartRule>().Select(x => x.Id).SingleAsync();
            _db.ChangeTracker.Clear();

            var saved = await Read(ruleId);
            Assert.Equal(new long[] { 11, 22 }, saved.Products.Select(x => x.Id).OrderBy(x => x));
            Assert.Equal("Selected A", saved.Products.Single(x => x.Id == 11).Name);
            Assert.Equal(10014, saved.VendorId);
            Assert.Equal(2, await _db.Set<CartRuleProduct>().CountAsync());
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"products\":null}")]
        [InlineData("{\"productIds\":[11]}")]
        [InlineData("{\"products\":[{\"productId\":11}]}")]
        [InlineData("{\"products\":[null]}")]
        [InlineData("{\"products\":[{\"id\":0}]}")]
        public async Task CreateRejectsMissingOrMalformedSelectionWithoutSaving(string json)
        {
            var form = JsonConvert.DeserializeObject<CartRuleForm>(json);
            form.Name = "Offer";
            form.RuleToApply = "by_percent";
            Assert.IsType<BadRequestObjectResult>(await Controller().Post(form));
            Assert.Empty(_db.Set<CartRule>());
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"products\":null}")]
        public async Task UpdateWithoutSelectionPreservesSavedProducts(string json)
        {
            var id = await SeedRule();
            var form = JsonConvert.DeserializeObject<CartRuleForm>(json);
            form.Name = "Updated offer";
            form.RuleToApply = "by_percent";
            form.VendorId = 10014;
            Assert.IsType<OkObjectResult>(await Controller().Put(id, form));
            _db.ChangeTracker.Clear();
            Assert.Equal(new long[] { 11, 22 }, (await Read(id)).Products.Select(x => x.Id).OrderBy(x => x));
        }

        [Fact]
        public async Task ExplicitSelectionReplacesSavedProducts()
        {
            var id = await SeedRule();
            Assert.IsType<OkObjectResult>(await Controller().Put(id, Form(22, 33, 33)));
            _db.ChangeTracker.Clear();
            Assert.Equal(new long[] { 22, 33 }, (await Read(id)).Products.Select(x => x.Id).OrderBy(x => x));
            Assert.Equal(2, await _db.Set<CartRuleProduct>().CountAsync());
        }

        [Fact]
        public async Task ExplicitEmptySelectionClearsSavedProducts()
        {
            var id = await SeedRule();
            Assert.IsType<OkObjectResult>(await Controller().Put(id, Form()));
            _db.ChangeTracker.Clear();
            Assert.Empty((await Read(id)).Products);
            Assert.Empty(_db.Set<CartRuleProduct>());
        }

        [Fact]
        public async Task InvalidUpdateLeavesSavedProductsUntouched()
        {
            var id = await SeedRule();
            Assert.IsType<BadRequestObjectResult>(await Controller().Put(id, Form(0)));
            _db.ChangeTracker.Clear();
            Assert.Equal(2, (await Read(id)).Products.Count);
        }

        [Fact]
        public async Task ExplicitEmptySelectionCanBeCreated()
        {
            Assert.IsType<OkObjectResult>(await Controller().Post(Form()));
            Assert.Empty(_db.Set<CartRuleProduct>());
        }

        private static CartRuleForm Form(params long[] ids) => new()
        {
            Name = "Season Offer",
            VendorId = 10014,
            RuleToApply = "by_percent",
            DiscountAmount = 10,
            Products = ids.Select(id => new CartRuleProductVm { Id = id }).ToList()
        };

        private async Task SeedProducts()
        {
            var products = new[]
            {
                new Product { Name = "Selected A", IsPublished = true },
                new Product { Name = "Selected B", IsPublished = false },
                new Product { Name = "Unselected", IsPublished = true }
            };
            for (var index = 0; index < products.Length; index++)
            {
                _db.Entry(products[index]).Property(x => x.Id).CurrentValue = (index + 1) * 11;
            }
            _db.AddRange(products);
            await _db.SaveChangesAsync();
        }

        private async Task<long> SeedRule()
        {
            await SeedProducts();
            Assert.IsType<OkObjectResult>(await Controller().Post(Form(11, 22)));
            var id = await _db.Set<CartRule>().Select(x => x.Id).SingleAsync();
            _db.ChangeTracker.Clear();
            return id;
        }

        private async Task<CartRuleForm> Read(long id) =>
            Assert.IsType<CartRuleForm>(Assert.IsType<JsonResult>(await Controller().Get(id)).Value);

        private CartRuleApiController Controller()
        {
            var repository = new Mock<IRepository<CartRule>>();
            repository.Setup(x => x.Query()).Returns(() => _db.Set<CartRule>());
            repository.Setup(x => x.Add(It.IsAny<CartRule>())).Callback<CartRule>(x => _db.Add(x));
            repository.Setup(x => x.SaveChangesAsync()).Returns(async () => { await _db.SaveChangesAsync(); });
            var workContext = new Mock<IWorkContext>();
            workContext.Setup(x => x.GetCurrentUser()).ReturnsAsync(new User { Id = 7, VendorId = 10014 });
            return new CartRuleApiController(repository.Object, workContext.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "vendor") }, "Test"))
                    }
                }
            };
        }

        public void Dispose() => _db.Dispose();

        private sealed class OfferDb : DbContext
        {
            public OfferDb() : base(new DbContextOptionsBuilder<OfferDb>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options) { }

            protected override void OnModelCreating(ModelBuilder builder)
            {
                var product = builder.Entity<Product>();
                foreach (var property in typeof(Product).GetProperties()
                    .Where(x => x.Name != nameof(Product.Id) && x.Name != nameof(Product.Name) && x.Name != nameof(Product.IsPublished)))
                {
                    product.Ignore(property.Name);
                }

                var rule = builder.Entity<CartRule>();
                rule.Ignore(x => x.Vendor);
                rule.Ignore(x => x.CustomerGroups);
                rule.Ignore(x => x.Categories);
                builder.Entity<Coupon>().HasOne(x => x.CartRule).WithMany(x => x.Coupons).HasForeignKey(x => x.CartRuleId);
                builder.Entity<CartRuleProduct>().HasKey(x => new { x.CartRuleId, x.ProductId });
                builder.Entity<CartRuleProduct>().HasOne(x => x.CartRule).WithMany(x => x.Products).HasForeignKey(x => x.CartRuleId);
                builder.Entity<CartRuleProduct>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            }
        }
    }
}
