using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service for managing orchard infrastructure data independently.
/// Handles CRUD operations for equipment, labor, and storage facilities.
/// </summary>
public class OrchardInfrastructureService : IOrchardInfrastructureService
{
    private readonly IRepository<OrchardInfrastructure> _infrastructureRepo;
    private readonly IRepository<OrchardProfile> _profileRepo;

    public OrchardInfrastructureService(
        IRepository<OrchardInfrastructure> infrastructureRepo,
        IRepository<OrchardProfile> profileRepo)
    {
        _infrastructureRepo = infrastructureRepo;
        _profileRepo = profileRepo;
    }

    public async Task<OrchardInfrastructure> GetByOrchardProfileIdAsync(long orchardProfileId)
    {
        return await _infrastructureRepo.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.OrchardProfileId == orchardProfileId)
            .ConfigureAwait(false);
    }

    public async Task<SectionOperationResult> CreateOrUpdateAsync(long orchardProfileId, OrchardInfrastructure infrastructure)
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

            var existing = await _infrastructureRepo.Query()
                .FirstOrDefaultAsync(i => i.OrchardProfileId == orchardProfileId)
                .ConfigureAwait(false);

            if (existing != null)
            {
                _infrastructureRepo.Remove(existing);
                await _infrastructureRepo.SaveChangesAsync().ConfigureAwait(false);
            }

            if (infrastructure != null)
            {
                var newInfrastructure = new OrchardInfrastructure
                {
                    OrchardProfileId = orchardProfileId,
                    EquipmentOwned = SerializeArray(infrastructure.EquipmentOwned),
                    StorageFacilities = SerializeArray(infrastructure.StorageFacilities),
                    OtherTools = SerializeArray(infrastructure.OtherTools),
                    PermanentLabor = infrastructure.PermanentLabor,
                    SeasonalLabor = infrastructure.SeasonalLabor,
                    ContractBasedLabor = infrastructure.ContractBasedLabor

                };
                _infrastructureRepo.Add(newInfrastructure);
                await _infrastructureRepo.SaveChangesAsync().ConfigureAwait(false);

                return new SectionOperationResult
                {
                    Success = true,
                    Message = "Infrastructure data saved successfully.",
                    RecordsAffected = 1
                };
            }

            return new SectionOperationResult
            {
                Success = true,
                Message = "Infrastructure data cleared successfully.",
                RecordsAffected = 0
            };
        }
        catch (Exception ex)
        {
            return new SectionOperationResult
            {
                Success = false,
                Message = $"Error saving infrastructure data: {ex.Message}",
                RecordsAffected = 0
            };
        }
    }

    public async Task<bool> DeleteAsync(long orchardProfileId)
    {
        try
        {
            var existing = await _infrastructureRepo.Query()
                .FirstOrDefaultAsync(i => i.OrchardProfileId == orchardProfileId)
                .ConfigureAwait(false);

            if (existing == null)
                return false;

            _infrastructureRepo.Remove(existing);
            await _infrastructureRepo.SaveChangesAsync().ConfigureAwait(false);
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
