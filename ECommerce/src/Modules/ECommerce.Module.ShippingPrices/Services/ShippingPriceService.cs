using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Shipping.Models;

namespace ECommerce.Module.ShippingPrices.Services
{
    public class ShippingPriceService : IShippingPriceService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRepositoryWithTypedId<ShippingProvider, string> _shippingProviderRepository;
        private readonly ILogger<ShippingPriceService> _logger;

        public ShippingPriceService(
            IServiceProvider serviceProvider,
            IRepositoryWithTypedId<ShippingProvider, string> shippingProviderRepository,
            ILogger<ShippingPriceService> logger)
        {
            _serviceProvider = serviceProvider;
            _shippingProviderRepository = shippingProviderRepository;
            _logger = logger;
        }

        public async Task<IList<ShippingPrice>> GetApplicableShippingPrices(GetShippingPriceRequest request)
        {
            var applicableShippingPrices = new List<ShippingPrice>();

            if (request?.ShippingAddress == null)
            {
                _logger.LogWarning("Shipping address is null in GetApplicableShippingPrices request");
                return applicableShippingPrices;
            }

            var providers = await _shippingProviderRepository.Query().ToListAsync();
            var shippingRateServices = _serviceProvider.GetServices<IShippingPriceServiceProvider>();

            foreach (var provider in providers)
            {
                if (!provider.IsEnabled)
                {
                    continue;
                }

                if (!provider.ToAllShippingEnabledCountries)
                {
                    if (provider.OnlyCountryIds == null || !provider.OnlyCountryIds.Contains(request.ShippingAddress.CountryId))
                    {
                        continue;
                    }
                }

                if (!provider.ToAllShippingEnabledStatesOrProvinces)
                {
                    if (provider.OnlyStateOrProvinceIds == null ||
                        !provider.OnlyStateOrProvinceIds.Contains(request.ShippingAddress.StateOrProvinceId))
                    {
                        continue;
                    }
                }

                var priceServiceType = Type.GetType(provider.ShippingPriceServiceTypeName);
                if (priceServiceType == null)
                {
                    _logger.LogWarning("Could not load type for shipping provider: {ProviderTypeName}", provider.ShippingPriceServiceTypeName);
                    continue;
                }

                var priceService = shippingRateServices.FirstOrDefault(x => x.GetType() == priceServiceType);
                if (priceService == null)
                {
                    _logger.LogWarning("No shipping price service provider found for type: {ProviderType}", priceServiceType.FullName);
                    continue;
                }

                try
                {
                    var response = await priceService.GetShippingPrices(request, provider);
                    if (response?.ApplicablePrices != null)
                    {
                        applicableShippingPrices.AddRange(response.ApplicablePrices);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting shipping prices for provider: {ProviderId}", provider.Id);
                }
            }

            return applicableShippingPrices;
        }
    }
}
