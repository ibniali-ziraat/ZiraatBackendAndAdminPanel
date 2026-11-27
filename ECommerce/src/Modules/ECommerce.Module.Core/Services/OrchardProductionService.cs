using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service for managing orchard production data independently.
/// Handles CRUD operations for yield, harvest, and marketing information.
/// </summary>
public class OrchardProductionService : IOrchardProductionService
{
    private readonly IRepository<OrchardProduction> _productionRepo;
    private readonly IRepository<OrchardProfile> _profileRepo;

    public OrchardProductionService(
        IRepository<OrchardProduction> productionRepo,
        IRepository<OrchardProfile> profileRepo)
    {
        _productionRepo = productionRepo;
        _profileRepo = profileRepo;
    }

    public async Task<OrchardProduction> GetByOrchardProfileIdAsync(long orchardProfileId)
    {
        return await _productionRepo.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrchardProfileId == orchardProfileId)
            .ConfigureAwait(false);
    }

    public async Task<SectionOperationResult> CreateOrUpdateAsync(long orchardProfileId, OrchardProduction production)
    {
        try
        {
            if (!await ProfileExistsAsync(orchardProfileId))
            {
                return new SectionOperationResult
                {
                    Success = false,
                    Message = $"Orchard profile with ID {orchardProfileId} not found.",
                    RecordsAffected = 0
                };
            }

            var existing = await _productionRepo.Query()
                .FirstOrDefaultAsync(p => p.OrchardProfileId == orchardProfileId)
                .ConfigureAwait(false);

            if (existing != null)
            {
                _productionRepo.Remove(existing);
                await _productionRepo.SaveChangesAsync().ConfigureAwait(false);
            }

            if (production != null)
            {
                var newProduction = new OrchardProduction
                {
                    OrchardProfileId = orchardProfileId,
                    AverageAnnualYield = production.AverageAnnualYield,
                    YieldUnit = production.YieldUnit,
                    PeakHarvestFromMonth = production.PeakHarvestFromMonth,
                    PeakHarvestToMonth = production.PeakHarvestToMonth,
                    SellingPreference = production.SellingPreference,
                    BuyerName = production.BuyerName,
                    BuyerContact = production.BuyerContact
                };
                _productionRepo.Add(newProduction);
                await _productionRepo.SaveChangesAsync().ConfigureAwait(false);

                return new SectionOperationResult
                {
                    Success = true,
                    Message = "Production data saved successfully.",
                    RecordsAffected = 1
                };
            }

            return new SectionOperationResult
            {
                Success = true,
                Message = "Production data cleared successfully.",
                RecordsAffected = 0
            };
        }
        catch (Exception ex)
        {
            return new SectionOperationResult
            {
                Success = false,
                Message = $"Error saving production data: {ex.Message}",
                RecordsAffected = 0
            };
        }
    }

    public async Task<bool> DeleteAsync(long orchardProfileId)
    {
        try
        {
            var existing = await _productionRepo.Query()
                .FirstOrDefaultAsync(p => p.OrchardProfileId == orchardProfileId)
                .ConfigureAwait(false);

            if (existing == null)
                return false;

            _productionRepo.Remove(existing);
            await _productionRepo.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<bool> ProfileExistsAsync(long orchardProfileId)
    {
        return await _profileRepo.Query()
            .AnyAsync(p => p.Id == orchardProfileId)
            .ConfigureAwait(false);
    }
}
