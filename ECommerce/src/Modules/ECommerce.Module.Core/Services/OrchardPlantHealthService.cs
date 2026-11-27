using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service for managing orchard plant health data independently.
/// Handles CRUD operations for disease, pest, and health management.
/// </summary>
public class OrchardPlantHealthService : IOrchardPlantHealthService
{
    private readonly IRepository<OrchardPlantHealth> _plantHealthRepo;
    private readonly IRepository<OrchardProfile> _profileRepo;

    public OrchardPlantHealthService(
        IRepository<OrchardPlantHealth> plantHealthRepo,
        IRepository<OrchardProfile> profileRepo)
    {
        _plantHealthRepo = plantHealthRepo;
        _profileRepo = profileRepo;
    }

    public async Task<OrchardPlantHealth> GetByOrchardProfileIdAsync(long orchardProfileId)
    {
        return await _plantHealthRepo.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrchardProfileId == orchardProfileId)
            .ConfigureAwait(false);
    }

    public async Task<SectionOperationResult> CreateOrUpdateAsync(long orchardProfileId, OrchardPlantHealth plantHealth)
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

            var existing = await _plantHealthRepo.Query()
                .FirstOrDefaultAsync(p => p.OrchardProfileId == orchardProfileId)
                .ConfigureAwait(false);

            if (existing != null)
            {
                _plantHealthRepo.Remove(existing);
                await _plantHealthRepo.SaveChangesAsync().ConfigureAwait(false);
            }

            if (plantHealth != null)
            {
                var newOrcharPlantHealth = new OrchardPlantHealth
                {
                    OrchardProfileId = orchardProfileId,
                    DiseaseHistory = SerializeArray(plantHealth.DiseaseHistory),
                    PestHistory = SerializeArray(plantHealth.PestHistory),
                    SpraySchedule = SerializeArray(plantHealth.SpraySchedule),
                    IsCropInsuranceCovered = plantHealth.IsCropInsuranceCovered
                };
                 
                _plantHealthRepo.Add(newOrcharPlantHealth);
                await _plantHealthRepo.SaveChangesAsync().ConfigureAwait(false);

                return new SectionOperationResult
                {
                    Success = true,
                    Message = "Plant health data saved successfully.",
                    RecordsAffected = 1
                };
            }

            return new SectionOperationResult
            {
                Success = true,
                Message = "Plant health data cleared successfully.",
                RecordsAffected = 0
            };
        }
        catch (Exception ex)
        {
            return new SectionOperationResult
            {
                Success = false,
                Message = $"Error saving plant health data: {ex.Message}",
                RecordsAffected = 0
            };
        }
    }

    public async Task<bool> DeleteAsync(long orchardProfileId)
    {
        try
        {
            var existing = await _plantHealthRepo.Query()
                .FirstOrDefaultAsync(p => p.OrchardProfileId == orchardProfileId)
                .ConfigureAwait(false);

            if (existing == null)
                return false;

            _plantHealthRepo.Remove(existing);
            await _plantHealthRepo.SaveChangesAsync().ConfigureAwait(false);
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

    private string SerializeArray(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return JsonSerializer.Serialize(new string[] { });

        var trimmed = json.Trim();
        if (trimmed.StartsWith("[") || trimmed.StartsWith("{"))
            return json;

        return JsonSerializer.Serialize(new[] { json });
    }
}
