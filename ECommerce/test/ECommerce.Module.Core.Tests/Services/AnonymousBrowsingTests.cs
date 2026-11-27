using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Events;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Localization;
using ECommerce.Module.Localization.Areas.Localization.Controllers;
using ECommerce.Module.ShoppingCart.Areas.ShoppingCart.Controllers;
using ECommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels;
using ECommerce.Module.ShoppingCart.Services;
using ECommerce.Module.ProductComparison.Areas.ProductComparison.Controllers;
using ECommerce.Module.ProductComparison.Areas.ProductComparison.ViewModels;
using ECommerce.Module.ProductComparison.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace ECommerce.Module.Core.Tests.Services
{
    public class AnonymousBrowsingTests
    {
        private static Mock<UserManager<User>> Manager()
        {
            var manager = new Mock<UserManager<User>>(MockBehavior.Strict,
                Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
            manager.SetupAllProperties();
            _ = manager.Object;
            manager.Invocations.Clear();
            return manager;
        }

        private static IWorkContext Anonymous()
        {
            var mock = new Mock<IWorkContext>();
            mock.Setup(x => x.GetCurrentUser()).ReturnsAsync((User)null);
            return mock.Object;
        }

        [Theory]
        [InlineData("/admin", null)]
        [InlineData("/admin/login", "invalid-guid")]
        [InlineData("/vendor", "39a53cdd-c80a-4c4f-a380-2b0265ad4a51")]
        [InlineData("/api/products", null)]
        public async Task AnonymousBrowsingNeverLoadsOrCreatesUsersOrSetsGuestCookies(string path, string cookie)
        {
            var manager = Manager();
            var http = new DefaultHttpContext();
            http.Request.Path = path;
            if (cookie != null) http.Request.Headers.Cookie = "SimplUserGuid=" + cookie;
            var context = new WorkContext(manager.Object, new HttpContextAccessor { HttpContext = http });
            for (var request = 0; request < 5; request++) Assert.Null(await context.GetCurrentUser());
            Assert.False(http.Response.Headers.ContainsKey("Set-Cookie"));
            manager.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task AnonymousClaimsAloneDoNotResolveAnAccount()
        {
            var manager = Manager();
            var http = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "10") }))
            };
            var context = new WorkContext(manager.Object, new HttpContextAccessor { HttpContext = http });
            Assert.Null(await context.GetCurrentUser());
            manager.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task LookupWithoutHttpRequestDoesNotCreateUser()
        {
            var manager = Manager();
            Assert.Null(await new WorkContext(manager.Object, new HttpContextAccessor()).GetCurrentUser());
            manager.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task AuthenticatedUserLookupRemainsCachedAndDoesNotWrite()
        {
            var manager = Manager();
            var http = new DefaultHttpContext { User = Principal("10") };
            var user = new User { Id = 10, FullName = "Registered user" };
            manager.Setup(x => x.GetUserAsync(http.User)).ReturnsAsync(user);
            var context = new WorkContext(manager.Object, new HttpContextAccessor { HttpContext = http });
            Assert.Same(user, await context.GetCurrentUser());
            Assert.Same(user, await context.GetCurrentUser());
            manager.Verify(x => x.GetUserAsync(http.User), Times.Once);
            manager.VerifyNoOtherCalls();
            Assert.False(http.Response.Headers.ContainsKey("Set-Cookie"));
        }

        [Fact]
        public async Task UnresolvedAuthenticatedIdentityDoesNotFallBackToGuest()
        {
            var manager = Manager();
            var http = new DefaultHttpContext { User = Principal("999") };
            http.Request.Headers.Cookie = "SimplUserGuid=39a53cdd-c80a-4c4f-a380-2b0265ad4a51";
            manager.Setup(x => x.GetUserAsync(http.User)).ReturnsAsync((User)null);
            Assert.Null(await new WorkContext(manager.Object, new HttpContextAccessor { HttpContext = http }).GetCurrentUser());
            manager.Verify(x => x.GetUserAsync(http.User), Times.Once);
            manager.VerifyNoOtherCalls();
            Assert.False(http.Response.Headers.ContainsKey("Set-Cookie"));
        }

        [Fact]
        public async Task AnonymousLookupDoesNotPreventLaterAuthenticationAndSignOutClearsAccess()
        {
            var manager = Manager();
            var http = new DefaultHttpContext();
            var context = new WorkContext(manager.Object, new HttpContextAccessor { HttpContext = http });
            Assert.Null(await context.GetCurrentUser());
            var first = new User { Id = 10 };
            var firstPrincipal = Principal("10");
            manager.Setup(x => x.GetUserAsync(firstPrincipal)).ReturnsAsync(first);
            http.User = firstPrincipal;
            Assert.Same(first, await context.GetCurrentUser());
            var second = new User { Id = 20 };
            var secondPrincipal = Principal("20");
            manager.Setup(x => x.GetUserAsync(secondPrincipal)).ReturnsAsync(second);
            http.User = secondPrincipal;
            Assert.Same(second, await context.GetCurrentUser());
            http.User = new ClaimsPrincipal(new ClaimsIdentity());
            Assert.Null(await context.GetCurrentUser());
        }

        [Fact]
        public async Task AnonymousSettingsUseDefaultsWithoutLoadingDatabaseUsers()
        {
            var manager = Manager();
            var definitions = new SettingDefinitionProvider().AddOrUpdate(new SettingDefinition("test-setting", "default-value"));
            var service = new SettingService(manager.Object, definitions, Anonymous());
            Assert.Equal("default-value", await service.GetSettingValueAsync("test-setting"));
            Assert.Null(await service.GetSettingValueAsync("unknown"));
            Assert.Equal("default-value", (await service.GetAllSettingsAsync())["test-setting"]);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateSettingAsync("test-setting", "new-value"));
            manager.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task AnonymousCultureFallsThroughToCookieProvider()
        {
            using var services = new ServiceCollection().AddSingleton(Anonymous()).BuildServiceProvider();
            var http = new DefaultHttpContext { RequestServices = services };
            Assert.Null(await new EfRequestCultureProvider().DetermineProviderCultureResult(http));
            http.Request.Headers.Cookie = CookieRequestCultureProvider.DefaultCookieName + "=" +
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("en-US"));
            var result = await new CookieRequestCultureProvider().DetermineProviderCultureResult(http);
            Assert.Equal("en-US", result.UICultures.Single().Value);
        }

        [Fact]
        public async Task RegisteredUserCultureStillTakesPrecedence()
        {
            var work = new Mock<IWorkContext>();
            work.Setup(x => x.GetCurrentUser()).ReturnsAsync(new User { Culture = "en-US" });
            using var services = new ServiceCollection().AddSingleton(work.Object).BuildServiceProvider();
            var result = await new EfRequestCultureProvider().DetermineProviderCultureResult(
                new DefaultHttpContext { RequestServices = services });
            Assert.Equal("en-US", result.UICultures.Single().Value);
        }

        [Fact]
        public async Task AnonymousLanguageChangesOnlyWriteCultureCookie()
        {
            var repository = new Mock<IRepositoryWithTypedId<User, long>>(MockBehavior.Strict);
            var controller = new LocalizationController(repository.Object, Anonymous())
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
            Assert.IsType<LocalRedirectResult>(await controller.SetLanguage("en-US", "/"));
            Assert.Contains(CookieRequestCultureProvider.DefaultCookieName + "=", controller.Response.Headers.SetCookie.ToString());
            Assert.DoesNotContain("SimplUserGuid", controller.Response.Headers.SetCookie.ToString());
            repository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task RegisteredLanguageChangesStillPersist()
        {
            var user = new User { Id = 10, Culture = "en-GB" };
            var work = new Mock<IWorkContext>();
            work.Setup(x => x.GetCurrentUser()).ReturnsAsync(user);
            var repository = new Mock<IRepositoryWithTypedId<User, long>>();
            var controller = new LocalizationController(repository.Object, work.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
            await controller.SetLanguage("en-US", "/");
            Assert.Equal("en-US", user.Culture);
            repository.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task SignInHandlersDoNotRequireAGuestAccount()
        {
            var work = Anonymous();
            var carts = new Mock<ICartService>(MockBehavior.Strict);
            var comparisons = new Mock<IComparingProductService>(MockBehavior.Strict);
            var users = new Mock<IRepository<User>>(MockBehavior.Strict);
            var signedIn = new UserSignedIn { UserId = 10 };
            await new ECommerce.Module.ShoppingCart.Events.UserSignedInHandler(work, carts.Object).Handle(signedIn, CancellationToken.None);
            await new ECommerce.Module.ProductComparison.Events.UserSignedInHandler(work, comparisons.Object).Handle(signedIn, CancellationToken.None);
            await new ECommerce.Module.Localization.Events.UserSignedInHandler(work, users.Object).Handle(signedIn, CancellationToken.None);
            carts.VerifyNoOtherCalls();
            comparisons.VerifyNoOtherCalls();
            users.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task AnonymousProductViewsDoNotWriteUserLinkedHistory()
        {
            var history = new Mock<IRepository<ECommerce.Module.ProductRecentlyViewed.Models.RecentlyViewedProduct>>(MockBehavior.Strict);
            var activities = new Mock<IRepository<ECommerce.Module.ActivityLog.Models.Activity>>(MockBehavior.Strict);
            var viewed = new EntityViewed { EntityId = 123, EntityTypeId = "Product" };
            await new ECommerce.Module.ProductRecentlyViewed.Events.EntityViewedHandler(history.Object, Anonymous()).Handle(viewed, CancellationToken.None);
            await new ECommerce.Module.ActivityLog.Events.EntityViewedHandler(activities.Object, Anonymous()).Handle(viewed, CancellationToken.None);
            history.VerifyNoOtherCalls();
            activities.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task AnonymousCartIsEmptyAndMutationsRequireSignIn()
        {
            var carts = new Mock<ICartService>(MockBehavior.Strict);
            var factory = new Mock<IStringLocalizerFactory>();
            factory.Setup(x => x.Create(null)).Returns(Mock.Of<IStringLocalizer>());
            var controller = new CartController(null, carts.Object, null, Anonymous(), Mock.Of<ICurrencyService>(), factory.Object, null);
            var result = Assert.IsType<JsonResult>(await controller.List());
            Assert.Empty(Assert.IsType<CartVm>(result.Value).Items);
            Assert.IsType<UnauthorizedResult>(await controller.AddToCart(new AddToCartModel()));
            Assert.IsType<UnauthorizedResult>(await controller.AddToCartResult(1));
            Assert.IsType<UnauthorizedResult>(await controller.UpdateQuantity(new CartQuantityUpdate { Quantity = 1 }));
            Assert.IsType<UnauthorizedResult>(await controller.ApplyCoupon(new ApplyCouponForm()));
            Assert.IsType<UnauthorizedResult>(await controller.Remove(1));
            carts.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task AnonymousComparisonsAreEmptyAndMutationsRequireSignIn()
        {
            var controller = new ComparingProductController(null, null, null, null, null, null, Anonymous());
            var result = Assert.IsType<OkObjectResult>(await controller.Index());
            var model = Assert.IsType<ProductComparisonVm>(result.Value);
            Assert.Empty(model.Products);
            Assert.Empty(model.Attributes);
            Assert.IsType<UnauthorizedResult>(await controller.AddToComparison(new AddToComparisonModel()));
            Assert.IsType<UnauthorizedResult>(await controller.Remove(1));
        }

        private static ClaimsPrincipal Principal(string id) => new(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id) }, "test"));
    }
}
