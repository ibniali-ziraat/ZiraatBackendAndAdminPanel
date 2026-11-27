using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Shipping.Models;
using ECommerce.Module.ShippingFree.Models;
using ECommerce.Module.ShippingFree.Services;
using ECommerce.Module.ShippingPrices.Services;
using Xunit;

namespace ECommerce.Module.ShippingFree.Tests.Services
{
    public class FreeShippingServiceProviderTests
    {
        [Fact]
        public async Task GetShippingPrices_ShouldReturnFreeShipping()
        {
            // Arrange
            var currencyServiceMock = new Mock<ICurrencyService>();
            var freeShippingProvider = new ShippingProvider
            {
                AdditionalSettings = JsonConvert.SerializeObject(new FreeShippingSetting
                {
                    MinimumOrderAmount = 50 // Adjust the value based on your requirement
                })
            };
            var freeShippingServiceProvider = new FreeShippingServiceProvider(currencyServiceMock.Object);
            var request = new GetShippingPriceRequest
            {
                OrderAmount = 60 // Exceeds the MinimumOrderAmount
            };

            // Act
            var response = await freeShippingServiceProvider.GetShippingPrices(request, freeShippingProvider);

            // Assert
            Assert.True(response.IsSuccess);
            Assert.Single(response.ApplicablePrices);

            var shippingPrice = response.ApplicablePrices[0];
            Assert.Equal("Free", shippingPrice.Name);
            Assert.Equal(0, shippingPrice.Price);
        }

        [Fact]
        public async Task GetShippingPrices_ShouldNotReturnFreeShipping()
        {
            // Arrange
            var currencyServiceMock = new Mock<ICurrencyService>();
            var freeShippingProvider = new ShippingProvider
            {
                AdditionalSettings = JsonConvert.SerializeObject(new FreeShippingSetting
                {
                    MinimumOrderAmount = 50 // Adjust the value based on your requirement
                })
            };
            var freeShippingServiceProvider = new FreeShippingServiceProvider(currencyServiceMock.Object);
            var request = new GetShippingPriceRequest
            {
                OrderAmount = 40 // Below the MinimumOrderAmount
            };

            // Act
            var response = await freeShippingServiceProvider.GetShippingPrices(request, freeShippingProvider);

            // Assert
            Assert.True(response.IsSuccess);
            Assert.Empty(response.ApplicablePrices);
        }
    }
}
