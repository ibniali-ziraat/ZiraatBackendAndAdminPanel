using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service for managing orchard input cost data independently.
/// Handles CRUD operations for pesticide suppliers and payment information.
/// </summary>
public class OrchardInputCostService : IOrchardInputCostService
{
    private readonly IRepository<OrchardInputCost> _inputCostRepo;
    private readonly IRepository<OrchardProfile> _profileRepo;

    public OrchardInputCostService(
        IRepository<OrchardInputCost> inputCostRepo,
        IRepository<OrchardProfile> profileRepo)
    {
        _inputCostRepo = inputCostRepo;
        _profileRepo = profileRepo;
    }

    public async Task<OrchardInputCost> GetByOrchardProfileIdAsync(long orchardProfileId)
    {
        return await _inputCostRepo.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.OrchardProfileId == orchardProfileId)
            .ConfigureAwait(false);
    }

    public async Task<SectionOperationResult> CreateOrUpdateAsync(long orchardProfileId, OrchardInputCost inputCost)
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

            var existing = await _inputCostRepo.Query()
                .FirstOrDefaultAsync(i => i.OrchardProfileId == orchardProfileId)
                .ConfigureAwait(false);

            if (existing != null)
            {
                _inputCostRepo.Remove(existing);
                await _inputCostRepo.SaveChangesAsync().ConfigureAwait(false);
            }

            if (inputCost != null)
            {
                var newInputCost = new OrchardInputCost
                {
                    OrchardProfileId = orchardProfileId,
                    PesticideSupplierLocation = inputCost.PesticideSupplierLocation,
                    PaymentType = inputCost.PaymentType,
                    CreditPeriod = inputCost.CreditPeriod
                };
                _inputCostRepo.Add(newInputCost);
                await _inputCostRepo.SaveChangesAsync().ConfigureAwait(false);

                return new SectionOperationResult
                {
                    Success = true,
                    Message = "Input cost data saved successfully.",
                    RecordsAffected = 1
                };
            }

            return new SectionOperationResult
            {
                Success = true,
                Message = "Input cost data cleared successfully.",
                RecordsAffected = 0
            };
        }
        catch (Exception ex)
        {
            return new SectionOperationResult
            {
                Success = false,
                Message = $"Error saving input cost data: {ex.Message}",
                RecordsAffected = 0
            };
        }
    }

    public async Task<bool> DeleteAsync(long orchardProfileId)
    {
        try
        {
            var existing = await _inputCostRepo.Query()
                .FirstOrDefaultAsync(i => i.OrchardProfileId == orchardProfileId)
                .ConfigureAwait(false);

            if (existing == null)
                return false;

            _inputCostRepo.Remove(existing);
            await _inputCostRepo.SaveChangesAsync().ConfigureAwait(false);
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
