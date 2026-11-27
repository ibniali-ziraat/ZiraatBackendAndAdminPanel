using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class OrchardProfileViewModel
    {
        public long Id { get; set; } = 0;
        public string OrchardId { get; set; }

        // Basic Details
        public string OrchardName { get; set; }
        public decimal Kanals { get; set; }
        public decimal Marlas { get; set; }
        public int EstablishmentYear { get; set; }
        public string PlantationPattern { get; set; }
        public string OwnershipType { get; set; }

        // Address
        public string District { get; set; }
        public string Tehsil { get; set; }
        public string Zone { get; set; }
        public string PinCode { get; set; }

        // Geo Location
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        // Variety & Planting
        public List<VarietyDto> Varieties { get; set; } = new List<VarietyDto>();

        // Soil & Water
        public SoilAndWaterDto SoilAndWater { get; set; }

        // Crop Nutrition
        public CropNutritionDto CropNutrition { get; set; }

        // Plant Health
        public PlantHealthDto PlantHealth { get; set; }

        // Infrastructure
        public InfrastructureDto Infrastructure { get; set; }

        // Production
        public ProductionDto Production { get; set; }

        // Input Cost
        public InputCostDto InputCost { get; set; }

        // Optional Fields
        public OptionalFieldsDto OptionalFields { get; set; }
    }


    public class VarietyDto
    {
        public long? Id { get; set; }
        public string VarietyName { get; set; }
        public int NumberOfTrees { get; set; }
        public string RootstockUsed { get; set; }
        public string PlantDensity { get; set; }
        public int TreesLessThan5Years { get; set; }
        public int TreesFiveToTenYears { get; set; }
        public int TreesMoreThan10Years { get; set; }
    }

    public class SoilAndWaterDto
    {
        public string SoilType { get; set; }
        public decimal? SoilPh { get; set; }
        public string SoilNitrogen { get; set; }
        public string SoilPhosphorus { get; set; }
        public string SoilPotassium { get; set; }
        public string SoilOrganicCarbon { get; set; }
        public string SoilMicronutrients { get; set; }
        /// <summary>
        /// Array of URLs to soil test reports
        /// </summary>
        public string[] SoilTestReportUrls { get; set; }
        public string[] SoilHealthStatus { get; set; }
        public DateTime? LastSoilTestDate { get; set; }
        public string WaterSource { get; set; }
        public string IrrigationMethod { get; set; }
        public string WaterAvailability { get; set; }
    }

    public class CropNutritionDto
    {
        public string FertilizerPractices { get; set; }
        public bool HasFertigation { get; set; }
        public string NutritionPlan { get; set; }
        public bool IsMulchingApplied { get; set; }
        public DateTime? LastFertilizerApplicationDate { get; set; }
        public string FertilizerApplicationMode { get; set; }
    }

    public class PlantHealthDto
    {
        public string[] DiseaseHistory { get; set; }
        public string[] PestHistory { get; set; }
        public string[] SpraySchedule { get; set; }
        public bool IsCropInsuranceCovered { get; set; }
    }

    public class InfrastructureDto
    {
        public string[] EquipmentOwned { get; set; }
        public string[] StorageFacilities { get; set; }
        public string[] OtherTools { get; set; }
        public int PermanentLabor { get; set; }
        public int SeasonalLabor { get; set; }
        public int ContractBasedLabor { get; set; }
    }

    public class ProductionDto
    {
        public decimal AverageAnnualYield { get; set; }
        public string YieldUnit { get; set; }
        public string PeakHarvestFromMonth { get; set; }
        public string PeakHarvestToMonth { get; set; }
        public string SellingPreference { get; set; }
        public string BuyerName { get; set; }
        public string BuyerContact { get; set; }
    }

    public class InputCostDto
    {
        public string PesticideSupplierLocation { get; set; }
        public string PaymentType { get; set; }
        public string CreditPeriod { get; set; }
    }

    public class OptionalFieldsDto
    {
        public string[] GovernmentSchemesAvailed { get; set; }
        public bool IsFpoOrCooperativeMember { get; set; }
        public string[] Certifications { get; set; }
        public string[] DigitalToolsUsed { get; set; }
    }

    /// <summary>
    /// Response DTO for Orchard Profile API endpoints.
    /// Used when returning orchard profile data to clients.
    /// Includes all profile details with nested child entities.
    /// </summary>
    public class OrchardProfileResponseDto
    {
        /// <summary>
        /// Unique database identifier for the orchard profile
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// External unique identifier for the orchard (e.g., "ORCH_12345")
        /// </summary>
        public string OrchardId { get; set; }

        /// <summary>
        /// Name of the orchard
        /// </summary>
        public string OrchardName { get; set; }

        /// <summary>
        /// Size in kanals (traditional land measurement)
        /// </summary>
        public decimal Kanals { get; set; }

        /// <summary>
        /// Size in marlas (traditional land measurement)
        /// </summary>
        public decimal Marlas { get; set; }

        /// <summary>
        /// Year when the orchard was established
        /// </summary>
        public int EstablishmentYear { get; set; }

        /// <summary>
        /// Pattern of plantation (e.g., "High Density", "Low Density")
        /// </summary>
        public string PlantationPattern { get; set; }

        /// <summary>
        /// Type of ownership (e.g., "Owned", "Leased")
        /// </summary>
        public string OwnershipType { get; set; }

        /// <summary>
        /// Administrative district where the orchard is located
        /// </summary>
        public string District { get; set; }

        /// <summary>
        /// Administrative tehsil/block where the orchard is located
        /// </summary>
        public string Tehsil { get; set; }

        /// <summary>
        /// Zone information for the location
        /// </summary>
        public string Zone { get; set; }

        /// <summary>
        /// Postal code of the orchard location
        /// </summary>
        public string PinCode { get; set; }

        /// <summary>
        /// Primary latitude coordinate of the orchard
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// Primary longitude coordinate of the orchard
        /// </summary>
        public decimal? Longitude { get; set; }


        /// <summary>
        /// List of fruit varieties planted in the orchard
        /// </summary>
        public List<VarietyDto> Varieties { get; set; } = new List<VarietyDto>();

        /// <summary>
        /// Soil composition and water management details
        /// </summary>
        public SoilAndWaterDto SoilAndWater { get; set; }

        /// <summary>
        /// Crop nutrition and fertilization details
        /// </summary>
        public CropNutritionDto CropNutrition { get; set; }

        /// <summary>
        /// Plant health, disease, and pest information
        /// </summary>
        public PlantHealthDto PlantHealth { get; set; }

        /// <summary>
        /// Farm infrastructure, equipment, and labor resources
        /// </summary>
        public InfrastructureDto Infrastructure { get; set; }

        /// <summary>
        /// Production yield and harvest information
        /// </summary>
        public ProductionDto Production { get; set; }

        /// <summary>
        /// Input costs and supplier details
        /// </summary>
        public InputCostDto InputCost { get; set; }

        /// <summary>
        /// Optional fields including certifications and government schemes
        /// </summary>
        public OptionalFieldsDto OptionalFields { get; set; }

        /// <summary>
        /// Timestamp when the profile was created
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Timestamp when the profile was last updated
        /// </summary>
        public DateTime? UpdatedOn { get; set; }
    }

    /// <summary>
    /// Legacy alias for OrchardProfileResponseDto (for backward compatibility)
    /// </summary>
    public class OrchardProfileMainResponseDto : OrchardProfileResponseDto
    {
    }

    /// <summary>
    /// PATCH DTO for partial updates to Orchard Profile.
    /// Allows updating only the sections that are provided.
    /// Null sections will be ignored during update.
    /// </summary>
    public class OrchardProfilePatchViewModel
    {
        /// <summary>
        /// Name of the orchard (nullable - only update if provided)
        /// </summary>
        public string OrchardName { get; set; }

        /// <summary>
        /// Size in kanals (nullable)
        /// </summary>
        public decimal? Kanals { get; set; }

        /// <summary>
        /// Size in marlas (nullable)
        /// </summary>
        public decimal? Marlas { get; set; }

        /// <summary>
        /// Year when the orchard was established (nullable)
        /// </summary>
        public int? EstablishmentYear { get; set; }

        /// <summary>
        /// Pattern of plantation (nullable)
        /// </summary>
        public string PlantationPattern { get; set; }

        /// <summary>
        /// Type of ownership (nullable)
        /// </summary>
        public string OwnershipType { get; set; }

        /// <summary>
        /// Administrative district (nullable)
        /// </summary>
        public string District { get; set; }

        /// <summary>
        /// Administrative tehsil/block (nullable)
        /// </summary>
        public string Tehsil { get; set; }

        /// <summary>
        /// Zone information (nullable)
        /// </summary>
        public string Zone { get; set; }

        /// <summary>
        /// Postal code (nullable)
        /// </summary>
        public string PinCode { get; set; }

        /// <summary>
        /// Latitude coordinate (nullable)
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// Longitude coordinate (nullable)         
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// Variety & Planting section (nullable)
        /// </summary>
        public List<VarietyDto> Varieties { get; set; }

        /// <summary>
        /// Soil & Water section (nullable)
        /// </summary>
        public SoilAndWaterDto SoilAndWater { get; set; }

        /// <summary>
        /// Crop Nutrition section (nullable)
        /// </summary>
        public CropNutritionDto CropNutrition { get; set; }

        /// <summary>
        /// Plant Health section (nullable)
        /// </summary>
        public PlantHealthDto PlantHealth { get; set; }

        /// <summary>
        /// Infrastructure section (nullable)
        /// </summary>
        public InfrastructureDto Infrastructure { get; set; }

        /// <summary>
        /// Production section (nullable)
        /// </summary>
        public ProductionDto Production { get; set; }

        /// <summary>
        /// Input Cost section (nullable)
        /// </summary>
        public InputCostDto InputCost { get; set; }

        /// <summary>
        /// Optional Fields section (nullable)
        /// </summary>
        public OptionalFieldsDto OptionalFields { get; set; }
    }

    /// <summary>
    /// DTO for basic orchard profile details in PATCH operations.
    /// All properties are nullable to support partial updates.
    /// </summary>
    public class OrchardBasicDetailsPatchDto
    {
        /// <summary>
        /// Name of the orchard (nullable - only update if provided)
        /// </summary>
        public string OrchardName { get; set; }

        /// <summary>
        /// Size in kanals (nullable)
        /// </summary>
        public decimal? Kanals { get; set; }

        /// <summary>
        /// Size in marlas (nullable)
        /// </summary>
        public decimal? Marlas { get; set; }

        /// <summary>
        /// Year when the orchard was established (nullable)
        /// </summary>
        public int? EstablishmentYear { get; set; }

        /// <summary>
        /// Pattern of plantation (nullable)
        /// </summary>
        public string PlantationPattern { get; set; }

        /// <summary>
        /// Type of ownership (nullable)
        /// </summary>
        public string OwnershipType { get; set; }

        /// <summary>
        /// Administrative district (nullable)
        /// </summary>
        public string District { get; set; }

        /// <summary>
        /// Administrative tehsil/block (nullable)
        /// </summary>
        public string Tehsil { get; set; }

        /// <summary>
        /// Zone information (nullable)
        /// </summary>
        public string Zone { get; set; }

        /// <summary>
        /// Postal code (nullable)
        /// </summary>
        public string PinCode { get; set; }

        /// <summary>
        /// Latitude coordinate (nullable)
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// Longitude coordinate (nullable)         
        /// </summary>
        public decimal? Longitude { get; set; }
    }

    /// <summary>
    /// DTO for creating production data.
    /// All required fields for initial production setup.
    /// </summary>
    public class ProductionCreateDto
    {
        /// <summary>
        /// Average annual yield from the orchard
        /// </summary>
        [Required(ErrorMessage = "Average annual yield is required")]
        public decimal AverageAnnualYield { get; set; }

        /// <summary>
        /// Unit of measurement for yield (e.g., "kg", "tons", "quintals")
        /// </summary>
        [Required(ErrorMessage = "Yield unit is required")]
        public string YieldUnit { get; set; }

        /// <summary>
        /// Month when peak harvest starts
        /// </summary>
        [Required(ErrorMessage = "Peak harvest from month is required")]
        public string PeakHarvestFromMonth { get; set; }

        /// <summary>
        /// Month when peak harvest ends
        /// </summary>
        [Required(ErrorMessage = "Peak harvest to month is required")]
        public string PeakHarvestToMonth { get; set; }

        /// <summary>
        /// Preference for selling (e.g., "Wholesale", "Retail", "Direct to Consumer")
        /// </summary>
        public string SellingPreference { get; set; }

        /// <summary>
        /// Name of the buyer (if applicable)
        /// </summary>
        public string BuyerName { get; set; }

        /// <summary>
        /// Contact information of the buyer (phone or email)
        /// </summary>
        public string BuyerContact { get; set; }
    }

    /// <summary>
    /// DTO for updating production data.
    /// All fields are optional to support partial updates.
    /// </summary>
    public class ProductionUpdateDto
    {
        /// <summary>
        /// Average annual yield from the orchard (nullable - only update if provided)
        /// </summary>
        public decimal? AverageAnnualYield { get; set; }

        /// <summary>
        /// Unit of measurement for yield (nullable)
        /// </summary>
        public string YieldUnit { get; set; }

        /// <summary>
        /// Month when peak harvest starts (nullable)
        /// </summary>
        public string PeakHarvestFromMonth { get; set; }

        /// <summary>
        /// Month when peak harvest ends (nullable)
        /// </summary>
        public string PeakHarvestToMonth { get; set; }

        /// <summary>
        /// Preference for selling (nullable)
        /// </summary>
        public string SellingPreference { get; set; }

        /// <summary>
        /// Name of the buyer (nullable)
        /// </summary>
        public string BuyerName { get; set; }

        /// <summary>
        /// Contact information of the buyer (nullable)
        /// </summary>
        public string BuyerContact { get; set; }
    }

    #region Variety DTOs

    /// <summary>
    /// DTO for creating variety data.
    /// All required fields for initial variety setup.
    /// </summary>
    public class VarietyCreateDto
    {
        /// <summary>
        /// Name of the variety
        /// </summary>
        [Required(ErrorMessage = "Variety name is required")]
        public string VarietyName { get; set; }

        /// <summary>
        /// Total number of trees of this variety
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Number of trees must be greater than 0")]
        public int NumberOfTrees { get; set; }

        /// <summary>
        /// Rootstock used for grafting (if applicable)
        /// </summary>
        public string RootstockUsed { get; set; }

        /// <summary>
        /// Plant density information
        /// </summary>
        public string PlantDensity { get; set; }

        /// <summary>
        /// Number of trees less than 5 years old
        /// </summary>
        public int TreesLessThan5Years { get; set; }

        /// <summary>
        /// Number of trees between 5 and 10 years old
        /// </summary>
        public int TreesFiveToTenYears { get; set; }

        /// <summary>
        /// Number of trees more than 10 years old
        /// </summary>
        public int TreesMoreThan10Years { get; set; }
    }

    /// <summary>
    /// DTO for updating variety data.
    /// All fields are optional to support partial updates.
    /// </summary>
    public class VarietyUpdateDto
    {
        /// <summary>
        /// Name of the variety (nullable)
        /// </summary>
        public string VarietyName { get; set; }

        /// <summary>
        /// Total number of trees of this variety (nullable)
        /// </summary>
        public int? NumberOfTrees { get; set; }

        /// <summary>
        /// Rootstock used for grafting (nullable)
        /// </summary>
        public string RootstockUsed { get; set; }

        /// <summary>
        /// Plant density information (nullable)
        /// </summary>
        public string PlantDensity { get; set; }

        /// <summary>
        /// Number of trees less than 5 years old (nullable)
        /// </summary>
        public int? TreesLessThan5Years { get; set; }

        /// <summary>
        /// Number of trees between 5 and 10 years old (nullable)
        /// </summary>
        public int? TreesFiveToTenYears { get; set; }

        /// <summary>
        /// Number of trees more than 10 years old (nullable)
        /// </summary>
        public int? TreesMoreThan10Years { get; set; }
    }

    #endregion

    #region Soil and Water DTOs

    /// <summary>
    /// DTO for creating soil and water data.
    /// All required fields for initial setup.
    /// </summary>
    public class SoilAndWaterCreateDto
    {
        /// <summary>
        /// Type of soil (e.g., "Loamy", "Sandy", "Clay")
        /// </summary>
        [Required(ErrorMessage = "Soil type is required")]
        public string SoilType { get; set; }

        /// <summary>
        /// pH level of the soil
        /// </summary>
        public decimal? SoilPh { get; set; }

        /// <summary>
        /// Nitrogen content level
        /// </summary>
        public string SoilNitrogen { get; set; }

        /// <summary>
        /// Phosphorus content level
        /// </summary>
        public string SoilPhosphorus { get; set; }

        /// <summary>
        /// Potassium content level
        /// </summary>
        public string SoilPotassium { get; set; }

        /// <summary>
        /// Organic carbon content
        /// </summary>
        public string SoilOrganicCarbon { get; set; }

        /// <summary>
        /// Micronutrients information
        /// </summary>
        public string SoilMicronutrients { get; set; }

        /// <summary>
        /// Array of URLs to soil test reports
        /// </summary>
        public string[] SoilTestReportUrls { get; set; }

        /// <summary>
        /// Soil health status array
        /// </summary>
        public string[] SoilHealthStatus { get; set; }

        /// <summary>
        /// Date of last soil test
        /// </summary>
        public DateTime? LastSoilTestDate { get; set; }

        /// <summary>
        /// Source of water (e.g., "Well", "Canal", "Tube Well")
        /// </summary>
        [Required(ErrorMessage = "Water source is required")]
        public string WaterSource { get; set; }

        /// <summary>
        /// Irrigation method used (e.g., "Drip", "Flood", "Sprinkler")
        /// </summary>
        [Required(ErrorMessage = "Irrigation method is required")]
        public string IrrigationMethod { get; set; }

        /// <summary>
        /// Water availability level
        /// </summary>
        public string WaterAvailability { get; set; }
    }

    /// <summary>
    /// DTO for updating soil and water data.
    /// All fields are optional to support partial updates.
    /// </summary>
    public class SoilAndWaterUpdateDto
    {
        /// <summary>
        /// Type of soil (nullable)
        /// </summary>
        public string SoilType { get; set; }

        /// <summary>
        /// pH level of the soil (nullable)
        /// </summary>
        public decimal? SoilPh { get; set; }

        /// <summary>
        /// Nitrogen content level (nullable)
        /// </summary>
        public string SoilNitrogen { get; set; }

        /// <summary>
        /// Phosphorus content level (nullable)
        /// </summary>
        public string SoilPhosphorus { get; set; }

        /// <summary>
        /// Potassium content level (nullable)
        /// </summary>
        public string SoilPotassium { get; set; }

        /// <summary>
        /// Organic carbon content (nullable)
        /// </summary>
        public string SoilOrganicCarbon { get; set; }

        /// <summary>
        /// Micronutrients information (nullable)
        /// </summary>
        public string SoilMicronutrients { get; set; }

        /// <summary>
        /// Array of URLs to soil test reports (nullable)
        /// </summary>
        public string[] SoilTestReportUrls { get; set; }

        /// <summary>
        /// Soil health status array (nullable)
        /// </summary>
        public string[] SoilHealthStatus { get; set; }

        /// <summary>
        /// Date of last soil test (nullable)
        /// </summary>
        public DateTime? LastSoilTestDate { get; set; }

        /// <summary>
        /// Source of water (nullable)
        /// </summary>
        public string WaterSource { get; set; }

        /// <summary>
        /// Irrigation method used (nullable)
        /// </summary>
        public string IrrigationMethod { get; set; }

        /// <summary>
        /// Water availability level (nullable)
        /// </summary>
        public string WaterAvailability { get; set; }
    }

    #endregion

    #region Crop Nutrition DTOs

    /// <summary>
    /// DTO for creating crop nutrition data.
    /// All required fields for initial nutrition setup.
    /// </summary>
    public class CropNutritionCreateDto
    {
        /// <summary>
        /// Fertilizer application practices used
        /// </summary>
        [Required(ErrorMessage = "Fertilizer practices are required")]
        public string FertilizerPractices { get; set; }

        /// <summary>
        /// Whether fertigation facility is available
        /// </summary>
        public bool HasFertigationFacility { get; set; }

        /// <summary>
        /// Nutrition plan document or description
        /// </summary>
        public string NutritionPlan { get; set; }

        /// <summary>
        /// Whether mulching is applied
        /// </summary>
        public bool IsMulchingApplied { get; set; }

        /// <summary>
        /// Date of last fertilizer application
        /// </summary>
        public DateTime? LastFertilizerApplicationDate { get; set; }

        /// <summary>
        /// Mode of fertilizer application (e.g., "Foliar", "Soil", "Fertigation")
        /// </summary>
        public string FertilizerApplicationMode { get; set; }
    }

    /// <summary>
    /// DTO for updating crop nutrition data.
    /// All fields are optional to support partial updates.
    /// </summary>
    public class CropNutritionUpdateDto
    {
        /// <summary>
        /// Fertilizer application practices used (nullable)
        /// </summary>
        public string FertilizerPractices { get; set; }

        /// <summary>
        /// Whether fertigation facility is available (nullable)
        /// </summary>
        public bool? HasFertigationFacility { get; set; }

        /// <summary>
        /// Nutrition plan document or description (nullable)
        /// </summary>
        public string NutritionPlan { get; set; }

        /// <summary>
        /// Whether mulching is applied (nullable)
        /// </summary>
        public bool? IsMulchingApplied { get; set; }

        /// <summary>
        /// Date of last fertilizer application (nullable)
        /// </summary>
        public DateTime? LastFertilizerApplicationDate { get; set; }

        /// <summary>
        /// Mode of fertilizer application (nullable)
        /// </summary>
        public string FertilizerApplicationMode { get; set; }
    }
}

    #endregion
