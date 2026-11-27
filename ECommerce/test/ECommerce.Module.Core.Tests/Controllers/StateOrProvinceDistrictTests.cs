using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Models;
using ECommerce.Module.Core.Areas.Core.Controllers;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ECommerce.Module.Core.Tests.Controllers
{
    public class StateOrProvinceDistrictTests : IDisposable
    {
        private readonly LocationDb _db = new();

        [Fact]
        public async Task CreatesDistrictForRouteStateAndReturnsExistingDistrictReadRoute()
        {
            _db.Add(new StateOrProvince(10) { Name = "State" });
            await _db.SaveChangesAsync();
            var result = Assert.IsType<CreatedAtActionResult>(await Controller().AddDistrict(10,
                new DistrictForm { Name = "  Srinagar  ", Type = " District ", Location = " Kashmir " }));
            var district = Assert.Single(_db.Set<District>());
            Assert.Equal(10, district.StateOrProvinceId);
            Assert.Equal("Srinagar", district.Name);
            Assert.Equal("District", district.Type);
            Assert.Equal("Kashmir", district.Location);
            Assert.Equal("DistrictApi", result.ControllerName);
            Assert.Equal(nameof(DistrictApiController.GetById), result.ActionName);
            Assert.Equal(district.Id, result.RouteValues["id"]);
            var read = await new DistrictApiController(Repo<District>()).GetById(district.Id);
            Assert.IsType<OkObjectResult>(read);
        }

        [Fact]
        public async Task MissingStateReturnsNotFoundWithoutSaving()
        {
            var result = await Controller().AddDistrict(999, new DistrictForm { Name = "Srinagar" });
            Assert.IsType<NotFoundObjectResult>(result);
            Assert.Empty(_db.Set<District>());
        }

        [Fact]
        public async Task DuplicateNameInSameStateReturnsConflict()
        {
            _db.Add(new StateOrProvince(10) { Name = "State" });
            _db.Add(new District(20) { StateOrProvinceId = 10, Name = " Srinagar " });
            await _db.SaveChangesAsync();
            var result = await Controller().AddDistrict(10, new DistrictForm { Name = "srinagar" });
            Assert.IsType<ConflictObjectResult>(result);
            Assert.Single(_db.Set<District>());
        }

        [Fact]
        public async Task SameNameCanExistInDifferentStates()
        {
            _db.Add(new StateOrProvince(10) { Name = "First state" });
            _db.Add(new StateOrProvince(11) { Name = "Second state" });
            _db.Add(new District(20) { StateOrProvinceId = 10, Name = "Central" });
            await _db.SaveChangesAsync();
            Assert.IsType<CreatedAtActionResult>(await Controller().AddDistrict(11, new DistrictForm { Name = "Central" }));
            Assert.Equal(2, _db.Set<District>().Count());
        }

        [Fact]
        public async Task NullBodyReturnsBadRequest()
        {
            Assert.IsType<BadRequestObjectResult>(await Controller().AddDistrict(10, null));
            Assert.Empty(_db.Set<District>());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task MissingNameFailsModelValidation(string name)
        {
            await AssertInvalid(new DistrictForm { Name = name });
        }

        [Fact]
        public async Task OversizedNameAndTypeFailModelValidation()
        {
            await AssertInvalid(new DistrictForm { Name = new string('a', 451) });
            await AssertInvalid(new DistrictForm { Name = "District", Type = new string('a', 451) });
        }

        [Fact]
        public void AddingDistrictRequiresAdmin()
        {
            var method = typeof(StateOrProvinceApiController).GetMethod(nameof(StateOrProvinceApiController.AddDistrict));
            var authorization = (AuthorizeAttribute)Attribute.GetCustomAttribute(method, typeof(AuthorizeAttribute));
            Assert.Equal("admin", authorization.Roles);
        }

        private async Task AssertInvalid(DistrictForm model)
        {
            var controller = Controller();
            var errors = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), errors, true));
            foreach (var error in errors)
                controller.ModelState.AddModelError(error.MemberNames.FirstOrDefault() ?? "", error.ErrorMessage);
            Assert.IsType<BadRequestObjectResult>(await controller.AddDistrict(10, model));
            Assert.Empty(_db.Set<District>());
        }

        private StateOrProvinceApiController Controller() =>
            new(Repo<StateOrProvince>(), Mock.Of<IRepositoryWithTypedId<Country, string>>(), Repo<District>());

        private IRepository<T> Repo<T>() where T : class, IEntityWithTypedId<long>
        {
            var mock = new Mock<IRepository<T>>();
            mock.Setup(x => x.Query()).Returns(() => _db.Set<T>());
            mock.Setup(x => x.Add(It.IsAny<T>())).Callback<T>(x => _db.Add(x));
            mock.Setup(x => x.SaveChangesAsync()).Returns(async () => { await _db.SaveChangesAsync(); });
            return mock.Object;
        }

        public void Dispose() => _db.Dispose();

        private sealed class LocationDb : DbContext
        {
            public LocationDb() : base(new DbContextOptionsBuilder<LocationDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options) { }
            protected override void OnModelCreating(ModelBuilder builder)
            {
                builder.Entity<StateOrProvince>().Ignore(x => x.Country);
                builder.Entity<District>();
            }
        }
    }
}
