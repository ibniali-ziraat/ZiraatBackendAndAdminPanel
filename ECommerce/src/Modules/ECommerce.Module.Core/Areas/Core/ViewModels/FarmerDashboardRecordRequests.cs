using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class FarmActivityRequest : IValidatableObject
    {
        public static readonly string[] Kinds = { "harvest", "sale", "expense", "irrigation", "fertilizer", "spray", "labor", "equipment", "pruning", "fertigation", "seasonal-advisory" };
        [Required, StringLength(32)] public string Kind { get; set; }
        public DateTimeOffset OccurredOn { get; set; }
        public DateTimeOffset? ScheduledFor { get; set; }
        public bool IsCompleted { get; set; } = true;
        [Range(typeof(decimal), "0", "1000000000")] public decimal? Quantity { get; set; }
        [Range(typeof(decimal), "0", "1000000000")] public decimal? Amount { get; set; }
        [StringLength(32)] public string ExpenseCategory { get; set; }
        [StringLength(2000)] public string Notes { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Kinds.Contains(Kind))
                yield return new ValidationResult("Unsupported activity kind.", new[] { nameof(Kind) });
            if (OccurredOn.Year < 2000 || OccurredOn.Year > 2100 || (IsCompleted && OccurredOn > DateTimeOffset.UtcNow))
                yield return new ValidationResult("An occurrence date is required; completed activities cannot be in the future.", new[] { nameof(OccurredOn) });
            if (!IsCompleted && !ScheduledFor.HasValue)
                yield return new ValidationResult("Pending activities require ScheduledFor.", new[] { nameof(ScheduledFor) });
            if (ScheduledFor.HasValue && (ScheduledFor.Value.Year < 2000 || ScheduledFor.Value.Year > 2100))
                yield return new ValidationResult("ScheduledFor must be within reporting years 2000-2100.", new[] { nameof(ScheduledFor) });
            if (IsCompleted && new[] { "harvest", "sale", "irrigation", "fertilizer", "spray", "labor", "equipment" }.Contains(Kind) && !(Quantity > 0))
                yield return new ValidationResult("A positive quantity is required for this completed activity.", new[] { nameof(Quantity) });
            if (IsCompleted && (Kind == "sale" || Kind == "expense") && !Amount.HasValue)
                yield return new ValidationResult("A recorded amount is required for sales and expenses.", new[] { nameof(Amount) });
            if (Amount.HasValue && Kind != "sale" && Kind != "expense")
                yield return new ValidationResult("Record spending as a separate expense to avoid double counting.", new[] { nameof(Amount) });
            if (Kind == "expense" && !new[] { "fertilizer", "pesticide", "labor", "material", "equipment", "other" }.Contains(ExpenseCategory))
                yield return new ValidationResult("Select a valid expense category.", new[] { nameof(ExpenseCategory) });
            if (Kind != "expense" && ExpenseCategory != null)
                yield return new ValidationResult("ExpenseCategory applies only to expenses.", new[] { nameof(ExpenseCategory) });
        }
    }

    public class FarmAssessmentRequest : IValidatableObject
    {
        public DateTimeOffset ObservedOn { get; set; }
        [Range(1, 100000000)] public int? TreesAssessed { get; set; }
        [Range(0, 100000000)] public int? LivingTrees { get; set; }
        [Range(typeof(decimal), "0", "1000")] public decimal? AverageTreeAgeYears { get; set; }
        [Range(typeof(decimal), "0", "1000000000")] public decimal? YieldKgPerTree { get; set; }
        [Range(typeof(decimal), "0", "1000000000")] public decimal? YieldKgPerHectare { get; set; }
        [Range(1, 100000000)] public int? TreesMonitored { get; set; }
        [Range(0, 100000000)] public int? PestAffectedTrees { get; set; }
        [Range(0, 100000000)] public int? DiseaseAffectedTrees { get; set; }
        [RegularExpression("^(Low|Medium|High)$")] public string RiskLevel { get; set; }
        public bool? PesticideResistanceSuspected { get; set; }
        [Range(typeof(decimal), "0", "14")] public decimal? SoilPh { get; set; }
        [Range(typeof(decimal), "0", "1000")] public decimal? SoilEc { get; set; }
        [Range(typeof(decimal), "0", "100")] public decimal? OrganicMatterPercent { get; set; }
        [RegularExpression("^(Low|Medium|High)$")] public string NitrogenLevel { get; set; }
        [RegularExpression("^(Low|Medium|High)$")] public string PhosphorusLevel { get; set; }
        [RegularExpression("^(Low|Medium|High)$")] public string PotassiumLevel { get; set; }
        [Range(0, 100000000)] public int? DeficiencySymptomsCount { get; set; }
        [StringLength(2000)] public string Notes { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ObservedOn.Year < 2000 || ObservedOn > DateTimeOffset.UtcNow)
                yield return new ValidationResult("An observation date no later than now is required.", new[] { nameof(ObservedOn) });
            if (LivingTrees.HasValue != TreesAssessed.HasValue || LivingTrees > TreesAssessed)
                yield return new ValidationResult("LivingTrees and TreesAssessed must be supplied together; living trees cannot exceed assessed trees.", new[] { nameof(LivingTrees) });
            if (((PestAffectedTrees.HasValue || DiseaseAffectedTrees.HasValue) && !TreesMonitored.HasValue) || PestAffectedTrees > TreesMonitored || DiseaseAffectedTrees > TreesMonitored)
                yield return new ValidationResult("Affected trees require a monitored-tree count and cannot exceed it.", new[] { nameof(TreesMonitored) });
            if (!TreesAssessed.HasValue && !AverageTreeAgeYears.HasValue && !YieldKgPerTree.HasValue && !YieldKgPerHectare.HasValue && !TreesMonitored.HasValue && RiskLevel == null && !PesticideResistanceSuspected.HasValue && !SoilPh.HasValue && !SoilEc.HasValue && !OrganicMatterPercent.HasValue && NitrogenLevel == null && PhosphorusLevel == null && PotassiumLevel == null && !DeficiencySymptomsCount.HasValue)
                yield return new ValidationResult("At least one measured field is required.");
        }
    }
}
