using System;
using System.ComponentModel.DataAnnotations;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class FarmActivity : EntityBase
    {
        public long OrchardProfileId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public OrchardProfile OrchardProfile { get; set; }
        public long RecordedById { get; set; }
        [Required, StringLength(32)]
        public string Kind { get; set; }
        public DateTimeOffset OccurredOn { get; set; }
        public DateTimeOffset? ScheduledFor { get; set; }
        public bool IsCompleted { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Amount { get; set; }
        [StringLength(32)]
        public string ExpenseCategory { get; set; }
        [StringLength(2000)]
        public string Notes { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset UpdatedOn { get; set; }
    }

    public class FarmAssessment : EntityBase
    {
        public long OrchardProfileId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public OrchardProfile OrchardProfile { get; set; }
        public long RecordedById { get; set; }
        public DateTimeOffset ObservedOn { get; set; }
        public int? TreesAssessed { get; set; }
        public int? LivingTrees { get; set; }
        public decimal? AverageTreeAgeYears { get; set; }
        public decimal? YieldKgPerTree { get; set; }
        public decimal? YieldKgPerHectare { get; set; }
        public int? TreesMonitored { get; set; }
        public int? PestAffectedTrees { get; set; }
        public int? DiseaseAffectedTrees { get; set; }
        [StringLength(16)]
        public string RiskLevel { get; set; }
        public bool? PesticideResistanceSuspected { get; set; }
        public decimal? SoilPh { get; set; }
        public decimal? SoilEc { get; set; }
        public decimal? OrganicMatterPercent { get; set; }
        [StringLength(16)]
        public string NitrogenLevel { get; set; }
        [StringLength(16)]
        public string PhosphorusLevel { get; set; }
        [StringLength(16)]
        public string PotassiumLevel { get; set; }
        public int? DeficiencySymptomsCount { get; set; }
        [StringLength(2000)]
        public string Notes { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset UpdatedOn { get; set; }
    }
}
