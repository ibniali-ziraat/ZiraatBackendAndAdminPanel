using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Module.Core.Services
{
    public class MeasuredValueRequest
    {
        [Range(typeof(decimal), "0.0001", "999999999")] public decimal Value { get; set; }
        [Required, StringLength(16)] public string Unit { get; set; }
    }
    public class EquipmentHoursRequest
    {
        [Required, StringLength(64)] public string Equipment { get; set; }
        [Range(typeof(decimal), "0.0001", "100000")] public decimal Hours { get; set; }
    }
    public abstract class FarmOperationRequest
    {
        public Guid Version { get; set; }
        [Required] public DateTime? OperationDate { get; set; }
        [Required] public MeasuredValueRequest Coverage { get; set; }
        [Range(typeof(decimal), "0", "1000000")] public decimal? LabourHours { get; set; }
        [Range(1, 100000)] public int? NumberOfLabourers { get; set; }
        [MaxLength(50)] public string[] EquipmentUsed { get; set; } = Array.Empty<string>();
        [MaxLength(50)] public EquipmentHoursRequest[] EquipmentHours { get; set; } = Array.Empty<EquipmentHoursRequest>();
        [Range(typeof(decimal), "0", "999999999999")] public decimal? Cost { get; set; }
        [StringLength(3)] public string Currency { get; set; }
        [StringLength(2000)] public string Notes { get; set; }
    }
    public class FertigationOperationRequest : FarmOperationRequest
    {
        [Required, StringLength(200)] public string FertilizerType { get; set; }
        [Required] public MeasuredValueRequest QuantityApplied { get; set; }
        [Required] public MeasuredValueRequest IrrigationVolumeApplied { get; set; }
    }
    public class SprayOperationRequest : FarmOperationRequest
    {
        [Required, StringLength(200)] public string ProductUsed { get; set; }
        [Required, StringLength(200)] public string TargetPestDisease { get; set; }
        [Required] public MeasuredValueRequest DoseApplied { get; set; }
        public MeasuredValueRequest TotalMixtureVolume { get; set; }
        public MeasuredValueRequest ActualProductQuantity { get; set; }
    }
    public class PruningOperationRequest : FarmOperationRequest
    {
        [Required, RegularExpression("^(maintenance|formative|rejuvenation|sanitary)$")] public string PruningType { get; set; }
    }
    public class HarvestSaleLotRequest
    {
        [Required] public MeasuredValueRequest Quantity { get; set; }
        public bool IsSold { get; set; }
        public MeasuredValueRequest PriceReceivedPerUnit { get; set; }
        [StringLength(200)] public string BuyerOrMandi { get; set; }
    }
    public class HarvestOperationRequest : FarmOperationRequest
    {
        [Required] public MeasuredValueRequest QuantityHarvested { get; set; }
        [Range(typeof(decimal), "0", "999999999999")] public decimal? CostOfHarvesting { get; set; }
        [MaxLength(100)] public HarvestSaleLotRequest[] SaleLots { get; set; } = Array.Empty<HarvestSaleLotRequest>();
    }
    public class OperationVoidRequest : WorkflowVersionRequest
    {
        [Required, StringLength(1000)] public string Reason { get; set; }
    }
    public record OperationReceipt(Guid Id, Guid Version, string Status, string Kind, decimal? TotalIncome = null);
}
