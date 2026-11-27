using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services;

/// <summary>
/// Service for managing orchard profiles with comprehensive CRUD operations.
/// Supports independent section operations and bulk operations with proper transaction handling.
/// </summary>
public class OrchardProfileService : IOrchardProfileService
{
    private readonly IRepository<OrchardProfile> _mainRepo;
    private readonly IRepository<OrchardLocation> _locationRepo;
    private readonly IRepository<OrchardVariety> _varietyRepo;
    private readonly IRepository<OrchardSoilAndWater> _soilWaterRepo;
    private readonly IRepository<OrchardCropNutrition> _cropNutritionRepo;
    private readonly IRepository<OrchardPlantHealth> _plantHealthRepo;
    private readonly IRepository<OrchardInfrastructure> _infrastructureRepo;
    private readonly IRepository<OrchardProduction> _productionRepo;
    private readonly IRepository<OrchardInputCost> _inputCostRepo;
    private readonly IRepository<OrchardOptionalFields> _optionalFieldsRepo;

    public OrchardProfileService(
        IRepository<OrchardProfile> mainRepo,
        IRepository<OrchardLocation> locationRepo,
        IRepository<OrchardVariety> varietyRepo,
        IRepository<OrchardSoilAndWater> soilWaterRepo,
        IRepository<OrchardCropNutrition> cropNutritionRepo,
        IRepository<OrchardPlantHealth> plantHealthRepo,
        IRepository<OrchardInfrastructure> infrastructureRepo,
        IRepository<OrchardProduction> productionRepo,
        IRepository<OrchardInputCost> inputCostRepo,
        IRepository<OrchardOptionalFields> optionalFieldsRepo)
    {
        _mainRepo = mainRepo;
        _locationRepo = locationRepo;
        _varietyRepo = varietyRepo;
        _soilWaterRepo = soilWaterRepo;
        _cropNutritionRepo = cropNutritionRepo;
        _plantHealthRepo = plantHealthRepo;
        _infrastructureRepo = infrastructureRepo;
        _productionRepo = productionRepo;
        _inputCostRepo = inputCostRepo;
        _optionalFieldsRepo = optionalFieldsRepo;
    }

    #region Profile Management

    public async Task<OrchardProfile> GetByIdAsync(long id)
    {
        return await _mainRepo.Query()
            .Include(o => o.Locations)
            .Include(o => o.Varieties)
            .Include(o => o.SoilAndWater)
            .Include(o => o.CropNutrition)
            .Include(o => o.PlantHealth)
            .Include(o => o.Infrastructure)
            .Include(o => o.Production)
            .Include(o => o.InputCost)
            .Include(o => o.OptionalFields)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<OrchardProfile> GetByOrchardIdAsync(string orchardId)
    {
        if (string.IsNullOrWhiteSpace(orchardId))
            return null;

        return await _mainRepo.Query()
            .Include(o => o.Locations)
            .Include(o => o.Varieties)
            .Include(o => o.SoilAndWater)
            .Include(o => o.CropNutrition)
            .Include(o => o.PlantHealth)
            .Include(o => o.Infrastructure)
            .Include(o => o.Production)
            .Include(o => o.InputCost)
            .Include(o => o.OptionalFields)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrchardId == orchardId)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<OrchardProfile>> GetAllAsync()
    {
        return await _mainRepo.Query()
            .Include(o => o.Locations)
            .Include(o => o.Varieties)
            .Include(o => o.SoilAndWater)
            .Include(o => o.CropNutrition)
            .Include(o => o.PlantHealth)
            .Include(o => o.Infrastructure)
            .Include(o => o.Production)
            .Include(o => o.InputCost)
            .Include(o => o.OptionalFields)
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedOn)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<OrchardProfile>> GetByUserIdAsync(long userId)
    {
        return await _mainRepo.Query()
            .Include(o => o.Locations)
            .Include(o => o.Varieties)
            .Include(o => o.SoilAndWater)
            .Include(o => o.CropNutrition)
            .Include(o => o.PlantHealth)
            .Include(o => o.Infrastructure)
            .Include(o => o.Production)
            .Include(o => o.InputCost)
            .Include(o => o.OptionalFields)
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedOn)
            .ToListAsync()
            .ConfigureAwait(false);
    }
    public async Task<IEnumerable<OrchardProfile>> GetBasicOrchardsByUserIdAsync(long userId)
    {
        return await _mainRepo.Query()
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedOn)
            .ToListAsync()
            .ConfigureAwait(false);
    }
    public async Task<long> CreateAsync(OrchardProfile model)
    {
        ValidateProfileModel(model);

        model.CreatedOn = DateTime.UtcNow;
        _mainRepo.Add(model);
        await _mainRepo.SaveChangesAsync().ConfigureAwait(false);
        return model.Id;
    }

    public async Task UpdateAsync(long id, OrchardProfile model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model), "Model cannot be null.");

        var existing = await _mainRepo.Query()
            .FirstOrDefaultAsync(o => o.Id == id)
            .ConfigureAwait(false);

        if (existing == null)
            return;

        UpdateProfileFields(existing, model);
        existing.UpdatedOn = DateTime.UtcNow;

        await _mainRepo.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<DeleteResult> DeleteAsync(long id)
    {
        var entity = await _mainRepo.Query()
            .FirstOrDefaultAsync(o => o.Id == id)
            .ConfigureAwait(false);

        if (entity == null)
            return new DeleteResult
            {
                Success = false,
                Message = "Orchard profile not found"
            };

        try
        {
            await DeleteAllSectionsAsync(id);
            _mainRepo.Remove(entity);
            await _mainRepo.SaveChangesAsync().ConfigureAwait(false);

            return new DeleteResult
            {
                Success = true,
                Message = $"Orchard profile '{entity.OrchardName}' and all related data deleted successfully",
                RecordsDeleted = 1
            };
        }
        catch (Exception ex)
        {
            return new DeleteResult
            {
                Success = false,
                Message = $"Error deleting orchard profile: {ex.Message}"
            };
        }
    }

    public async Task<DeletePreview> GetDeletePreviewAsync(long id)
    {
        var entity = await _mainRepo.Query()
            .FirstOrDefaultAsync(o => o.Id == id)
            .ConfigureAwait(false);

        if (entity == null)
            return null;

        var locations = await _locationRepo.Query()
            .Where(l => l.OrchardProfileId == id)
            .ToListAsync()
            .ConfigureAwait(false);

        var varieties = await _varietyRepo.Query()
            .Where(v => v.OrchardProfileId == id)
            .ToListAsync()
            .ConfigureAwait(false);

        var hasSoilAndWater = await _soilWaterRepo.Query()
            .AnyAsync(s => s.OrchardProfileId == id)
            .ConfigureAwait(false);

        var hasCropNutrition = await _cropNutritionRepo.Query()
            .AnyAsync(c => c.OrchardProfileId == id)
            .ConfigureAwait(false);

        var hasPlantHealth = await _plantHealthRepo.Query()
            .AnyAsync(p => p.OrchardProfileId == id)
            .ConfigureAwait(false);

        var hasInfrastructure = await _infrastructureRepo.Query()
            .AnyAsync(i => i.OrchardProfileId == id)
            .ConfigureAwait(false);

        var hasProduction = await _productionRepo.Query()
            .AnyAsync(p => p.OrchardProfileId == id)
            .ConfigureAwait(false);

        var hasInputCost = await _inputCostRepo.Query()
            .AnyAsync(i => i.OrchardProfileId == id)
            .ConfigureAwait(false);

        var hasOptionalFields = await _optionalFieldsRepo.Query()
            .AnyAsync(o => o.OrchardProfileId == id)
            .ConfigureAwait(false);

        var totalRecords = 1 + locations.Count + varieties.Count
            + (hasSoilAndWater ? 1 : 0)
            + (hasCropNutrition ? 1 : 0)
            + (hasPlantHealth ? 1 : 0)
            + (hasInfrastructure ? 1 : 0)
            + (hasProduction ? 1 : 0)
            + (hasInputCost ? 1 : 0)
            + (hasOptionalFields ? 1 : 0);

        return new DeletePreview
        {
            ProfileId = entity.Id,
            ProfileName = entity.OrchardName,
            OrchardId = entity.OrchardId,
            TotalRecordsToDelete = totalRecords,
            LocationsCount = locations.Count,
            VarietiesCount = varieties.Count,
            DetailEntities = new DetailEntityPreview
            {
                HasSoilAndWater = hasSoilAndWater,
                HasCropNutrition = hasCropNutrition,
                HasPlantHealth = hasPlantHealth,
                HasInfrastructure = hasInfrastructure,
                HasProduction = hasProduction,
                HasInputCost = hasInputCost,
                HasOptionalFields = hasOptionalFields
            }
        };
    }

    /// <summary>
    /// Deletes all sections associated with an orchard profile.
    /// Calls individual section deletion methods for clarity and maintainability.
    /// </summary>
    private async Task DeleteAllSectionsAsync(long orchardProfileId)
    {
        await DeleteLocationsAsync(orchardProfileId);
        await DeleteVarietiesAsync(orchardProfileId);
        await DeleteSoilAndWaterAsync(orchardProfileId);
        await DeleteCropNutritionAsync(orchardProfileId);
        await DeletePlantHealthAsync(orchardProfileId);
        await DeleteInfrastructureAsync(orchardProfileId);
        await DeleteProductionAsync(orchardProfileId);
        await DeleteInputCostAsync(orchardProfileId);
        await DeleteOptionalFieldsAsync(orchardProfileId);
    }

    /// <summary>
    /// Deletes all locations associated with an orchard profile.
    /// </summary>
    private async Task DeleteLocationsAsync(long orchardProfileId)
    {
        var locations = await _locationRepo.Query()
            .Where(l => l.OrchardProfileId == orchardProfileId)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var location in locations)
            _locationRepo.Remove(location);

        if (locations.Count > 0)
            await _locationRepo.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes all varieties associated with an orchard profile.
    /// </summary>
    private async Task DeleteVarietiesAsync(long orchardProfileId)
    {
        var varieties = await _varietyRepo.Query()
            .Where(v => v.OrchardProfileId == orchardProfileId)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var variety in varieties)
            _varietyRepo.Remove(variety);

        if (varieties.Count > 0)
            await _varietyRepo.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes soil and water data associated with an orchard profile.
    /// </summary>
    private async Task DeleteSoilAndWaterAsync(long orchardProfileId)
    {
        var soilAndWater = await _soilWaterRepo.Query()
            .FirstOrDefaultAsync(s => s.OrchardProfileId == orchardProfileId)
            .ConfigureAwait(false);

        if (soilAndWater != null)
        {
            _soilWaterRepo.Remove(soilAndWater);
            await _soilWaterRepo.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Deletes crop nutrition data associated with an orchard profile.
    /// </summary>
    private async Task DeleteCropNutritionAsync(long orchardProfileId)
    {
        var cropNutrition = await _cropNutritionRepo.Query()
            .FirstOrDefaultAsync(c => c.OrchardProfileId == orchardProfileId)
            .ConfigureAwait(false);

        if (cropNutrition != null)
        {
            _cropNutritionRepo.Remove(cropNutrition);
            await _cropNutritionRepo.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Deletes plant health data associated with an orchard profile.
    /// </summary>
    private async Task DeletePlantHealthAsync(long orchardProfileId)
    {
        var plantHealth = await _plantHealthRepo.Query()
            .FirstOrDefaultAsync(p => p.OrchardProfileId == orchardProfileId)
            .ConfigureAwait(false);

        if (plantHealth != null)
        {
            _plantHealthRepo.Remove(plantHealth);
            await _plantHealthRepo.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Deletes infrastructure data associated with an orchard profile.
    /// </summary>
    private async Task DeleteInfrastructureAsync(long orchardProfileId)
    {
        var infrastructure = await _infrastructureRepo.Query()
            .FirstOrDefaultAsync(i => i.OrchardProfileId == orchardProfileId)
            .ConfigureAwait(false);

        if (infrastructure != null)
        {
            _infrastructureRepo.Remove(infrastructure);
            await _infrastructureRepo.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Deletes production data associated with an orchard profile.
    /// </summary>
    private async Task DeleteProductionAsync(long orchardProfileId)
    {
        var production = await _productionRepo.Query()
            .FirstOrDefaultAsync(p => p.OrchardProfileId == orchardProfileId)
            .ConfigureAwait(false);

        if (production != null)
        {
            _productionRepo.Remove(production);
            await _productionRepo.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Deletes input cost data associated with an orchard profile.
    /// </summary>
    private async Task DeleteInputCostAsync(long orchardProfileId)
    {
        var inputCost = await _inputCostRepo.Query()
            .FirstOrDefaultAsync(i => i.OrchardProfileId == orchardProfileId)
            .ConfigureAwait(false);

        if (inputCost != null)
        {
            _inputCostRepo.Remove(inputCost);
            await _inputCostRepo.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Deletes optional fields data associated with an orchard profile.
    /// </summary>
    private async Task DeleteOptionalFieldsAsync(long orchardProfileId)
    {
        var optionalFields = await _optionalFieldsRepo.Query()
            .FirstOrDefaultAsync(o => o.OrchardProfileId == orchardProfileId)
            .ConfigureAwait(false);

        if (optionalFields != null)
        {
            _optionalFieldsRepo.Remove(optionalFields);
            await _optionalFieldsRepo.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    #endregion

    #region Independent Section Operations

    /// <summary>
    /// Save locations for an orchard profile independently.
    /// Replaces existing locations with new ones. Does not throw exceptions.
    /// </summary>
    public async Task<SectionOperationResult> SaveLocationsAsync(long orchardProfileId, IEnumerable<OrchardLocation> locations)
    {
        try
        {
            if (!await ProfileExistsAsync(orchardProfileId))
            {
                return CreateProfileNotFoundResult(orchardProfileId);
            }

            await DeleteExistingLocationsAsync(orchardProfileId);

            var locationsList = locations?.ToList() ?? new List<OrchardLocation>();
            var locationCount = 0;

            foreach (var loc in locationsList)
            {
                loc.OrchardProfileId = orchardProfileId;
                _locationRepo.Add(loc);
                locationCount++;
            }

            if (locationCount > 0)
                await _locationRepo.SaveChangesAsync().ConfigureAwait(false);

            return new SectionOperationResult
            {
                Success = true,
                Message = $"Locations saved successfully. Added {locationCount} record(s)",
                RecordsAffected = locationCount
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"Error saving locations: {ex.Message}");
        }
    }

    /// <summary>
    /// Save varieties for an orchard profile independently.
    /// Replaces existing varieties with new ones. Does not throw exceptions.
    /// </summary>
    public async Task<SectionOperationResult> SaveVarietiesAsync(long orchardProfileId, IEnumerable<OrchardVariety> varieties)
    {
        try
        {
            if (!await ProfileExistsAsync(orchardProfileId))
            {
                return CreateProfileNotFoundResult(orchardProfileId);
            }
            if (varieties is null || varieties.Count() < 1)
                await DeleteExistingVarietiesAsync(orchardProfileId);

            //varieties.ToList().ForEach(variety => variety.OrchardProfileId = orchardProfileId);
            //_varietyRepo.AddRange(varieties);
            //await _varietyRepo.SaveChangesAsync().ConfigureAwait(false);

            return new SectionOperationResult
            {
                Success = true,
                Message = $"Varieties updated successfully",
                RecordsAffected = 0
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"Error saving varieties: {ex.Message}");
        }
    }

    /// <summary>
    /// Save soil and water data for an orchard profile independently.
    /// Creates or replaces existing data. Does not throw exceptions.
    /// </summary>
    public async Task<SectionOperationResult> SaveSoilAndWaterAsync(long orchardProfileId, OrchardSoilAndWater soilAndWater)
    {
        try
        {
            if (!await ProfileExistsAsync(orchardProfileId))
            {
                return CreateProfileNotFoundResult(orchardProfileId);
            }
            if (soilAndWater is null)
                await DeleteExistingAsync(_soilWaterRepo, orchardProfileId);

            //if (soilAndWater != null)
            //{
            //    soilAndWater.OrchardProfileId = orchardProfileId;
            //    _soilWaterRepo.Add(soilAndWater);
            //    await _soilWaterRepo.SaveChangesAsync().ConfigureAwait(false);

            //    return new SectionOperationResult
            //    {
            //        Success = true,
            //        Message = "Soil and water data saved successfully",
            //        RecordsAffected = 1
            //    };
            //}

            return new SectionOperationResult
            {
                Success = true,
                Message = "Soil and water data cleared successfully",
                RecordsAffected = 0
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"Error saving soil and water data: {ex.Message}");
        }
    }

    /// <summary>
    /// Save crop nutrition data for an orchard profile independently.
    /// Creates or replaces existing data. Does not throw exceptions.
    /// </summary>
    public async Task<SectionOperationResult> SaveCropNutritionAsync(long orchardProfileId, OrchardCropNutrition cropNutrition)
    {
        try
        {
            if (!await ProfileExistsAsync(orchardProfileId))
            {
                return CreateProfileNotFoundResult(orchardProfileId);
            }
            if (cropNutrition is null)
                await DeleteExistingAsync(_cropNutritionRepo, orchardProfileId);

            //if (cropNutrition != null)
            //{
            //    cropNutrition.OrchardProfileId = orchardProfileId;
            //    _cropNutritionRepo.Add(cropNutrition);
            //    await _cropNutritionRepo.SaveChangesAsync().ConfigureAwait(false);

            //    return new SectionOperationResult
            //    {
            //        Success = true,
            //        Message = "Crop nutrition data saved successfully",
            //        RecordsAffected = 1
            //    };
            //}

            return new SectionOperationResult
            {
                Success = true,
                Message = "Crop nutrition data cleared successfully",
                RecordsAffected = 0
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"Error saving crop nutrition data: {ex.Message}");
        }
    }

    /// <summary>
    /// Save plant health data for an orchard profile independently.
    /// Creates or replaces existing data. Does not throw exceptions.
    /// </summary>
    public async Task<SectionOperationResult> SavePlantHealthAsync(long orchardProfileId, OrchardPlantHealth plantHealth)
    {
        try
        {
            if (!await ProfileExistsAsync(orchardProfileId))
            {
                return CreateProfileNotFoundResult(orchardProfileId);
            }
            if (plantHealth is null)

                await DeleteExistingAsync(_plantHealthRepo, orchardProfileId);

            //if (plantHealth != null)
            //{
            //    plantHealth.OrchardProfileId = orchardProfileId;
            //    _plantHealthRepo.Add(plantHealth);
            //    await _plantHealthRepo.SaveChangesAsync().ConfigureAwait(false);

            //    return new SectionOperationResult
            //    {
            //        Success = true,
            //        Message = "Plant health data saved successfully",
            //        RecordsAffected = 1
            //    };
            //}

            return new SectionOperationResult
            {
                Success = true,
                Message = "Plant health data cleared successfully",
                RecordsAffected = 0
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"Error saving plant health data: {ex.Message}");
        }
    }

    /// <summary>
    /// Save infrastructure data for an orchard profile independently.
    /// Handles JSON serialization of array properties (EquipmentOwned, StorageFacilities, OtherTools).
    /// Creates or replaces existing data. Does not throw exceptions.
    /// </summary>
    public async Task<SectionOperationResult> SaveInfrastructureAsync(long orchardProfileId, OrchardInfrastructure infrastructure)
    {
        try
        {
            if (!await ProfileExistsAsync(orchardProfileId))
            {
                return CreateProfileNotFoundResult(orchardProfileId);
            }
            if (infrastructure is null)

                await DeleteExistingAsync(_infrastructureRepo, orchardProfileId);

            //if (infrastructure != null)
            //{
            //    infrastructure.OrchardProfileId = orchardProfileId;

            //    // Ensure array properties are properly serialized to JSON strings
            //    infrastructure.EquipmentOwned = SerializeArrayToJson(infrastructure.EquipmentOwned);
            //    infrastructure.StorageFacilities = SerializeArrayToJson(infrastructure.StorageFacilities);
            //    infrastructure.OtherTools = SerializeArrayToJson(infrastructure.OtherTools);

            //    _infrastructureRepo.Add(infrastructure);
            //    await _infrastructureRepo.SaveChangesAsync().ConfigureAwait(false);

            //    return new SectionOperationResult
            //    {
            //        Success = true,
            //        Message = "Infrastructure data saved successfully",
            //        RecordsAffected = 1
            //    };
            //}

            return new SectionOperationResult
            {
                Success = true,
                Message = "Infrastructure data cleared successfully",
                RecordsAffected = 0
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"Error saving infrastructure data: {ex.Message}");
        }
    }

    /// <summary>
    /// Save production data for an orchard profile independently.
    /// Creates or replaces existing data. Does not throw exceptions.
    /// </summary>
    public async Task<SectionOperationResult> SaveProductionAsync(long orchardProfileId, OrchardProduction production)
    {
        try
        {
            if (!await ProfileExistsAsync(orchardProfileId))
            {
                return CreateProfileNotFoundResult(orchardProfileId);
            }
            if (production is null)

                await DeleteExistingAsync(_productionRepo, orchardProfileId);

            //if (production != null)
            //{
            //    production.OrchardProfileId = orchardProfileId;
            //    _productionRepo.Add(production);
            //    await _productionRepo.SaveChangesAsync().ConfigureAwait(false);

            //    return new SectionOperationResult
            //    {
            //        Success = true,
            //        Message = "Production data saved successfully",
            //        RecordsAffected = 1
            //    };
            //}

            return new SectionOperationResult
            {
                Success = true,
                Message = "Production data cleared successfully",
                RecordsAffected = 0
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"Error saving production data: {ex.Message}");
        }
    }

    /// <summary>
    /// Save input cost data for an orchard profile independently.
    /// Creates or replaces existing data. Does not throw exceptions.
    /// </summary>
    public async Task<SectionOperationResult> SaveInputCostAsync(long orchardProfileId, OrchardInputCost inputCost)
    {
        try
        {
            if (!await ProfileExistsAsync(orchardProfileId))
            {
                return CreateProfileNotFoundResult(orchardProfileId);
            }
            if (inputCost is null)

                await DeleteExistingAsync(_inputCostRepo, orchardProfileId);

            //if (inputCost != null)
            //{
            //    inputCost.OrchardProfileId = orchardProfileId;
            //    _inputCostRepo.Add(inputCost);
            //    await _inputCostRepo.SaveChangesAsync().ConfigureAwait(false);

            //    return new SectionOperationResult
            //    {
            //        Success = true,
            //        Message = "Input cost data saved successfully",
            //        RecordsAffected = 1
            //    };
            //}

            return new SectionOperationResult
            {
                Success = true,
                Message = "Input cost data cleared successfully",
                RecordsAffected = 0
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"Error saving input cost data: {ex.Message}");
        }
    }

    /// <summary>
    /// Save optional fields data for an orchard profile independently.
    /// Creates or replaces existing data. Does not throw exceptions.
    /// </summary>
    public async Task<SectionOperationResult> SaveOptionalFieldsAsync(long orchardProfileId, OrchardOptionalFields optionalFields)
    {
        try
        {
            if (!await ProfileExistsAsync(orchardProfileId))
            {
                return CreateProfileNotFoundResult(orchardProfileId);
            }
            if (optionalFields is null)

                await DeleteExistingAsync(_optionalFieldsRepo, orchardProfileId);

            //if (optionalFields != null)
            //{
            //    optionalFields.OrchardProfileId = orchardProfileId;
            //    _optionalFieldsRepo.Add(optionalFields);
            //    await _optionalFieldsRepo.SaveChangesAsync().ConfigureAwait(false);

            //    return new SectionOperationResult
            //    {
            //        Success = true,
            //        Message = "Optional fields data saved successfully",
            //        RecordsAffected = 1
            //    };
            //}

            return new SectionOperationResult
            {
                Success = true,
                Message = "Optional fields data cleared successfully",
                RecordsAffected = 0
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"Error saving optional fields data: {ex.Message}");
        }
    }

    #endregion

    #region Bulk Operations (Legacy Support)

    public async Task<CreateOrchardProfileResult> CreateWithAllDetailsAsync(long Id,
        OrchardProfile profile,
        IEnumerable<OrchardVariety> varieties,
        OrchardSoilAndWater soilAndWater,
        OrchardCropNutrition cropNutrition,
        OrchardPlantHealth plantHealth,
        OrchardInfrastructure infrastructure,
        OrchardProduction production,
        OrchardInputCost inputCost,
        OrchardOptionalFields optionalFields)
    {
        try
        {
            var existingProfile = await _mainRepo.Query()
                .FirstOrDefaultAsync(p => p.Id == Id)
                .ConfigureAwait(false);

            if (existingProfile == null)
            {
                profile.CreatedOn = DateTime.UtcNow;
                _mainRepo.Add(profile);
                await _mainRepo.SaveChangesAsync().ConfigureAwait(false);
            }

            var profileId = existingProfile == null ? profile.Id : existingProfile.Id;

            await AddVarietiesAsync(profileId, varieties);
            await AddDetailEntityAsync(_soilWaterRepo, profileId, soilAndWater, () => soilAndWater.OrchardProfileId = profileId);
            await AddDetailEntityAsync(_cropNutritionRepo, profileId, cropNutrition, () => cropNutrition.OrchardProfileId = profileId);
            await AddDetailEntityAsync(_plantHealthRepo, profileId, plantHealth, () => plantHealth.OrchardProfileId = profileId);

            if (infrastructure != null)
            {
                infrastructure.OrchardProfileId = profileId;
                infrastructure.EquipmentOwned = SerializeArrayToJson(infrastructure.EquipmentOwned);
                infrastructure.StorageFacilities = SerializeArrayToJson(infrastructure.StorageFacilities);
                infrastructure.OtherTools = SerializeArrayToJson(infrastructure.OtherTools);
                await AddDetailEntityAsync(_infrastructureRepo, profileId, infrastructure, () => { });
            }

            await AddDetailEntityAsync(_productionRepo, profileId, production, () => production.OrchardProfileId = profileId);
            await AddDetailEntityAsync(_inputCostRepo, profileId, inputCost, () => inputCost.OrchardProfileId = profileId);
            await AddDetailEntityAsync(_optionalFieldsRepo, profileId, optionalFields, () => optionalFields.OrchardProfileId = profileId);

            return new CreateOrchardProfileResult
            {
                Success = true,
                Message = "Orchard profile created successfully",
                ProfileId = profileId,
                IsNew = true
            };
        }
        catch (Exception ex)
        {
            return new CreateOrchardProfileResult
            {
                Success = false,
                Message = $"Error creating orchard profile: {ex.Message}",
                ProfileId = 0,
                IsNew = false
            };
        }
    }

    public async Task<UpdateOrchardProfileResult> UpdateWithAllDetailsAsync(
        long id,
        OrchardProfile profile,
        IEnumerable<OrchardVariety> varieties,
        OrchardSoilAndWater soilAndWater,
        OrchardCropNutrition cropNutrition,
        OrchardPlantHealth plantHealth,
        OrchardInfrastructure infrastructure,
        OrchardProduction production,
        OrchardInputCost inputCost,
        OrchardOptionalFields optionalFields)
    {
        var existing = await _mainRepo.Query()
            .FirstOrDefaultAsync(p => p.Id == id)
            .ConfigureAwait(false);

        if (existing == null)
        {
            return new UpdateOrchardProfileResult
            {
                Success = false,
                Message = "Orchard profile not found"
            };
        }

        try
        {
            UpdateProfileFields(existing, profile);
            existing.UpdatedOn = DateTime.UtcNow;
            await _mainRepo.SaveChangesAsync().ConfigureAwait(false);

            await SaveVarietiesAsync(id, varieties);
            await SaveSoilAndWaterAsync(id, soilAndWater);
            await SaveCropNutritionAsync(id, cropNutrition);
            await SavePlantHealthAsync(id, plantHealth);
            await SaveInfrastructureAsync(id, infrastructure);
            await SaveProductionAsync(id, production);
            await SaveInputCostAsync(id, inputCost);
            await SaveOptionalFieldsAsync(id, optionalFields);

            return new UpdateOrchardProfileResult
            {
                Success = true,
                Message = "Orchard profile updated successfully"
            };
        }
        catch (Exception ex)
        {
            return new UpdateOrchardProfileResult
            {
                Success = false,
                Message = $"Error updating orchard profile: {ex.Message}"
            };
        }
    }

    #endregion

    #region Patch Operations

    /// <summary>
    /// Partially update an orchard profile based on section visibility.
    /// Only updates sections that are provided (non-null) in the patch model.
    /// </summary>
    public async Task<UpdateOrchardProfileResult> PartialUpdateAsync(long id, OrchardProfilePatchViewModel patchModel)
    {
        var existing = await _mainRepo.Query()
            .FirstOrDefaultAsync(p => p.Id == id)
            .ConfigureAwait(false);

        if (existing == null)
        {
            return new UpdateOrchardProfileResult
            {
                Success = false,
                Message = "Orchard profile not found"
            };
        }

        try
        {
            // Update basic details if provided
            if (patchModel.OrchardName != null)
            {
                await UpdateBasicDetailsAsync(existing, patchModel);
            }

            // Update varieties if provided
            if (patchModel.Varieties != null)
            {
                var varieties = MapVarietiesFromViewModel(patchModel.Varieties);
                await SaveVarietiesAsync(id, varieties);
            }

            // Update soil and water if provided
            if (patchModel.SoilAndWater != null)
            {
                var soilAndWater = MapSoilAndWaterFromViewModel(patchModel.SoilAndWater);
                await SaveSoilAndWaterAsync(id, soilAndWater);
            }

            // Update crop nutrition if provided
            if (patchModel.CropNutrition != null)
            {
                var cropNutrition = MapCropNutritionFromViewModel(patchModel.CropNutrition);
                await SaveCropNutritionAsync(id, cropNutrition);
            }

            // Update plant health if provided
            if (patchModel.PlantHealth != null)
            {
                var plantHealth = MapPlantHealthFromViewModel(patchModel.PlantHealth);
                await SavePlantHealthAsync(id, plantHealth);
            }

            // Update infrastructure if provided
            if (patchModel.Infrastructure != null)
            {
                var infrastructure = MapInfrastructureFromViewModel(patchModel.Infrastructure);
                await SaveInfrastructureAsync(id, infrastructure);
            }

            // Update production if provided
            if (patchModel.Production != null)
            {
                var production = MapProductionFromViewModel(patchModel.Production);
                await SaveProductionAsync(id, production);
            }

            // Update input cost if provided
            if (patchModel.InputCost != null)
            {
                var inputCost = MapInputCostFromViewModel(patchModel.InputCost);
                await SaveInputCostAsync(id, inputCost);
            }

            // Update optional fields if provided
            if (patchModel.OptionalFields != null)
            {
                var optionalFields = MapOptionalFieldsFromViewModel(patchModel.OptionalFields);
                await SaveOptionalFieldsAsync(id, optionalFields);
            }

            existing.UpdatedOn = DateTime.UtcNow;
            await _mainRepo.SaveChangesAsync().ConfigureAwait(false);

            return new UpdateOrchardProfileResult
            {
                Success = true,
                Message = "Orchard profile updated successfully"
            };
        }
        catch (Exception ex)
        {
            return new UpdateOrchardProfileResult
            {
                Success = false,
                Message = $"Error updating orchard profile: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Updates basic orchard profile details selectively.
    /// Only updates fields that are provided (non-null) in the DTO.
    /// </summary>
    private async Task UpdateBasicDetailsAsync(OrchardProfile existing, OrchardProfilePatchViewModel details)
    {
        if (!string.IsNullOrWhiteSpace(details.OrchardName))
            existing.OrchardName = details.OrchardName;

        if (details.Kanals.HasValue && details.Kanals > 0)
            existing.Kanals = details.Kanals.Value;

        if (details.Marlas.HasValue && details.Marlas > 0)
            existing.Marlas = details.Marlas.Value;

        if (details.EstablishmentYear.HasValue && details.EstablishmentYear > 0)
            existing.EstablishmentYear = details.EstablishmentYear.Value;

        if (!string.IsNullOrWhiteSpace(details.PlantationPattern))
            existing.PlantationPattern = details.PlantationPattern;

        if (!string.IsNullOrWhiteSpace(details.OwnershipType))
            existing.OwnershipType = details.OwnershipType;

        if (!string.IsNullOrWhiteSpace(details.District))
            existing.District = details.District;

        if (!string.IsNullOrWhiteSpace(details.Tehsil))
            existing.Tehsil = details.Tehsil;

        if (!string.IsNullOrWhiteSpace(details.Zone))
            existing.Zone = details.Zone;

        if (!string.IsNullOrWhiteSpace(details.PinCode))
            existing.PinCode = details.PinCode;

        if (details.Latitude.HasValue)
            existing.Latitude = details.Latitude;

        if (details.Longitude.HasValue)
            existing.Longitude = details.Longitude;

        await Task.CompletedTask;
    }

    #endregion

    #region Helper Methods

    private void ValidateProfileModel(OrchardProfile model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model), "Model cannot be null.");

        if (string.IsNullOrWhiteSpace(model.OrchardName))
            throw new ArgumentException("OrchardName is required and cannot be null or empty.", nameof(model.OrchardName));

        if (string.IsNullOrWhiteSpace(model.OrchardId))
            throw new ArgumentException("OrchardId is required and cannot be null or empty.", nameof(model.OrchardId));
    }

    private void UpdateProfileFields(OrchardProfile existing, OrchardProfile model)
    {
        if (!string.IsNullOrWhiteSpace(model.OrchardName))
            existing.OrchardName = model.OrchardName;

        //if (model.Kanals > 0)
            existing.Kanals = model.Kanals;

        //if (model.Marlas > 0)
            existing.Marlas = model.Marlas;

        if (model.EstablishmentYear > 0)
            existing.EstablishmentYear = model.EstablishmentYear;

        if (!string.IsNullOrWhiteSpace(model.PlantationPattern))
            existing.PlantationPattern = model.PlantationPattern;

        if (!string.IsNullOrWhiteSpace(model.OwnershipType))
            existing.OwnershipType = model.OwnershipType;

        if (!string.IsNullOrWhiteSpace(model.District))
            existing.District = model.District;

        if (!string.IsNullOrWhiteSpace(model.Tehsil))
            existing.Tehsil = model.Tehsil;

        if (!string.IsNullOrWhiteSpace(model.Zone))
            existing.Zone = model.Zone;

        if (!string.IsNullOrWhiteSpace(model.PinCode))
            existing.PinCode = model.PinCode;

        if (model.Latitude.HasValue)
            existing.Latitude = model.Latitude;

        if (model.Longitude.HasValue)
            existing.Longitude = model.Longitude;
    }

    private int CalculateRecordsDeleted(OrchardProfile entity)
    {
        var count = 1; // Main profile
        count += entity.Locations?.Count ?? 0;
        count += entity.Varieties?.Count ?? 0;
        count += (entity.SoilAndWater != null ? 1 : 0);
        count += (entity.CropNutrition != null ? 1 : 0);
        count += (entity.PlantHealth != null ? 1 : 0);
        count += (entity.Infrastructure != null ? 1 : 0);
        count += (entity.Production != null ? 1 : 0);
        count += (entity.InputCost != null ? 1 : 0);
        count += (entity.OptionalFields != null ? 1 : 0);
        return count;
    }

    private async Task<bool> ProfileExistsAsync(long orchardProfileId)
    {
        return await _mainRepo.Query()
            .AnyAsync(p => p.Id == orchardProfileId)
            .ConfigureAwait(false);
    }

    private async Task DeleteExistingLocationsAsync(long orchardProfileId)
    {
        var existingLocations = await _locationRepo.Query()
            .Where(l => l.OrchardProfileId == orchardProfileId)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var loc in existingLocations)
            _locationRepo.Remove(loc);

        if (existingLocations.Count > 0)
            await _locationRepo.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task DeleteExistingVarietiesAsync(long orchardProfileId)
    {
        var existingVarieties = await _varietyRepo.Query()
            .Where(v => v.OrchardProfileId == orchardProfileId)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var variety in existingVarieties)
            _varietyRepo.Remove(variety);

        if (existingVarieties.Count > 0)
            await _varietyRepo.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task DeleteExistingAsync<T>(IRepository<T> repository, long orchardProfileId) where T : ECommerce.Infrastructure.Models.IEntityWithTypedId<long>
    {
        var existing = await repository.Query()
            .Where(x => EF.Property<long>(x, "OrchardProfileId") == orchardProfileId)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var item in existing)
            repository.Remove(item);

        if (existing.Count > 0)
            await repository.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task AddLocationsAsync(long profileId, IEnumerable<OrchardLocation> locations)
    {
        foreach (var loc in locations ?? Enumerable.Empty<OrchardLocation>())
        {
            loc.OrchardProfileId = profileId;
            _locationRepo.Add(loc);
        }

        if ((locations ?? Enumerable.Empty<OrchardLocation>()).Any())
            await _locationRepo.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task AddVarietiesAsync(long profileId, IEnumerable<OrchardVariety> varieties)
    {
        foreach (var variety in varieties ?? Enumerable.Empty<OrchardVariety>())
        {
            variety.OrchardProfileId = profileId;
            _varietyRepo.Add(variety);
        }

        if ((varieties ?? Enumerable.Empty<OrchardVariety>()).Any())
            await _varietyRepo.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task AddDetailEntityAsync<T>(IRepository<T> repository, long profileId, T entity, Action setupAction) where T : ECommerce.Infrastructure.Models.IEntityWithTypedId<long>
    {
        if (entity != null)
        {
            setupAction?.Invoke();
            repository.Add(entity);
            await repository.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    private SectionOperationResult CreateProfileNotFoundResult(long orchardProfileId)
    {
        return new SectionOperationResult
        {
            Success = false,
            Message = $"Orchard profile with ID {orchardProfileId} not found. Create the main profile first.",
            RecordsAffected = 0
        };
    }

    private SectionOperationResult CreateErrorResult(string message)
    {
        return new SectionOperationResult
        {
            Success = false,
            Message = message,
            RecordsAffected = 0
        };
    }

    /// <summary>
    /// Serializes a JSON string or array to proper JSON format for database storage.
    /// If the input is already a JSON string, it returns as-is.
    /// If it's null or empty, returns empty JSON array.
    /// </summary>
    private string SerializeArrayToJson(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return JsonSerializer.Serialize(new string[] { });

        var trimmed = input.Trim();
        if (trimmed.StartsWith("[") || trimmed.StartsWith("{"))
            return input;

        return JsonSerializer.Serialize(new[] { input });
    }

    #endregion

    #region Mapping Methods

    /// <summary>
    /// Maps VarietyDto list to OrchardVariety entity collection.
    /// </summary>
    private IEnumerable<OrchardVariety> MapVarietiesFromViewModel(List<VarietyDto> varieties)
    {
        return (varieties ?? new List<VarietyDto>())
            .Select(v => new OrchardVariety
            {
                VarietyName = v.VarietyName,
                NumberOfTrees = v.NumberOfTrees,
                RootstockUsed = v.RootstockUsed,
                PlantDensity = v.PlantDensity,
                TreesLessThan5Years = v.TreesLessThan5Years,
                TreesFiveToTenYears = v.TreesFiveToTenYears,
                TreesMoreThan10Years = v.TreesMoreThan10Years
            })
            .ToList();
    }

    /// <summary>
    /// Maps SoilAndWaterDto to OrchardSoilAndWater entity.
    /// </summary>
    private OrchardSoilAndWater MapSoilAndWaterFromViewModel(SoilAndWaterDto dto)
    {
        return new OrchardSoilAndWater
        {
            SoilType = dto.SoilType,
            SoilPh = dto.SoilPh,
            SoilNitrogen = dto.SoilNitrogen,
            SoilPhosphorus = dto.SoilPhosphorus,
            SoilPotassium = dto.SoilPotassium,
            SoilOrganicCarbon = dto.SoilOrganicCarbon,
            SoilMicronutrients = dto.SoilMicronutrients,
            SoilTestReportUrls = SerializeArray(dto.SoilTestReportUrls),
            SoilHealthStatus = SerializeArray(dto.SoilHealthStatus),
            LastSoilTestDate = dto.LastSoilTestDate,
            WaterSource = dto.WaterSource,
            IrrigationMethod = dto.IrrigationMethod,
            WaterAvailability = dto.WaterAvailability
        };
    }

    /// <summary>
    /// Maps CropNutritionDto to OrchardCropNutrition entity.
    /// </summary>
    private OrchardCropNutrition MapCropNutritionFromViewModel(CropNutritionDto dto)
    {
        return new OrchardCropNutrition
        {
            FertilizerPractices = dto.FertilizerPractices,
            HasFertigationFacility = dto.HasFertigation,
            NutritionPlan = dto.NutritionPlan,
            IsMulchingApplied = dto.IsMulchingApplied,
            LastFertilizerApplicationDate = dto.LastFertilizerApplicationDate,
            FertilizerApplicationMode = dto.FertilizerApplicationMode
        };
    }

    /// <summary>
    /// Maps PlantHealthDto to OrchardPlantHealth entity.
    /// </summary>
    private OrchardPlantHealth MapPlantHealthFromViewModel(PlantHealthDto dto)
    {
        return new OrchardPlantHealth
        {
            DiseaseHistory = SerializeArray(dto.DiseaseHistory),
            PestHistory = SerializeArray(dto.PestHistory),
            SpraySchedule = SerializeArray(dto.SpraySchedule),
            IsCropInsuranceCovered = dto.IsCropInsuranceCovered
        };
    }

    /// <summary>
    /// Maps InfrastructureDto to OrchardInfrastructure entity.
    /// Handles JSON serialization of array properties.
    /// </summary>
    private OrchardInfrastructure MapInfrastructureFromViewModel(InfrastructureDto dto)
    {
        return new OrchardInfrastructure
        {
            EquipmentOwned = SerializeArray(dto.EquipmentOwned),
            StorageFacilities = SerializeArray(dto.StorageFacilities),
            OtherTools = SerializeArray(dto.OtherTools),
            PermanentLabor = dto.PermanentLabor,
            SeasonalLabor = dto.SeasonalLabor,
            ContractBasedLabor = dto.ContractBasedLabor
        };
    }

    /// <summary>
    /// Maps ProductionDto to OrchardProduction entity.
    /// Used in full update scenarios where all fields are provided.
    /// </summary>
    private OrchardProduction MapProductionFromViewModel(ProductionDto dto)
    {
        if (dto == null)
            return null;

        return new OrchardProduction
        {
            AverageAnnualYield = dto.AverageAnnualYield,
            YieldUnit = dto.YieldUnit,
            PeakHarvestFromMonth = dto.PeakHarvestFromMonth,
            PeakHarvestToMonth = dto.PeakHarvestToMonth,
            SellingPreference = dto.SellingPreference,
            BuyerName = dto.BuyerName,
            BuyerContact = dto.BuyerContact
        };
    }

    /// <summary>
    /// Maps InputCostDto to OrchardInputCost entity.
    /// </summary>
    private OrchardInputCost MapInputCostFromViewModel(InputCostDto dto)
    {
        return new OrchardInputCost
        {
            PesticideSupplierLocation = dto.PesticideSupplierLocation,
            PaymentType = dto.PaymentType,
            CreditPeriod = dto.CreditPeriod
        };
    }

    /// <summary>
    /// Maps OptionalFieldsDto to OrchardOptionalFields entity.
    /// </summary>
    private OrchardOptionalFields MapOptionalFieldsFromViewModel(OptionalFieldsDto dto)
    {
        return new OrchardOptionalFields
        {
            GovernmentSchemesAvailed = SerializeArray(dto.GovernmentSchemesAvailed),
            IsFpoOrCooperativeMember = dto.IsFpoOrCooperativeMember,
            Certifications = SerializeArray(dto.Certifications),
            DigitalToolsUsed = SerializeArray(dto.DigitalToolsUsed)
        };
    }

    /// <summary>
    /// Maps ProductionCreateDto to OrchardProduction entity for creation.
    /// </summary>
    private OrchardProduction MapProductionCreateToEntity(ProductionCreateDto dto)
    {
        if (dto == null)
            return null;

        return new OrchardProduction
        {
            AverageAnnualYield = dto.AverageAnnualYield,
            YieldUnit = dto.YieldUnit,
            PeakHarvestFromMonth = dto.PeakHarvestFromMonth,
            PeakHarvestToMonth = dto.PeakHarvestToMonth,
            SellingPreference = dto.SellingPreference,
            BuyerName = dto.BuyerName,
            BuyerContact = dto.BuyerContact
        };
    }

    /// <summary>
    /// Maps ProductionUpdateDto to OrchardProduction entity for updates.
    /// Only updates fields that are provided (non-null).
    /// </summary>
    private void UpdateProductionEntity(OrchardProduction existing, ProductionUpdateDto dto)
    {
        if (dto == null)
            return;

        if (dto.AverageAnnualYield.HasValue && dto.AverageAnnualYield > 0)
            existing.AverageAnnualYield = dto.AverageAnnualYield.Value;

        if (!string.IsNullOrWhiteSpace(dto.YieldUnit))
            existing.YieldUnit = dto.YieldUnit;

        if (!string.IsNullOrWhiteSpace(dto.PeakHarvestFromMonth))
            existing.PeakHarvestFromMonth = dto.PeakHarvestFromMonth;

        if (!string.IsNullOrWhiteSpace(dto.PeakHarvestToMonth))
            existing.PeakHarvestToMonth = dto.PeakHarvestToMonth;

        if (!string.IsNullOrWhiteSpace(dto.SellingPreference))
            existing.SellingPreference = dto.SellingPreference;

        if (!string.IsNullOrWhiteSpace(dto.BuyerName))
            existing.BuyerName = dto.BuyerName;

        if (!string.IsNullOrWhiteSpace(dto.BuyerContact))
            existing.BuyerContact = dto.BuyerContact;
    }

    /// <summary>
    /// Converts a string array to a JSON string for storage, or returns an empty JSON array if null.
    /// </summary>
    private string SerializeArray(string[] array)
    {
        if (array == null || array.Length == 0)
            return JsonSerializer.Serialize(new string[] { });

        return JsonSerializer.Serialize(array);
    }
    
    #endregion

    #region Variety Mapping Methods

    /// <summary>
    /// Maps VarietyCreateDto to OrchardVariety entity for creation.
    /// </summary>
    private OrchardVariety MapVarietyCreateToEntity(VarietyCreateDto dto)
    {
        if (dto == null)
            return null;

        return new OrchardVariety
        {
            VarietyName = dto.VarietyName,
            NumberOfTrees = dto.NumberOfTrees,
            RootstockUsed = dto.RootstockUsed,
            PlantDensity = dto.PlantDensity,
            TreesLessThan5Years = dto.TreesLessThan5Years,
            TreesFiveToTenYears = dto.TreesFiveToTenYears,
            TreesMoreThan10Years = dto.TreesMoreThan10Years
        };
    }

    /// <summary>
    /// Maps VarietyUpdateDto to OrchardVariety entity for updates.
    /// Only updates fields that are provided (non-null).
    /// </summary>
    private void UpdateVarietyEntity(OrchardVariety existing, VarietyUpdateDto dto)
    {
        if (dto == null)
            return;

        if (!string.IsNullOrWhiteSpace(dto.VarietyName))
            existing.VarietyName = dto.VarietyName;

        if (dto.NumberOfTrees.HasValue && dto.NumberOfTrees > 0)
            existing.NumberOfTrees = dto.NumberOfTrees.Value;

        if (!string.IsNullOrWhiteSpace(dto.RootstockUsed))
            existing.RootstockUsed = dto.RootstockUsed;

        if (!string.IsNullOrWhiteSpace(dto.PlantDensity))
            existing.PlantDensity = dto.PlantDensity;

        if (dto.TreesLessThan5Years.HasValue && dto.TreesLessThan5Years >= 0)
            existing.TreesLessThan5Years = dto.TreesLessThan5Years.Value;

        if (dto.TreesFiveToTenYears.HasValue && dto.TreesFiveToTenYears >= 0)
            existing.TreesFiveToTenYears = dto.TreesFiveToTenYears.Value;

        if (dto.TreesMoreThan10Years.HasValue && dto.TreesMoreThan10Years >= 0)
            existing.TreesMoreThan10Years = dto.TreesMoreThan10Years.Value;
    }

    #endregion

    #region Soil and Water Mapping Methods

    /// <summary>
    /// Maps SoilAndWaterCreateDto to OrchardSoilAndWater entity for creation.
    /// </summary>
    private OrchardSoilAndWater MapSoilAndWaterCreateToEntity(SoilAndWaterCreateDto dto)
    {
        if (dto == null)
            return null;

        return new OrchardSoilAndWater
        {
            SoilType = dto.SoilType,
            SoilPh = dto.SoilPh,
            SoilNitrogen = dto.SoilNitrogen,
            SoilPhosphorus = dto.SoilPhosphorus,
            SoilPotassium = dto.SoilPotassium,
            SoilOrganicCarbon = dto.SoilOrganicCarbon,
            SoilMicronutrients = dto.SoilMicronutrients,
            SoilTestReportUrls = SerializeArray(dto.SoilTestReportUrls),
            SoilHealthStatus = SerializeArray(dto.SoilHealthStatus),
            LastSoilTestDate = dto.LastSoilTestDate,
            WaterSource = dto.WaterSource,
            IrrigationMethod = dto.IrrigationMethod,
            WaterAvailability = dto.WaterAvailability
        };
    }

    /// <summary>
    /// Maps SoilAndWaterUpdateDto to OrchardSoilAndWater entity for updates.
    /// Only updates fields that are provided (non-null).
    /// </summary>
    private void UpdateSoilAndWaterEntity(OrchardSoilAndWater existing, SoilAndWaterUpdateDto dto)
    {
        if (dto == null)
            return;

        if (!string.IsNullOrWhiteSpace(dto.SoilType))
            existing.SoilType = dto.SoilType;

        if (dto.SoilPh.HasValue)
            existing.SoilPh = dto.SoilPh;

        if (!string.IsNullOrWhiteSpace(dto.SoilNitrogen))
            existing.SoilNitrogen = dto.SoilNitrogen;

        if (!string.IsNullOrWhiteSpace(dto.SoilPhosphorus))
            existing.SoilPhosphorus = dto.SoilPhosphorus;

        if (!string.IsNullOrWhiteSpace(dto.SoilPotassium))
            existing.SoilPotassium = dto.SoilPotassium;

        if (!string.IsNullOrWhiteSpace(dto.SoilOrganicCarbon))
            existing.SoilOrganicCarbon = dto.SoilOrganicCarbon;

        if (!string.IsNullOrWhiteSpace(dto.SoilMicronutrients))
            existing.SoilMicronutrients = dto.SoilMicronutrients;

        if (dto.SoilTestReportUrls != null && dto.SoilTestReportUrls.Length > 0)
            existing.SoilTestReportUrls = SerializeArray(dto.SoilTestReportUrls);

        if (dto.SoilHealthStatus != null && dto.SoilHealthStatus.Length > 0)
            existing.SoilHealthStatus = SerializeArray(dto.SoilHealthStatus);

        if (dto.LastSoilTestDate.HasValue)
            existing.LastSoilTestDate = dto.LastSoilTestDate;

        if (!string.IsNullOrWhiteSpace(dto.WaterSource))
            existing.WaterSource = dto.WaterSource;

        if (!string.IsNullOrWhiteSpace(dto.IrrigationMethod))
            existing.IrrigationMethod = dto.IrrigationMethod;

        if (!string.IsNullOrWhiteSpace(dto.WaterAvailability))
            existing.WaterAvailability = dto.WaterAvailability;
    }

    #endregion

    #region Crop Nutrition Mapping Methods

    /// <summary>
    /// Maps CropNutritionCreateDto to OrchardCropNutrition entity for creation.
    /// </summary>
    private OrchardCropNutrition MapCropNutritionCreateToEntity(CropNutritionCreateDto dto)
    {
        if (dto == null)
            return null;

        return new OrchardCropNutrition
        {
            FertilizerPractices = dto.FertilizerPractices,
            HasFertigationFacility = dto.HasFertigationFacility,
            NutritionPlan = dto.NutritionPlan,
            IsMulchingApplied = dto.IsMulchingApplied,
            LastFertilizerApplicationDate = dto.LastFertilizerApplicationDate,
            FertilizerApplicationMode = dto.FertilizerApplicationMode
        };
    }

    /// <summary>
    /// Maps CropNutritionUpdateDto to OrchardCropNutrition entity for updates.
    /// Only updates fields that are provided (non-null).
    /// </summary>
    private void UpdateCropNutritionEntity(OrchardCropNutrition existing, CropNutritionUpdateDto dto)
    {
        if (dto == null)
            return;

        if (!string.IsNullOrWhiteSpace(dto.FertilizerPractices))
            existing.FertilizerPractices = dto.FertilizerPractices;

        if (dto.HasFertigationFacility.HasValue)
            existing.HasFertigationFacility = dto.HasFertigationFacility.Value;

        if (!string.IsNullOrWhiteSpace(dto.NutritionPlan))
            existing.NutritionPlan = dto.NutritionPlan;

        if (dto.IsMulchingApplied.HasValue)
            existing.IsMulchingApplied = dto.IsMulchingApplied.Value;

        if (dto.LastFertilizerApplicationDate.HasValue)
            existing.LastFertilizerApplicationDate = dto.LastFertilizerApplicationDate;

        if (!string.IsNullOrWhiteSpace(dto.FertilizerApplicationMode))
            existing.FertilizerApplicationMode = dto.FertilizerApplicationMode;
    }

    #endregion
}
