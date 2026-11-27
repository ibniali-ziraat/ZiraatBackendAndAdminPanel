using System;
using System.Collections.Generic;
using System.Linq;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using Newtonsoft.Json;

namespace ECommerce.Module.Catalog.Areas.Catalog.ViewModels
{
    public class ProductThumbnail
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Slug { get; set; }

        public decimal Price { get; set; }

        public decimal? OldPrice { get; set; }

        public decimal? SpecialPrice { get; set; }

        public bool IsCallForPricing { get; set; }

        public bool IsAllowToOrder { get; set; }

        public int? StockQuantity { get; set; }

        public DateTimeOffset? SpecialPriceStart { get; set; }

        public DateTimeOffset? SpecialPriceEnd { get; set; }

        public Media ThumbnailImage { get; set; }

        public string ThumbnailUrl { get; set; }

        public int ReviewsCount { get; set; }

        public double? RatingAverage { get; set; }

        public CalculatedProductPrice CalculatedProductPrice { get; set; }

        public long? VendorId { get; set; }

        public long? VendorProductId { get; set; }

        public bool IsAvailableForCreditBuyers { get; set; }

        public decimal? SalePriceForCreditor { get; set; }

        public decimal? CreditSaleHikePercentage { get; set; }

        public decimal? NetSalePriceForZiraat { get; set; }

        public ProductVendorDetail Vendor { get; set; }

        public IList<ProductAttributeVm> Attributes { get; set; } = new List<ProductAttributeVm>();

        public IList<ProductOptionVm> Options { get; set; } = new List<ProductOptionVm>();

        public static ProductThumbnail FromProduct(Product product)
        {
            var productThumbnail = new ProductThumbnail
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Price = product.Price,
                OldPrice = product.OldPrice,
                SpecialPrice = product.SpecialPrice,
                SpecialPriceStart = product.SpecialPriceStart,
                SpecialPriceEnd = product.SpecialPriceEnd,
                StockQuantity = product.StockQuantity,
                IsAllowToOrder = product.IsAllowToOrder,
                IsCallForPricing = product.IsCallForPricing,
                ThumbnailImage = product.ThumbnailImage,
                ReviewsCount = product.ReviewsCount,
                RatingAverage = product.RatingAverage,
                Attributes = product.AttributeValues.Select(attributeValue => new ProductAttributeVm
                {
                    AttributeValueId = attributeValue.Id,
                    Id = attributeValue.AttributeId,
                    Name = attributeValue.Attribute?.Name,
                    GroupName = attributeValue.Attribute?.Group?.Name,
                    Value = attributeValue.Value
                }).ToList(),
                Options = product.OptionValues.OrderBy(optionValue => optionValue.SortIndex).Select(optionValue => new ProductOptionVm
                {
                    Id = optionValue.OptionId,
                    Name = optionValue.Option?.Name,
                    DisplayType = optionValue.DisplayType,
                    Values = DeserializeOptionValues(optionValue.Value)
                }).ToList()
            };

            return productThumbnail;
        }

        private static IList<ProductOptionValueVm> DeserializeOptionValues(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? new List<ProductOptionValueVm>()
                : JsonConvert.DeserializeObject<IList<ProductOptionValueVm>>(value) ?? new List<ProductOptionValueVm>();
        }
    }

    public class ProductVendorDetail
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Slug { get; set; }

        public string Description { get; set; }

        public string Email { get; set; }

        public bool IsActive { get; set; }
    }
}
