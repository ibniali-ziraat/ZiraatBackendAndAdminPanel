namespace ECommerce.Module.Core.Services;

/// <summary>
/// Result of a delete operation with cascade impact information.
/// </summary>
public class DeleteResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int RecordsDeleted { get; set; }
}

/// <summary>
/// Preview of what will be deleted due to cascade delete configuration.
/// </summary>
public class DeletePreview
{
    public long ProfileId { get; set; }
    public string ProfileName { get; set; }
    public string OrchardId { get; set; }
    
    /// <summary>
    /// Total number of records that will be deleted (parent + all children)
    /// </summary>
    public int TotalRecordsToDelete { get; set; }
    
    /// <summary>
    /// One-to-many child records
    /// </summary>
    public int LocationsCount { get; set; }
    public int VarietiesCount { get; set; }
    
    /// <summary>
    /// One-to-one detail entities (cascade delete)
    /// </summary>
    public DetailEntityPreview DetailEntities { get; set; }
    
    /// <summary>
    /// Gets a human-readable summary
    /// </summary>
    public string GetSummary() =>
        $"Deleting '{ProfileName}' will remove {TotalRecordsToDelete} records: " +
        $"1 profile, {LocationsCount} locations, {VarietiesCount} varieties, " +
        $"and {DetailEntities.GetDetailCount()} detail sections.";
}

/// <summary>
/// Details of one-to-one entities affected by cascade delete.
/// </summary>
public class DetailEntityPreview
{
    public bool HasSoilAndWater { get; set; }
    public bool HasCropNutrition { get; set; }
    public bool HasPlantHealth { get; set; }
    public bool HasInfrastructure { get; set; }
    public bool HasProduction { get; set; }
    public bool HasInputCost { get; set; }
    public bool HasOptionalFields { get; set; }
    
    /// <summary>
    /// Count of detail entities that will be cascade deleted
    /// </summary>
    public int GetDetailCount() =>
        (HasSoilAndWater ? 1 : 0) +
        (HasCropNutrition ? 1 : 0) +
        (HasPlantHealth ? 1 : 0) +
        (HasInfrastructure ? 1 : 0) +
        (HasProduction ? 1 : 0) +
        (HasInputCost ? 1 : 0) +
        (HasOptionalFields ? 1 : 0);
}
