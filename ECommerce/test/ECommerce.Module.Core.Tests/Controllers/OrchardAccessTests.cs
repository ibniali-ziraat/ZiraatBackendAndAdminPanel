using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.Controllers;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ECommerce.Module.Core.Tests.Controllers
{
    public class OrchardAccessTests : IClassFixture<OrchardAccessTests.Host>
    {
        private readonly Host _host;
        public OrchardAccessTests(Host host) => _host = host;

        public static IEnumerable<object[]> ProtectedRoutes()
        {
            foreach (var path in new[] { "api/core/orchardProfile/2", "api/core/orchardProfile/byOrchardId/other",
                "api/core/orchardProfile/user/8" })
                yield return new object[] { "GET", path };
            foreach (var verb in new[] { "PUT", "DELETE" })
                yield return new object[] { verb, "api/core/orchardProfile/2" };
            foreach (var section in new[] { "soil-water", "crop-nutrition", "plant-health", "infrastructure",
                "production", "input-cost", "optional-fields" })
                foreach (var verb in new[] { "GET", "POST", "PATCH", "DELETE" })
                    yield return new object[] { verb, "api/core/orchard/2/" + section };
            foreach (var verb in new[] { "GET", "POST" })
                yield return new object[] { verb, "api/core/orchard/2/varieties" };
            yield return new object[] { "POST", "api/core/orchard/2/varieties/bulk" };
            foreach (var verb in new[] { "GET", "PUT", "DELETE" })
                yield return new object[] { verb, "api/core/orchard/2/varieties/99" };
        }

        [Theory]
        [MemberData(nameof(ProtectedRoutes))]
        public async Task HttpPipelineRejectsCrossOwnerRequestsBeforeActions(string method, string path)
        {
            using var response = await _host.Send(method, path, "7", "customer");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(0, _host.MutationCalls());
        }

        [Theory]
        [InlineData("api/core/orchardProfile/1")]
        [InlineData("api/core/orchardProfile/byOrchardId/owned")]
        [InlineData("api/core/orchard/1/soil-water")]
        public async Task AnonymousPrivateReadsReturn401(string path)
        {
            using var response = await _host.Send("GET", path);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("7", "customer", 200)]
        [InlineData("9", "admin", 200)]
        [InlineData("9", "vendor", 404)]
        [InlineData("7", "vendor", 200)]
        [InlineData("invalid", "admin", 401)]
        public async Task OwnerAndAdminReadButVendorRoleDoesNotGrantAccess(string user, string role, int status)
        {
            using var response = await _host.Send("GET", "api/core/orchardProfile/1", user, role);
            Assert.Equal(status, (int)response.StatusCode);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        public async Task ChildMustBelongToAuthorizedParent(string method)
        {
            using var response = await _host.Send(method, "api/core/orchard/1/varieties/99", "7", "customer");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(0, _host.MutationCalls());
        }

        [Theory]
        [InlineData("7", "customer")]
        [InlineData("9", "admin")]
        public async Task AmbiguousExternalReferenceIsDenied(string user, string role)
        {
            using var response = await _host.Send("GET", "api/core/orchardProfile/byOrchardId/duplicate", user, role);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData(false, "orchardProfileId", "1", "7", "customer", true)]
        [InlineData(false, "orchardProfileId", "2", "7", "customer", false)]
        [InlineData(false, "orchardProfileId", "2", "7", "admin", true)]
        [InlineData(false, "orchardProfileId", "999", "7", "admin", false)]
        [InlineData(false, "orchardProfileId", "-1", "7", "admin", false)]
        [InlineData(false, "orchardProfileId", "bad", "7", "admin", false)]
        [InlineData(false, "id", "1", "7", "customer", false)]
        [InlineData(true, "userId", "7", "7", "customer", true)]
        [InlineData(true, "userId", "8", "7", "customer", false)]
        [InlineData(true, "userId", "8", "7", "admin", true)]
        [InlineData(true, "orchardId", "owned", "7", "customer", true)]
        [InlineData(true, "orchardId", "other", "7", "customer", false)]
        public async Task SharedFilterHasFailClosedRouteRules(bool profileRoutes, string key, string value,
            string user, string role, bool allowed)
        {
            var http = new DefaultHttpContext { User = Principal(user, role) };
            http.Request.Method = "GET";
            var routes = new RouteData();
            routes.Values[key] = value;
            var context = new AuthorizationFilterContext(
                new ActionContext(http, routes, new ControllerActionDescriptor()), new List<IFilterMetadata>());
            await new OrchardAccessFilter(_host.Repository, profileRoutes).OnAuthorizationAsync(context);
            if (allowed) Assert.Null(context.Result);
            else Assert.IsType<NotFoundResult>(context.Result);
        }

        [Fact]
        public void EveryOrchardControllerRequiresAuthenticationAndSharedFilter()
        {
            Assert.Equal(9, Host.Controllers.Length);
            foreach (var type in Host.Controllers)
            {
                Assert.NotNull(type.GetCustomAttribute<OrchardAccessAttribute>());
                Assert.NotEmpty(type.GetCustomAttributes<AuthorizeAttribute>());
                Assert.Null(type.GetCustomAttribute<AllowAnonymousAttribute>());
                Assert.DoesNotContain(type.GetMethods(), m => m.GetCustomAttribute<AllowAnonymousAttribute>() != null);
            }
        }

        private static ClaimsPrincipal Principal(string user, string role) => new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user), new Claim(ClaimTypes.Role, role)
        }, "test"));

        public sealed class Host : IAsyncLifetime
        {
            public static readonly Type[] Controllers =
            {
                typeof(OrchardProfileController), typeof(OrchardSoilAndWaterController),
                typeof(OrchardCropNutritionController), typeof(OrchardPlantHealthController),
                typeof(OrchardInfrastructureController), typeof(OrchardProductionController),
                typeof(OrchardInputCostController), typeof(OrchardOptionalFieldsController), typeof(OrchardVarietyController)
            };
            private readonly OrchardDb _db = new();
            private readonly List<Mock> _services = new();
            private WebApplication _app;
            private HttpClient _client;
            public IRepository<OrchardProfile> Repository { get; private set; }

            public async Task InitializeAsync()
            {
                var owned = Seed(1, 7, "owned");
                Seed(2, 8, "other");
                Seed(3, 7, "duplicate");
                Seed(4, 8, "duplicate");
                await _db.SaveChangesAsync();
                var repo = new Mock<IRepository<OrchardProfile>>();
                repo.Setup(x => x.Query()).Returns(() => _db.Set<OrchardProfile>());
                Repository = repo.Object;
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
                builder.Logging.ClearProviders();
                builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
                builder.Services.AddSingleton(Repository);
                builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("test", null);
                builder.Services.AddAuthorization();
                builder.Services.AddControllers().AddApplicationPart(typeof(OrchardProfileController).Assembly)
                    .ConfigureApplicationPartManager(m => m.FeatureProviders.Add(new OnlyOrchardControllers()));
                foreach (var parameter in Controllers.SelectMany(x => x.GetConstructors().Single().GetParameters())
                    .Select(x => x.ParameterType).Distinct().Where(x => x.IsInterface &&
                        !(x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ILogger<>))))
                {
                    var mock = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(parameter));
                    _services.Add(mock);
                    builder.Services.AddSingleton(parameter, mock.Object);
                }
                var profiles = (Mock<IOrchardProfileService>)_services.Single(x => x is Mock<IOrchardProfileService>);
                profiles.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(owned);
                var varieties = (Mock<IOrchardVarietyService>)_services.Single(x => x is Mock<IOrchardVarietyService>);
                varieties.Setup(x => x.GetByIdAsync(99)).ReturnsAsync(new OrchardVariety { OrchardProfileId = 2 });
                _app = builder.Build();
                _app.UseAuthentication();
                _app.UseAuthorization();
                _app.MapControllers();
                await _app.StartAsync();
                var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.Single();
                _client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(20) };
            }

            private OrchardProfile Seed(long id, long owner, string reference)
            {
                var row = new OrchardProfile { UserId = owner, OrchardId = reference, OrchardName = reference };
                _db.Entry(row).Property(x => x.Id).CurrentValue = id;
                _db.Add(row);
                return row;
            }

            public Task<HttpResponseMessage> Send(string method, string path, string user = null, string role = null)
            {
                var request = new HttpRequestMessage(new HttpMethod(method), path);
                if (user != null)
                {
                    request.Headers.Add("X-Test-User", user);
                    request.Headers.Add("X-Test-Role", role);
                }
                if (method != "GET") request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
                return _client.SendAsync(request);
            }

            public int MutationCalls() => _services.Sum(m => m.Invocations.Count(x =>
                x.Method.Name.StartsWith("Create") || x.Method.Name.StartsWith("Update") ||
                x.Method.Name.StartsWith("Delete")));

            public async Task DisposeAsync()
            {
                _client?.Dispose();
                if (_app != null) await _app.DisposeAsync();
                await _db.DisposeAsync();
            }
        }

        public sealed class TestAuthentication : AuthenticationHandler<AuthenticationSchemeOptions>
        {
            public TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
                UrlEncoder encoder) : base(options, logger, encoder) { }
            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var user = Request.Headers["X-Test-User"].ToString();
                return Task.FromResult(string.IsNullOrEmpty(user) ? AuthenticateResult.NoResult() :
                    AuthenticateResult.Success(new AuthenticationTicket(
                        Principal(user, Request.Headers["X-Test-Role"].ToString()), Scheme.Name)));
            }
        }

        private sealed class OnlyOrchardControllers : IApplicationFeatureProvider<ControllerFeature>
        {
            public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
            {
                foreach (var type in feature.Controllers.Where(x => !Host.Controllers.Contains(x.AsType())).ToList())
                    feature.Controllers.Remove(type);
            }
        }

        private sealed class OrchardDb : DbContext
        {
            public OrchardDb() : base(new DbContextOptionsBuilder<OrchardDb>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options) { }
            protected override void OnModelCreating(ModelBuilder builder)
            {
                var row = builder.Entity<OrchardProfile>();
                foreach (var property in typeof(OrchardProfile).GetProperties()
                    .Where(x => !x.PropertyType.IsValueType && x.PropertyType != typeof(string)))
                    row.Ignore(property.Name);
            }
        }
    }
}
