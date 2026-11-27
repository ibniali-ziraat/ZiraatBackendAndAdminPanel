using System;
using ECommerce.Module.Catalog.Areas.Catalog.ViewModels;
using ECommerce.Module.Catalog.Models;

namespace ECommerce.Module.Catalog.Services
{
    public interface IProductPricingService
    {
        CalculatedProductPrice CalculateProductPrice(ProductThumbnail productThumbnail);

        CalculatedProductPrice CalculateProductPrice(Product product);

        CalculatedProductPrice CalculateProductPrice(decimal price, decimal? oldPrice, decimal? specialPrice, DateTimeOffset? specialPriceStart, DateTimeOffset? specialPriceEnd);
    }
}
