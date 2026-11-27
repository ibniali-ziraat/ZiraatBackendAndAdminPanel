using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service for managing orchard soil and water data independently.
/// Handles CRUD operations for soil composition and water management.
/// </summary>
public class OrchardSoilAndWaterService : IOrchardSoilAndWaterService
{
    private readonly IRepository<OrchardSoilAndWater> _soilWaterRepo;
    private readonly IRepository<OrchardProfile> _profileRepo;

    public OrchardSoilAndWaterService(
        IRepository<OrchardSoilAndWater> soilWaterRepo,
        IRepository<OrchardProfile> profileRepo)
    {
        _soilWaterRepo = soilWaterRepo;
        _profileRepo = profileRepo;
    }

    public async Task<OrchardSoilAndWater> GetByOrchardProfileIdAsync(long orchardProfileId)
    {
        return await _soilWaterRepo.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrchardProfileId == orchardProfileId)
            .ConfigureAwait(false);
    }

    public async Task<SectionOperationResult> CreateOrUpdateAsync(long orchardProfileId, OrchardSoilAndWater soilAndWater)
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

            var existing = await _soilWaterRepo.Query()
                .FirstOrDefaultAsync(s => s.OrchardProfileId == orchardProfileId)
                .ConfigureAwait(false);

            if (existing != null)
            {
                _soilWaterRepo.Remove(existing);
                await _soilWaterRepo.SaveChangesAsync().ConfigureAwait(false);
            }

            if (soilAndWater != null)
            {
                var newSoilAndWater = new OrchardSoilAndWater
                {
                    OrchardProfileId = orchardProfileId,
                    SoilType = soilAndWater.SoilType,
                    SoilPh = soilAndWater.SoilPh,
                    SoilNitrogen = soilAndWater.SoilNitrogen,
                    SoilPhosphorus = soilAndWater.SoilPhosphorus,
                    SoilPotassium = soilAndWater.SoilPotassium,
                    SoilOrganicCarbon = soilAndWater.SoilOrganicCarbon,
                    SoilMicronutrients = soilAndWater.SoilMicronutrients,
                    SoilTestReportUrls = soilAndWater.SoilTestReportUrls,
                    SoilHealthStatus = SerializeArray(soilAndWater.SoilHealthStatus),
                    LastSoilTestDate = soilAndWater.LastSoilTestDate,
                    WaterSource = soilAndWater.WaterSource,
                    IrrigationMethod = soilAndWater.IrrigationMethod,
                    WaterAvailability = soilAndWater.WaterAvailability
                };
                _soilWaterRepo.Add(newSoilAndWater);
                await _soilWaterRepo.SaveChangesAsync().ConfigureAwait(false);

                return new SectionOperationResult
                {
                    Success = true,
                    Message = "Soil and water data saved successfully.",
                    RecordsAffected = 1
                };
            }

            return new SectionOperationResult
            {
                Success = true,
                Message = "Soil and water data cleared successfully.",
                RecordsAffected = 0
            };
        }
        catch (Exception ex)
        {
            return new SectionOperationResult
            {
                Success = false,
                Message = $"Error saving soil and water data: {ex.Message}",
                RecordsAffected = 0
            };
        }
    }

    public async Task<bool> DeleteAsync(long orchardProfileId)
    {
        try
        {
            var existing = await _soilWaterRepo.Query()
                .FirstOrDefaultAsync(s => s.OrchardProfileId == orchardProfileId)
                .ConfigureAwait(false);

            if (existing == null)
                return false;

            _soilWaterRepo.Remove(existing);
            await _soilWaterRepo.SaveChangesAsync().ConfigureAwait(false);
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
