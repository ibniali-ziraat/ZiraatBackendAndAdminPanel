using System.Threading.Tasks;
using Newtonsoft.Json;
using ECommerce.Module.ShippingPrices.Services;
using ECommerce.Module.ShippingFree.Models;
using ECommerce.Module.Shipping.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.ShippingFree.Services
{
    public class FreeShippingServiceProvider : IShippingPriceServiceProvider
    {
        private readonly ICurrencyService _currencyService;

        public FreeShippingServiceProvider(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public Task<GetShippingPriceResponse> GetShippingPrices(GetShippingPriceRequest request, ShippingProvider provider)
        {
            var response = new GetShippingPriceResponse { IsSuccess = true };

            var freeShippingSetting = JsonConvert.DeserializeObject<FreeShippingSetting>(provider.AdditionalSettings);

            if (request.OrderAmount < freeShippingSetting.MinimumOrderAmount)
            {
                return Task.FromResult(response);
            }

            response.ApplicablePrices.Add(new ShippingPrice(_currencyService)
            {
                Name = "Free",
                Price = 0
            });

            return Task.FromResult(response);
        }
    }
}
