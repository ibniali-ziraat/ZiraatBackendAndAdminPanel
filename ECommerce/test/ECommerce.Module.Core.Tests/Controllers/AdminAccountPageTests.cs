using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.Controllers;
using ECommerce.Module.Core.Areas.Core.ViewModels.Manage;
using ECommerce.Module.Core.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ECommerce.Module.Core.Tests.Controllers
{
    public class AdminAccountPageTests
    {
        [Fact]
        public async Task AdminBrowserAccountLinkRedirectsToAdminScreen()
        {
            var (controller, manager) = Create("text/html", "admin");
            var result = Assert.IsType<LocalRedirectResult>(await controller.Index());
            Assert.Equal("~/admin#!/account", result.Url);
            manager.Verify(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
        }

        [Theory]
        [InlineData("application/json", "admin")]
        [InlineData("*/*", "admin")]
        [InlineData("text/html", "customer")]
        public async Task ExistingApiResponsesRemainJson(string accept, string role)
        {
            var (controller, _) = Create(accept, role);
            var result = Assert.IsType<OkObjectResult>(await controller.Index());
            var model = (IndexViewModel)result.Value.GetType().GetProperty("model").GetValue(result.Value);
            Assert.Equal("Actual user name", model.FullName);
            Assert.Equal("admin@example.test", model.Email);
        }

        [Fact]
        public async Task MissingAccountReturnsUnauthorized()
        {
            var (controller, manager) = Create("application/json", "admin");
            manager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((User)null);
            Assert.IsType<UnauthorizedResult>(await controller.Index());
        }

        [Fact]
        public async Task ProfileUpdateReportsIdentityFailure()
        {
            var (controller, manager) = Create("application/json", "admin");
            manager.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(
                IdentityResult.Failed(new IdentityError { Description = "Update rejected." }));
            Assert.IsType<BadRequestObjectResult>(await controller.UserInfo(
                new UserInfoVm { FullName = "Changed", Email = "admin@example.test" }));
        }

        private static (ManageController, Mock<UserManager<User>>) Create(string accept, string role)
        {
            var manager = new Mock<UserManager<User>>(Mock.Of<IUserStore<User>>(),
                null, null, null, null, null, null, null, null);
            manager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(
                new User { Id = 7, FullName = "Actual user name", Email = "admin@example.test" });
            manager.Setup(x => x.HasPasswordAsync(It.IsAny<User>())).ReturnsAsync(true);
            manager.Setup(x => x.GetLoginsAsync(It.IsAny<User>())).ReturnsAsync(new List<UserLoginInfo>());
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "Test"))
            };
            context.Request.Headers.Accept = accept;
            var signIn = new Mock<SignInManager<User>>(manager.Object,
                new HttpContextAccessor { HttpContext = context }, Mock.Of<IUserClaimsPrincipalFactory<User>>(),
                Options.Create(new IdentityOptions()), NullLogger<SignInManager<User>>.Instance,
                Mock.Of<IAuthenticationSchemeProvider>(), Mock.Of<IUserConfirmation<User>>());
            signIn.Setup(x => x.IsTwoFactorClientRememberedAsync(It.IsAny<User>())).ReturnsAsync(false);
            var controller = new ManageController(manager.Object, signIn.Object, null, null,
                NullLoggerFactory.Instance, null, null)
            {
                ControllerContext = new ControllerContext { HttpContext = context }
            };
            return (controller, manager);
        }
    }
}
