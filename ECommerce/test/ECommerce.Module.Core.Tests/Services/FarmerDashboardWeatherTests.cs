using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Module.Core.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ECommerce.Module.Core.Tests.Services
{
    public class FarmerDashboardWeatherTests
    {
        [Fact]
        public async Task CurrentWeatherRequestsExplicitUnitsAndKeepsMissingFeelsLikeNull()
        {
            using var handler = new WeatherHandler();
            using var client = new HttpClient(handler);
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Weather:Meteoblue:ApiKey"] = "unit-test",
                ["Weather:Meteoblue:ForecastBaseUrl"] = "https://example.test/packages/"
            }).Build();
            var service = new WeatherService(factory.Object, config);
            var result = await service.GetCurrentSummaryAsync(34, 74, "Test orchard");
            var query = QueryHelpers.ParseQuery(handler.RequestUri.Query);
            Assert.Equal("km/h", query["windSpeedUnit"].ToString());
            Assert.Equal("C", query["temperatureUnit"].ToString());
            Assert.Equal(18, result.WindKph);
            Assert.Equal(20, result.TemperatureC);
            Assert.Null(result.FeelsLikeC);
        }

        private sealed class WeatherHandler : HttpMessageHandler
        {
            public Uri RequestUri { get; private set; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestUri = request.RequestUri;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"data_current\":{\"temperature\":20,\"windspeed\":18,\"relativehumidity\":60}}")
                });
            }
        }
    }
}
