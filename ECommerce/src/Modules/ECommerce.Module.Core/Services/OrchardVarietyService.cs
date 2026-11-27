using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service for managing orchard variety data independently.
/// Handles CRUD operations for fruit varieties planted in orchards.
/// </summary>
public class OrchardVarietyService : IOrchardVarietyService
{
    private readonly IRepository<OrchardVariety> _varietyRepo;
    private readonly IRepository<OrchardProfile> _profileRepo;

    public OrchardVarietyService(
        IRepository<OrchardVariety> varietyRepo,
        IRepository<OrchardProfile> profileRepo)
    {
        _varietyRepo = varietyRepo;
        _profileRepo = profileRepo;
    }

    public async Task<IEnumerable<OrchardVariety>> GetByOrchardProfileIdAsync(long orchardProfileId)
    {
        return await _varietyRepo.Query()
            .AsNoTracking()
            .Where(v => v.OrchardProfileId == orchardProfileId)
            .OrderByDescending(v => v.Id)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<OrchardVariety> GetByIdAsync(long varietyId)
    {
        return await _varietyRepo.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == varietyId)
            .ConfigureAwait(false);
    }

    public async Task<long> CreateAsync(long orchardProfileId, OrchardVariety variety)
    {
        if (!await ProfileExistsAsync(orchardProfileId))
            throw new InvalidOperationException($"Orchard profile with ID {orchardProfileId} not found.");

        if (string.IsNullOrWhiteSpace(variety.VarietyName))
            throw new ArgumentException("Variety name is required.", nameof(variety.VarietyName));

        variety.OrchardProfileId = orchardProfileId;
        _varietyRepo.Add(variety);
        await _varietyRepo.SaveChangesAsync().ConfigureAwait(false);
        return variety.Id;
    }

    public async Task<SectionOperationResult> CreateMultipleAsync(long orchardProfileId, IEnumerable<OrchardVariety> varieties)
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

            // Delete existing varieties
            await DeleteAllByOrchardProfileIdAsync(orchardProfileId);

            var varietiesList = varieties?.ToList() ?? new List<OrchardVariety>();
            var count = 0;

            foreach (var variety in varietiesList)
            {
                if (string.IsNullOrWhiteSpace(variety.VarietyName))
                {
                    return new SectionOperationResult
                    {
                        Success = false,
                        Message = "Variety name is required for all varieties.",
                        RecordsAffected = 0
                    };
                }

                variety.OrchardProfileId = orchardProfileId;
                _varietyRepo.Add(variety);
                count++;
            }

            if (count > 0)
                await _varietyRepo.SaveChangesAsync().ConfigureAwait(false);

            return new SectionOperationResult
            {
                Success = true,
                Message = $"Varieties saved successfully. Added {count} variety(ies).",
                RecordsAffected = count
            };
        }
        catch (Exception ex)
        {
            return new SectionOperationResult
            {
                Success = false,
                Message = $"Error saving varieties: {ex.Message}",
                RecordsAffected = 0
            };
        }
    }

    public async Task<bool> UpdateAsync(long varietyId, OrchardVariety variety)
    {
        var existing = await _varietyRepo.Query()
            .FirstOrDefaultAsync(v => v.Id == varietyId)
            .ConfigureAwait(false);

        if (existing == null)
            return false;

        if (!string.IsNullOrWhiteSpace(variety.VarietyName))
            existing.VarietyName = variety.VarietyName;

        if (variety.NumberOfTrees > 0)
            existing.NumberOfTrees = variety.NumberOfTrees;

        if (!string.IsNullOrWhiteSpace(variety.RootstockUsed))
            existing.RootstockUsed = variety.RootstockUsed;

        if (!string.IsNullOrWhiteSpace(variety.PlantDensity))
            existing.PlantDensity = variety.PlantDensity;

        if (variety.TreesLessThan5Years >= 0)
            existing.TreesLessThan5Years = variety.TreesLessThan5Years;

        if (variety.TreesFiveToTenYears >= 0)
            existing.TreesFiveToTenYears = variety.TreesFiveToTenYears;

        if (variety.TreesMoreThan10Years >= 0)
            existing.TreesMoreThan10Years = variety.TreesMoreThan10Years;

        await _varietyRepo.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<bool> DeleteAsync(long varietyId)
    {
        var variety = await _varietyRepo.Query()
            .FirstOrDefaultAsync(v => v.Id == varietyId)
            .ConfigureAwait(false);

        if (variety == null)
            return false;

        _varietyRepo.Remove(variety);
        await _varietyRepo.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<SectionOperationResult> DeleteAllByOrchardProfileIdAsync(long orchardProfileId)
    {
        try
        {
            var varieties = await _varietyRepo.Query()
                .Where(v => v.OrchardProfileId == orchardProfileId)
                .ToListAsync()
                .ConfigureAwait(false);

            var count = varieties.Count;

            foreach (var variety in varieties)
                _varietyRepo.Remove(variety);

            if (count > 0)
                await _varietyRepo.SaveChangesAsync().ConfigureAwait(false);

            return new SectionOperationResult
            {
                Success = true,
                Message = $"Deleted {count} variety(ies).",
                RecordsAffected = count
            };
        }
        catch (Exception ex)
        {
            return new SectionOperationResult
            {
                Success = false,
                Message = $"Error deleting varieties: {ex.Message}",
                RecordsAffected = 0
            };
        }
    }

    private async Task<bool> ProfileExistsAsync(long orchardProfileId)
    {
        return await _profileRepo.Query()
            .AnyAsync(p => p.Id == orchardProfileId)
            .ConfigureAwait(false);
    }
}
