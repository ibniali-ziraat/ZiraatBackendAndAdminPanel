using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    /// <summary>
    /// Manages vendor product assignments with multi-vendor conflict resolution
    /// </summary>
    public class VendorProductService : IVendorProductService
    {
        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IRepository<VendorProductVariation> _vendorProductVariationRepository;
        private readonly IRepository<VendorProductVariationOption> _vendorProductVariationOptionRepository;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IProductPriceProvider _productPriceProvider;

        public VendorProductService(
            IRepository<VendorProduct> vendorProductRepository,
            IRepository<VendorProductVariation> vendorProductVariationRepository,
            IRepository<VendorProductVariationOption> vendorProductVariationOptionRepository,
            IRepository<Vendor> vendorRepository,
            IProductPriceProvider productPriceProvider)
        {
            _vendorProductRepository = vendorProductRepository;
            _vendorProductVariationRepository = vendorProductVariationRepository;
            _vendorProductVariationOptionRepository = vendorProductVariationOptionRepository;
            _vendorRepository = vendorRepository;
            _productPriceProvider = productPriceProvider;
        }

        public async Task<VendorProduct> AssignProductToVendor(
             long vendorId,
            long productId,
            decimal? assignedPrice,
            decimal? discountPrice,
            int stockQuantity,
            bool isActive,
            string batchNumber = null,
            DateTime? manufacturingDate = null,
            decimal? maximumRetailPrice = null,
            decimal? taxRate = null,
            decimal? creditSaleHikePercentage = null,
            bool isAvailableForCreditBuyers = false,
            decimal? NetSalePriceForZiraat = null,
            decimal? SalePriceForCreditor = null,
            IList<VendorProductVariationForm> variations = null,
            bool syncToMainProduct = false)
        {
            var vendor = await _vendorRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == vendorId);

            if (vendor == null)
            {
                throw new InvalidOperationException($"Vendor with ID {vendorId} not found.");
            }

            var productInfo = await _productPriceProvider.GetProductInfoAsync(productId);
            if (productInfo == null)
            {
                throw new InvalidOperationException($"Product with ID {productId} not found.");
            }

            var existingAssignment = await _vendorProductRepository.Query()
                .FirstOrDefaultAsync(x => x.VendorId == vendorId && x.ProductId == productId);

            if (existingAssignment != null)
            {
                throw new InvalidOperationException("This product is already assigned to the vendor.");
            }

            var vendorProduct = new VendorProduct
            {
                VendorId = vendorId,
                ProductId = productId,
                ProductName = productInfo.Name,
                ProductSku = productInfo.Sku,
                BatchNumber = batchNumber,
                ManufacturingDate = manufacturingDate,
                MaximumRetailPrice = maximumRetailPrice,
                AssignedPrice = assignedPrice ?? productInfo.Price,
                OriginalPrice = productInfo.Price,
                DiscountPrice = discountPrice ?? 0,
                TaxRate = taxRate,
                CreditSaleHikePercentage = creditSaleHikePercentage,
                IsAvailableForCreditBuyers = isAvailableForCreditBuyers,
                StockQuantity = stockQuantity,
                IsActive = isActive,
                CreatedOn = DateTimeOffset.Now,
                LatestUpdatedOn = DateTimeOffset.Now,
                NetSalePriceForZiraat = NetSalePriceForZiraat,
                SalePriceForCreditor = SalePriceForCreditor
            };

            CalculateAutoCalculatedPrices(vendorProduct, !NetSalePriceForZiraat.HasValue, !SalePriceForCreditor.HasValue);
            SyncVendorProductVariations(vendorProduct, variations, productInfo);

            _vendorProductRepository.Add(vendorProduct);
            await _vendorProductRepository.SaveChangesAsync();

            return vendorProduct;
        }

        public async Task<VendorProduct> UpdateVendorProduct(
            long vendorProductId,
            decimal? assignedPrice,
            decimal? discountPrice,
            int stockQuantity,
            bool isActive,
            string batchNumber = null,
            DateTime? manufacturingDate = null,
            decimal? maximumRetailPrice = null,
            decimal? taxRate = null,
            decimal? creditSaleHikePercentage = null,
            bool isAvailableForCreditBuyers = false,
            decimal? NetSalePriceForZiraat = null,
            decimal? SalePriceForCreditor = null,
            IList<VendorProductVariationForm> variations = null,
            bool syncToMainProduct = false)
        {
            var vendorProduct = await _vendorProductRepository.Query()
                .Include(x => x.Variations).ThenInclude(x => x.OptionCombinations)
                .FirstOrDefaultAsync(x => x.Id == vendorProductId);

            if (vendorProduct == null)
            {
                throw new InvalidOperationException($"Vendor product assignment with ID {vendorProductId} not found.");
            }

            if (assignedPrice.HasValue)
            {
                vendorProduct.AssignedPrice = assignedPrice.Value;
            }

            if (discountPrice.HasValue)
            {
                vendorProduct.DiscountPrice = discountPrice.Value;
            }

            if (batchNumber != null)
            {
                vendorProduct.BatchNumber = batchNumber;
            }

            if (manufacturingDate.HasValue)
            {
                vendorProduct.ManufacturingDate = manufacturingDate.Value;
            }

            if (maximumRetailPrice.HasValue)
            {
                vendorProduct.MaximumRetailPrice = maximumRetailPrice.Value;
            }

            if (taxRate.HasValue)
            {
                vendorProduct.TaxRate = taxRate.Value;
            }

            if (creditSaleHikePercentage.HasValue)
            {
                vendorProduct.CreditSaleHikePercentage = creditSaleHikePercentage.Value;
            }
            if (NetSalePriceForZiraat.HasValue)
            {
                vendorProduct.NetSalePriceForZiraat = NetSalePriceForZiraat.Value;
            }
            if (SalePriceForCreditor.HasValue)
            {
                vendorProduct.SalePriceForCreditor = SalePriceForCreditor.Value;
            }

            vendorProduct.IsAvailableForCreditBuyers = isAvailableForCreditBuyers;
            vendorProduct.StockQuantity = stockQuantity;
            vendorProduct.IsActive = isActive;
            vendorProduct.LatestUpdatedOn = DateTimeOffset.Now;

            CalculateAutoCalculatedPrices(vendorProduct, !NetSalePriceForZiraat.HasValue, !SalePriceForCreditor.HasValue);

            if (variations != null)
            {
                var productInfo = await _productPriceProvider.GetProductInfoAsync(vendorProduct.ProductId);
                SyncVendorProductVariations(vendorProduct, variations, productInfo);
            }

            await _vendorProductRepository.SaveChangesAsync();

            return vendorProduct;
        }

        public async Task DeleteVendorProduct(long vendorProductId)
        {
            var vendorProduct = await _vendorProductRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == vendorProductId);

            if (vendorProduct == null)
            {
                throw new InvalidOperationException($"Vendor product assignment with ID {vendorProductId} not found.");
            }

            _vendorProductRepository.Remove(vendorProduct);
            await _vendorProductRepository.SaveChangesAsync();
        }

        public async Task<List<VendorProduct>> GetActiveVendorAssignmentsForProduct(long productId)
        {
            return await _vendorProductRepository.Query()
                .Where(x => x.ProductId == productId && x.IsActive && x.Vendor != null && !x.Vendor.IsDeleted)
                .ToListAsync();
        }

        public async Task<VendorProductAggregateData> GetAggregateProductData(long productId)
        {
            var activeAssignments = await GetActiveVendorAssignmentsForProduct(productId);

            if (!activeAssignments.Any())
            {
                return new VendorProductAggregateData
                {
                    ProductId = productId,
                    VendorCount = 0,
                    TotalAggregatedStock = 0
                };
            }

            var lowestPrice = activeAssignments
                .Where(x => x.AssignedPrice.HasValue)
                .Min(x => x.AssignedPrice) ?? 0;

            var highestDiscount = activeAssignments
                .Where(x => x.DiscountPrice.HasValue && x.DiscountPrice > 0)
                .Max(x => (decimal?)x.DiscountPrice) ?? 0;

            return new VendorProductAggregateData
            {
                ProductId = productId,
                ProductName = activeAssignments.First().ProductName,
                TotalAggregatedStock = activeAssignments.Sum(x => x.StockQuantity),
                LowestPrice = lowestPrice,
                HighestDiscount = highestDiscount,
                VendorCount = activeAssignments.Select(x => x.VendorId).Distinct().Count(),
                VendorOptions = activeAssignments
                    .Select(x => new VendorProductBrief
                    {
                        VendorProductId = x.Id,
                        VendorId = x.VendorId,
                        AssignedPrice = x.AssignedPrice,
                        DiscountPrice = x.DiscountPrice,
                        StockQuantity = x.StockQuantity,
                        IsActive = x.IsActive
                    })
                    .ToList()
            };
        }

        /// <summary>
        /// Calculates net sale price and credit sale price based on tax rate and credit hike percentage
        /// </summary>
        private void CalculateAutoCalculatedPrices(VendorProduct vendorProduct, bool updateNetSalePrice, bool updateSalePriceForCreditor)
        {
            if (updateNetSalePrice)
            {
                vendorProduct.NetSalePriceForZiraat = VendorProductPricing.GetNetSalePriceForZiraat(vendorProduct);
            }

            if (updateSalePriceForCreditor)
            {
                vendorProduct.SalePriceForCreditor = VendorProductPricing.GetSalePriceForCreditor(vendorProduct);
            }
        }

        private void SyncVendorProductVariations(VendorProduct vendorProduct, IList<VendorProductVariationForm> variationForms, ProductInfoDto productInfo)
        {
            if (variationForms == null)
            {
                return;
            }

            var availableOptions = (productInfo?.Options ?? new List<ProductOptionInfoDto>())
                .ToDictionary(x => x.Id);
            var incomingForms = variationForms
                .Where(x => x != null)
                .Select((form, index) => new
                {
                    Form = form,
                    SortIndex = form.SortIndex > 0 ? form.SortIndex : index
                })
                .ToList();

            if (incomingForms.Any() && !availableOptions.Any())
            {
                throw new InvalidOperationException("This product does not have admin-defined variation options.");
            }

            var existingById = vendorProduct.Variations.ToDictionary(x => x.Id);
            var existingBySignature = vendorProduct.Variations
                .GroupBy(BuildVariationSignature)
                .ToDictionary(x => x.Key, x => x.First());
            var touchedVariationIds = new HashSet<long>();
            var touchedSignatures = new HashSet<string>();

            foreach (var incoming in incomingForms)
            {
                var form = incoming.Form;
                var options = NormalizeVariationOptions(form.OptionCombinations, availableOptions);
                if (!options.Any())
                {
                    throw new InvalidOperationException("Each vendor variation must include at least one option value.");
                }

                var signature = BuildVariationSignature(options);
                if (touchedSignatures.Contains(signature))
                {
                    throw new InvalidOperationException("Duplicate vendor variation option values are not allowed.");
                }

                touchedSignatures.Add(signature);

                VendorProductVariation variation = null;
                if (form.Id > 0)
                {
                    existingById.TryGetValue(form.Id, out variation);
                }

                if (variation == null)
                {
                    existingBySignature.TryGetValue(signature, out variation);
                }

                if (variation == null)
                {
                    variation = new VendorProductVariation
                    {
                        CreatedOn = DateTimeOffset.Now
                    };
                    vendorProduct.Variations.Add(variation);
                }

                ApplyVariation(form, variation, vendorProduct, productInfo, options, incoming.SortIndex);
                touchedVariationIds.Add(variation.Id);
            }

            var removedVariations = vendorProduct.Variations
                .Where(x => x.Id > 0 && !touchedVariationIds.Contains(x.Id))
                .ToList();
            foreach (var variation in removedVariations)
            {
                _vendorProductVariationRepository.Remove(variation);
            }

            if (vendorProduct.Variations.Any(x => !removedVariations.Contains(x)))
            {
                vendorProduct.StockQuantity = vendorProduct.Variations
                    .Where(x => !removedVariations.Contains(x) && x.IsActive)
                    .Sum(x => x.StockQuantity);
            }
        }

        private IList<VendorProductVariationOptionForm> NormalizeVariationOptions(
            IList<VendorProductVariationOptionForm> optionForms,
            IDictionary<long, ProductOptionInfoDto> availableOptions)
        {
            var normalizedOptions = new List<VendorProductVariationOptionForm>();
            foreach (var optionForm in optionForms ?? new List<VendorProductVariationOptionForm>())
            {
                if (optionForm == null || optionForm.OptionId <= 0 || string.IsNullOrWhiteSpace(optionForm.Value))
                {
                    continue;
                }

                if (!availableOptions.TryGetValue(optionForm.OptionId, out var productOption))
                {
                    throw new InvalidOperationException($"Option ID {optionForm.OptionId} is not available for this product.");
                }

                normalizedOptions.Add(new VendorProductVariationOptionForm
                {
                    OptionId = optionForm.OptionId,
                    OptionName = string.IsNullOrWhiteSpace(optionForm.OptionName) ? productOption.Name : optionForm.OptionName,
                    Value = optionForm.Value.Trim(),
                    StockQuantity = optionForm.StockQuantity,
                    SortIndex = optionForm.SortIndex
                });
            }

            return normalizedOptions
                .GroupBy(x => x.OptionId)
                .Select(x => x.OrderBy(y => y.SortIndex).First())
                .OrderBy(x => x.SortIndex)
                .ThenBy(x => x.OptionId)
                .ToList();
        }

        private void ApplyVariation(
            VendorProductVariationForm form,
            VendorProductVariation variation,
            VendorProduct vendorProduct,
            ProductInfoDto productInfo,
            IList<VendorProductVariationOptionForm> options,
            int sortIndex)
        {
            var optionValueName = string.Join(" / ", options.Select(x => x.Value));
            variation.Name = string.IsNullOrWhiteSpace(form.Name) ? $"{productInfo?.Name ?? vendorProduct.ProductName} - {optionValueName}" : form.Name.Trim();
            variation.NormalizedName = string.IsNullOrWhiteSpace(form.NormalizedName) ? string.Join("-", options.Select(x => x.Value)).ToLowerInvariant() : form.NormalizedName.Trim();
            variation.Sku = string.IsNullOrWhiteSpace(form.Sku) ? vendorProduct.ProductSku : form.Sku.Trim();
            variation.Gtin = string.IsNullOrWhiteSpace(form.Gtin) ? null : form.Gtin.Trim();
            variation.MaximumRetailPrice = form.MaximumRetailPrice ?? vendorProduct.MaximumRetailPrice;
            variation.AssignedPrice = form.AssignedPrice ?? vendorProduct.AssignedPrice ?? productInfo?.Price;
            variation.OriginalPrice = productInfo?.Price ?? vendorProduct.OriginalPrice;
            variation.DiscountPrice = form.DiscountPrice ?? 0;
            variation.TaxRate = form.TaxRate ?? vendorProduct.TaxRate;
            variation.CreditSaleHikePercentage = form.CreditSaleHikePercentage ?? vendorProduct.CreditSaleHikePercentage;
            variation.IsAvailableForCreditBuyers = form.IsAvailableForCreditBuyers ?? vendorProduct.IsAvailableForCreditBuyers;
            variation.StockQuantity = GetVariationStockQuantity(form);
            variation.IsActive = form.IsActive;
            variation.SortIndex = sortIndex;
            variation.LatestUpdatedOn = DateTimeOffset.Now;
            variation.NetSalePriceForZiraat = form.NetSalePriceForZiraat;
            variation.SalePriceForCreditor = form.SalePriceForCreditor;

            CalculateAutoCalculatedVariationPrices(variation, !form.NetSalePriceForZiraat.HasValue, !form.SalePriceForCreditor.HasValue);
            SyncVariationOptions(variation, options);
        }

        private void SyncVariationOptions(VendorProductVariation variation, IList<VendorProductVariationOptionForm> options)
        {
            var touchedOptionIds = new HashSet<long>();
            foreach (var option in options)
            {
                var existing = variation.OptionCombinations.FirstOrDefault(x => x.OptionId == option.OptionId);
                if (existing == null)
                {
                    existing = new VendorProductVariationOption
                    {
                        OptionId = option.OptionId
                    };
                    variation.OptionCombinations.Add(existing);
                }

                existing.OptionName = option.OptionName;
                existing.Value = option.Value;
                existing.StockQuantity = option.StockQuantity ?? variation.StockQuantity;
                existing.SortIndex = option.SortIndex;
                touchedOptionIds.Add(option.OptionId);
            }

            var removedOptions = variation.OptionCombinations
                .Where(x => x.Id > 0 && !touchedOptionIds.Contains(x.OptionId))
                .ToList();
            foreach (var removedOption in removedOptions)
            {
                _vendorProductVariationOptionRepository.Remove(removedOption);
            }
        }

        private static string BuildVariationSignature(VendorProductVariation variation)
        {
            return BuildVariationSignature(variation.OptionCombinations
                .Select(x => new VendorProductVariationOptionForm
                {
                    OptionId = x.OptionId,
                    Value = x.Value,
                    StockQuantity = x.StockQuantity
                })
                .ToList());
        }

        private static string BuildVariationSignature(IEnumerable<VendorProductVariationOptionForm> options)
        {
            return string.Join("|", options
                .Where(x => x != null)
                .OrderBy(x => x.OptionId)
                .Select(x => $"{x.OptionId}:{(x.Value ?? string.Empty).Trim().ToLowerInvariant()}"));
        }

        private static void CalculateAutoCalculatedVariationPrices(VendorProductVariation variation, bool updateNetSalePrice, bool updateSalePriceForCreditor)
        {
            if (updateNetSalePrice)
            {
                var effectivePrice = GetEffectiveVariationPrice(variation);
                variation.NetSalePriceForZiraat = variation.TaxRate > 0
                    ? effectivePrice + (effectivePrice * variation.TaxRate.Value / 100)
                    : effectivePrice;
            }

            if (updateSalePriceForCreditor)
            {
                var netSalePrice = variation.NetSalePriceForZiraat ?? GetEffectiveVariationPrice(variation);
                variation.SalePriceForCreditor = variation.IsAvailableForCreditBuyers && variation.CreditSaleHikePercentage > 0
                    ? netSalePrice + (netSalePrice * variation.CreditSaleHikePercentage.Value / 100)
                    : netSalePrice;
            }
        }

        private static decimal GetEffectiveVariationPrice(VendorProductVariation variation)
        {
            if (variation.DiscountPrice.HasValue && variation.DiscountPrice.Value > 0)
            {
                return variation.DiscountPrice.Value;
            }

            return variation.AssignedPrice ?? 0;
        }

        private static int GetVariationStockQuantity(VendorProductVariationForm form)
        {
            var optionStocks = form.OptionCombinations?
                .Where(x => x?.StockQuantity != null)
                .Sum(x => x.StockQuantity.Value);

            if (optionStocks.HasValue && optionStocks.Value > 0)
            {
                return optionStocks.Value;
            }

            return form.StockQuantity ?? 0;
        }
    }
}
