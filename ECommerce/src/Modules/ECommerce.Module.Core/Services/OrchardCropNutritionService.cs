using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service for managing orchard crop nutrition data independently.
/// Handles CRUD operations for fertilization and nutrition management.
/// </summary>
public class OrchardCropNutritionService : IOrchardCropNutritionService
{
    private readonly IRepository<OrchardCropNutrition> _cropNutritionRepo;
    private readonly IRepository<OrchardProfile> _profileRepo;

    public OrchardCropNutritionService(
        IRepository<OrchardCropNutrition> cropNutritionRepo,
        IRepository<OrchardProfile> profileRepo)
    {
        _cropNutritionRepo = cropNutritionRepo;
        _profileRepo = profileRepo;
    }

    public async Task<OrchardCropNutrition> GetByOrchardProfileIdAsync(long orchardProfileId)
    {
        return await _cropNutritionRepo.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.OrchardProfileId == orchardProfileId)
            .ConfigureAwait(false);
    }

    public async Task<SectionOperationResult> CreateOrUpdateAsync(long orchardProfileId, OrchardCropNutrition cropNutrition)
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

            var existing = await _cropNutritionRepo.Query()
                .FirstOrDefaultAsync(c => c.OrchardProfileId == orchardProfileId)
                .ConfigureAwait(false);

            if (existing != null)
            {
                _cropNutritionRepo.Remove(existing);
                await _cropNutritionRepo.SaveChangesAsync().ConfigureAwait(false);
            }

            if (cropNutrition != null)
            {
                var newNutrition = new OrchardCropNutrition
                {
                    OrchardProfileId = orchardProfileId,
                    FertilizerPractices = cropNutrition.FertilizerPractices,
                    HasFertigationFacility = cropNutrition.HasFertigationFacility,
                    NutritionPlan = cropNutrition.NutritionPlan,
                    IsMulchingApplied = cropNutrition.IsMulchingApplied,
                    LastFertilizerApplicationDate = cropNutrition.LastFertilizerApplicationDate,
                    FertilizerApplicationMode = cropNutrition.FertilizerApplicationMode
                };

                _cropNutritionRepo.Add(newNutrition);
                await _cropNutritionRepo.SaveChangesAsync().ConfigureAwait(false);

                return new SectionOperationResult
                {
                    Success = true,
                    Message = "Crop nutrition data saved successfully.",
                    RecordsAffected = 1
                };
            }

            return new SectionOperationResult
            {
                Success = true,
                Message = "Crop nutrition data cleared successfully.",
                RecordsAffected = 0
            };
        }
        catch (Exception ex)
        {
            return new SectionOperationResult
            {
                Success = false,
                Message = $"Error saving crop nutrition data: {ex.Message}",
                RecordsAffected = 0
            };
        }
    }

    public async Task<bool> DeleteAsync(long orchardProfileId)
    {
        try
        {
            var existing = await _cropNutritionRepo.Query()
                .FirstOrDefaultAsync(c => c.OrchardProfileId == orchardProfileId)
                .ConfigureAwait(false);

            if (existing == null)
                return false;

            _cropNutritionRepo.Remove(existing);
            await _cropNutritionRepo.SaveChangesAsync().ConfigureAwait(false);
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
