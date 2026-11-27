using System;
using System.Collections.Generic;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    /// <summary>
    /// Main Orchard Profile - Parent entity containing all basic and hierarchical data
    /// </summary>
    public class OrchardProfile : EntityBase
    {
        public string OrchardId { get; set; }

        // Basic Details
        public string OrchardName { get; set; }
        public decimal Kanals { get; set; }
        public decimal Marlas { get; set; }
        public int EstablishmentYear { get; set; }
        public string PlantationPattern { get; set; }
        public string OwnershipType { get; set; }

        // Address Details
        public string District { get; set; }
        public string Tehsil { get; set; }
        public string Zone { get; set; }
        public string PinCode { get; set; }

        // Geo Location
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // Timestamps
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public long? UserId { get; set; }

        // Relationships
        public IList<OrchardLocation> Locations { get; set; } = new List<OrchardLocation>();
        public IList<OrchardVariety> Varieties { get; set; } = new List<OrchardVariety>();
        public OrchardSoilAndWater SoilAndWater { get; set; }
        public OrchardCropNutrition CropNutrition { get; set; }
        public OrchardPlantHealth PlantHealth { get; set; }
        public OrchardInfrastructure Infrastructure { get; set; }
        public OrchardProduction Production { get; set; }
        public OrchardInputCost InputCost { get; set; }
        public OrchardOptionalFields OptionalFields { get; set; }
    }

    /// <summary>
    /// GEO-LOCATION DATA - Maps to basicDetails.location.geoTaggedBoundary
    /// </summary>
    public class OrchardLocation : EntityBase
    {
        public long OrchardProfileId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        public OrchardProfile OrchardProfileMain { get; set; }
    }

    /// <summary>
    /// VARIETY & PLANTING DATA
    /// </summary>
    public class OrchardVariety : EntityBase
    {
        public long OrchardProfileId { get; set; }
        public string VarietyName { get; set; }
        public int NumberOfTrees { get; set; }
        public string RootstockUsed { get; set; }
        public string PlantDensity { get; set; }

        // Tree Age Distribution
        public int TreesLessThan5Years { get; set; }
        public int TreesFiveToTenYears { get; set; }
        public int TreesMoreThan10Years { get; set; }

        public OrchardProfile OrchardProfileMain { get; set; }
    }

    /// <summary>
    /// SOIL & WATER PROFILE DATA
    /// </summary>
    public class OrchardSoilAndWater : EntityBase
    {
        public long OrchardProfileId { get; set; }

        // Soil Test Records
        public string SoilType { get; set; }
        public decimal? SoilPh { get; set; }
        public string SoilNitrogen { get; set; }
        public string SoilPhosphorus { get; set; }
        public string SoilPotassium { get; set; }
        public string SoilOrganicCarbon { get; set; }
        public string SoilMicronutrients { get; set; }
        /// <summary>
        /// JSON-serialized array of URLs to soil test reports
        /// </summary>
        public string SoilTestReportUrls { get; set; }

        // Soil Health
        public string SoilHealthStatus { get; set; }
        public DateTime? LastSoilTestDate { get; set; }

        // Water Management
        public string WaterSource { get; set; }
        public string IrrigationMethod { get; set; }
        public string WaterAvailability { get; set; }

        public OrchardProfile OrchardProfileMain { get; set; }
    }

    /// <summary>
    /// CROP & NUTRITION MANAGEMENT DATA
    /// </summary>
    public class OrchardCropNutrition : EntityBase
    {
        public long OrchardProfileId { get; set; }

        public string FertilizerPractices { get; set; }
        public bool HasFertigationFacility { get; set; }
        public string NutritionPlan { get; set; }
        public bool IsMulchingApplied { get; set; }
        public DateTime? LastFertilizerApplicationDate { get; set; }
        public string FertilizerApplicationMode { get; set; }

        public OrchardProfile OrchardProfileMain { get; set; }
    }

    /// <summary>
    /// PLANT PROTECTION & HEALTH DATA
    /// </summary>
    public class OrchardPlantHealth : EntityBase
    {
        public long OrchardProfileId { get; set; }

        public string DiseaseHistory { get; set; }
        public string PestHistory { get; set; }
        public string SpraySchedule { get; set; }
        public bool IsCropInsuranceCovered { get; set; }

        public OrchardProfile OrchardProfileMain { get; set; }
    }

    /// <summary>
    /// FARM INFRASTRUCTURE & RESOURCES DATA
    /// </summary>
    public class OrchardInfrastructure : EntityBase
    {
        public long OrchardProfileId { get; set; }

        // Equipment & Storage - Stored as JSON arrays in database
        /// <summary>
        /// JSON serialized array of equipment owned (e.g., "Tractor", "Pump", "Sprayer")
        /// </summary>
        public string EquipmentOwned { get; set; }

        /// <summary>
        /// JSON serialized array of storage facilities (e.g., "Cold Store", "Godown")
        /// </summary>
        public string StorageFacilities { get; set; }

        /// <summary>
        /// JSON serialized array of other tools (e.g., "Pruning Shears", "Buckets")
        /// </summary>
        public string OtherTools { get; set; }

        // Labor
        public int PermanentLabor { get; set; }
        public int SeasonalLabor { get; set; }
        public int ContractBasedLabor { get; set; }

        public OrchardProfile OrchardProfileMain { get; set; }
    }

    /// <summary>
    /// PRODUCTION & MARKETING DATA
    /// </summary>
    public class OrchardProduction : EntityBase
    {
        public long OrchardProfileId { get; set; }

        // Yield Information
        public decimal AverageAnnualYield { get; set; }
        public string YieldUnit { get; set; }

        // Harvest Season
        public string PeakHarvestFromMonth { get; set; }
        public string PeakHarvestToMonth { get; set; }

        // Marketing
        public string SellingPreference { get; set; }
        public string BuyerName { get; set; }
        public string BuyerContact { get; set; }

        public OrchardProfile OrchardProfileMain { get; set; }
    }

    /// <summary>
    /// INPUT COST DETAILS DATA
    /// </summary>
    public class OrchardInputCost : EntityBase
    {
        public long OrchardProfileId { get; set; }

        public string PesticideSupplierLocation { get; set; }
        public string PaymentType { get; set; }
        public string CreditPeriod { get; set; }

        public OrchardProfile OrchardProfileMain { get; set; }
    }

    /// <summary>
    /// OPTIONAL / FUTURE-PROOF FIELDS DATA
    /// </summary>
    public class OrchardOptionalFields : EntityBase
    {
        public long OrchardProfileId { get; set; }

        public string GovernmentSchemesAvailed { get; set; }
        public bool IsFpoOrCooperativeMember { get; set; }
        public string Certifications { get; set; }
        public string DigitalToolsUsed { get; set; }

        public OrchardProfile OrchardProfileMain { get; set; }
    }
}
