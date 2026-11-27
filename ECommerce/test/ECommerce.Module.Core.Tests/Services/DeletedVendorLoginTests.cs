using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.Controllers;
using ECommerce.Module.Core.Areas.Core.ViewModels.Account;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ECommerce.Module.Core.Tests.Services
{
    public class DeletedVendorLoginTests : IDisposable
    {
        private readonly AccountDb _db = new();
        private readonly Mock<UserManager<User>> _users;
        private readonly UserAccessService _access;

        public DeletedVendorLoginTests()
        {
            _users = new Mock<UserManager<User>>(Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()), null, null, null, null, null, null, null);
            _users.Setup(x => x.Users).Returns(() => _db.Set<User>());
            _users.Setup(x => x.CheckPasswordAsync(It.IsAny<User>(), "valid-password")).ReturnsAsync(true);
            _users.Setup(x => x.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new List<string> { "customer" });
            _users.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).Returns<ClaimsPrincipal>(principal =>
            {
                var id = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier));
                return _db.Set<User>().SingleOrDefaultAsync(x => x.Id == id);
            });
            var vendors = new Mock<IRepository<Vendor>>();
            vendors.Setup(x => x.Query()).Returns(() => _db.Set<Vendor>());
            _access = new UserAccessService(_users.Object, vendors.Object);
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(false, false, false)]
        public async Task MobileLoginRejectsDeletedUsersAndDeletedOrMissingLinkedVendors(
            bool vendorDeleted, bool userDeleted, bool vendorExists)
        {
            var user = await Seed(vendorDeleted, userDeleted, vendorExists);
            var result = await Controller().Login(new LoginViewModel { MobileNumber = user.PhoneNumber, Password = "valid-password" });
            Assert.IsType<UnauthorizedObjectResult>(result);
            _users.Verify(x => x.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ActiveVendorCustomerRoleCanStillLogin()
        {
            var user = await Seed();
            Assert.IsType<OkObjectResult>(await Controller().Login(
                new LoginViewModel { MobileNumber = user.PhoneNumber, Password = "valid-password" }));
        }

        [Fact]
        public async Task UserWithoutVendorCanStillLogin()
        {
            var user = await Seed();
            user.VendorId = null;
            await _db.SaveChangesAsync();
            Assert.IsType<OkObjectResult>(await Controller().Login(
                new LoginViewModel { MobileNumber = user.PhoneNumber, Password = "valid-password" }));
        }

        [Fact]
        public async Task IncorrectPasswordStillFailsForActiveAccount()
        {
            var user = await Seed();
            Assert.IsType<UnauthorizedObjectResult>(await Controller().Login(
                new LoginViewModel { MobileNumber = user.PhoneNumber, Password = "wrong-password" }));
        }

        [Theory]
        [InlineData("Bearer", "customer")]
        [InlineData("Identity.Application", "vendor")]
        [InlineData("Bearer", "admin")]
        public async Task ExistingPrincipalBecomesInvalidAfterVendorDeletion(string scheme, string role)
        {
            var user = await Seed();
            var principal = Principal(user.Id, scheme, role);
            Assert.True(await _access.CanAccessAsync(principal));
            var vendor = await _db.Set<Vendor>().SingleAsync();
            vendor.IsDeleted = true;
            await _db.SaveChangesAsync();
            Assert.False(await _access.CanAccessAsync(principal));
            Assert.False(user.IsDeleted);
        }

        [Fact]
        public async Task ExistingPrincipalForMissingUserIsRejected()
        {
            Assert.False(await _access.CanAccessAsync(Principal(999, "Bearer", "customer")));
        }

        [Fact]
        public async Task AnonymousPrincipalIsRejectedWithoutUserLookup()
        {
            Assert.False(await _access.CanAccessAsync(new ClaimsPrincipal(new ClaimsIdentity())));
            _users.Verify(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
        }

        [Fact]
        public async Task SharedSignInCheckRejectsDeletedVendorForWebLoginToo()
        {
            var user = await Seed(vendorDeleted: true);
            Assert.False(await SignIn().CanSignInAsync(user));
        }

        [Fact]
        public async Task VendorClaimCannotOverrideStoredVendorAssociation()
        {
            var user = await Seed(vendorDeleted: true);
            var principal = Principal(user.Id, "Bearer", "customer");
            ((ClaimsIdentity)principal.Identity).AddClaim(new Claim("vendorId", "999"));
            Assert.False(await _access.CanAccessAsync(principal));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DeletedPhoneDuplicatesAreExcludedBeforeLogin(bool activeUserDeleted)
        {
            var user = await Seed(userDeleted: activeUserDeleted);
            _db.AddRange(
                new User { Id = 8, FullName = "Deleted account", UserName = "deleted-8", PhoneNumber = user.PhoneNumber, IsDeleted = true },
                new User { Id = 9, FullName = "Deleted account", UserName = "deleted-9", PhoneNumber = user.PhoneNumber, IsDeleted = true });
            await _db.SaveChangesAsync();

            var result = await Controller().Login(new LoginViewModel
            {
                MobileNumber = user.PhoneNumber, Password = "valid-password"
            });

            if (activeUserDeleted)
            {
                Assert.IsType<UnauthorizedObjectResult>(result);
                _users.Verify(x => x.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
            }
            else
            {
                Assert.IsType<OkObjectResult>(result);
                _users.Verify(x => x.CheckPasswordAsync(user, "valid-password"), Times.Once);
                _users.Verify(x => x.CheckPasswordAsync(It.Is<User>(u => u.IsDeleted), It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        public async Task DuplicateActivePhoneAccountsAreRejectedWithoutCheckingPasswords()
        {
            var user = await Seed();
            _db.Add(new User { Id = 8, FullName = "Other account", UserName = "other", PhoneNumber = user.PhoneNumber });
            await _db.SaveChangesAsync();

            Assert.IsType<UnauthorizedObjectResult>(await Controller().Login(
                new LoginViewModel { MobileNumber = user.PhoneNumber, Password = "valid-password" }));
            _users.Verify(x => x.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }

        private static ClaimsPrincipal Principal(long id, string scheme, string role) =>
            new(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, scheme));

        private async Task<User> Seed(bool vendorDeleted = false, bool userDeleted = false, bool vendorExists = true)
        {
            if (vendorExists)
            {
                var vendor = new Vendor { Name = "Vendor", Slug = "vendor", IsDeleted = vendorDeleted };
                _db.Entry(vendor).Property(x => x.Id).CurrentValue = 10014;
                _db.Add(vendor);
            }
            var user = new User
            {
                Id = 7, FullName = "Linked account", UserName = "linked", PhoneNumber = "9000000000",
                VendorId = 10014, IsDeleted = userDeleted
            };
            _db.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        private AccountSignInManager SignIn() => new(_users.Object,
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            Mock.Of<IUserClaimsPrincipalFactory<User>>(), Options.Create(new IdentityOptions()),
            NullLogger<SignInManager<User>>.Instance, Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<User>>(), _access);

        private AccountApiController Controller() => new(SignIn(), _users.Object,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Jwt:Key"] = "test-only-signing-key-not-used-in-production-0123456789"
            }).Build(), NullLogger<AccountController>.Instance, NullLoggerFactory.Instance);

        public void Dispose() => _db.Dispose();

        private sealed class AccountDb : DbContext
        {
            public AccountDb() : base(new DbContextOptionsBuilder<AccountDb>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options) { }

            protected override void OnModelCreating(ModelBuilder builder)
            {
                builder.Entity<Vendor>().Ignore(x => x.Users).Ignore(x => x.AssignedProducts);
                builder.Entity<User>().Ignore(x => x.UserAddresses).Ignore(x => x.DefaultShippingAddress)
                    .Ignore(x => x.DefaultBillingAddress).Ignore(x => x.Roles).Ignore(x => x.CustomerGroups);
            }
        }
    }
}
