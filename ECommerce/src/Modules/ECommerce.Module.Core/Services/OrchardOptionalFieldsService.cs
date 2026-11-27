using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service for managing orchard optional fields data independently.
/// Handles CRUD operations for certifications and optional information.
/// </summary>
public class OrchardOptionalFieldsService : IOrchardOptionalFieldsService
{
    private readonly IRepository<OrchardOptionalFields> _optionalFieldsRepo;
    private readonly IRepository<OrchardProfile> _profileRepo;

    public OrchardOptionalFieldsService(
        IRepository<OrchardOptionalFields> optionalFieldsRepo,
        IRepository<OrchardProfile> profileRepo)
    {
        _optionalFieldsRepo = optionalFieldsRepo;
        _profileRepo = profileRepo;
    }

    public async Task<OrchardOptionalFields> GetByOrchardProfileIdAsync(long orchardProfileId)
    {
        return await _optionalFieldsRepo.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrchardProfileId == orchardProfileId)
            .ConfigureAwait(false);
    }

    public async Task<SectionOperationResult> CreateOrUpdateAsync(long orchardProfileId, OrchardOptionalFields optionalFields)
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

            var existing = await _optionalFieldsRepo.Query()
                .FirstOrDefaultAsync(o => o.OrchardProfileId == orchardProfileId)
                .ConfigureAwait(false);

            if (existing != null)
            {
                _optionalFieldsRepo.Remove(existing);
                await _optionalFieldsRepo.SaveChangesAsync().ConfigureAwait(false);
            }

            if (optionalFields != null)
            {
                var newOptionalFields = new OrchardOptionalFields
                {
                    OrchardProfileId = orchardProfileId,
                    GovernmentSchemesAvailed = optionalFields.GovernmentSchemesAvailed,
                    IsFpoOrCooperativeMember = optionalFields.IsFpoOrCooperativeMember,
                    Certifications = optionalFields.Certifications,
                    DigitalToolsUsed = optionalFields.DigitalToolsUsed
                };

                _optionalFieldsRepo.Add(newOptionalFields);
                await _optionalFieldsRepo.SaveChangesAsync().ConfigureAwait(false);

                return new SectionOperationResult
                {
                    Success = true,
                    Message = "Optional fields data saved successfully.",
                    RecordsAffected = 1
                };
            }

            return new SectionOperationResult
            {
                Success = true,
                Message = "Optional fields data cleared successfully.",
                RecordsAffected = 0
            };
        }
        catch (Exception ex)
        {
            return new SectionOperationResult
            {
                Success = false,
                Message = $"Error saving optional fields data: {ex.Message}",
                RecordsAffected = 0
            };
        }
    }

    public async Task<bool> DeleteAsync(long orchardProfileId)
    {
        try
        {
            var existing = await _optionalFieldsRepo.Query()
                .FirstOrDefaultAsync(o => o.OrchardProfileId == orchardProfileId)
                .ConfigureAwait(false);

            if (existing == null)
                return false;

            _optionalFieldsRepo.Remove(existing);
            await _optionalFieldsRepo.SaveChangesAsync().ConfigureAwait(false);
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
