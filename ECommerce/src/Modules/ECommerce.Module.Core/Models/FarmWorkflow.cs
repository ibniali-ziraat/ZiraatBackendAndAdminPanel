using System;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models
{
    public class FarmAdvisory : IEntityWithTypedId<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid Version { get; set; } = Guid.NewGuid();
        public string Kind { get; set; }
        public string Title { get; set; }
        public string Recommendation { get; set; }
        public string Crop { get; set; }
        public string Variety { get; set; }
        public string GrowthStage { get; set; }
        public string TargetOrchardsJson { get; set; }
        public string Status { get; set; } = "draft";
        public DateTimeOffset WindowStart { get; set; }
        public DateTimeOffset WindowEnd { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset? PublishedOn { get; set; }
        public DateTimeOffset? CancelledOn { get; set; }
        public long CreatedById { get; set; }
        public string CancellationReason { get; set; }
    }

    public class FarmAdvisoryRecipient : IEntityWithTypedId<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AdvisoryId { get; set; }
        public FarmAdvisory Advisory { get; set; }
        public long OrchardProfileId { get; set; }
        public OrchardProfile Orchard { get; set; }
        public long FarmerId { get; set; }
        public string OrchardName { get; set; }
        public Guid Version { get; set; } = Guid.NewGuid();
        public DateTimeOffset? ReadOn { get; set; }
        public DateTimeOffset? AcknowledgedOn { get; set; }
        public DateTimeOffset? CompletedOn { get; set; }
    }

    public class FarmAcknowledgment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RecipientId { get; set; }
        public long ActorId { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public bool OpenOperationForm { get; set; }
    }

    // Typed operation details and completion validation are introduced in Phases 2/3.
    public class FarmOperation : IEntityWithTypedId<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RecipientId { get; set; }
        public Guid Version { get; set; } = Guid.NewGuid();
        public long OrchardProfileId { get; set; }
        public long FarmerId { get; set; }
        public long RecordedById { get; set; }
        public string Kind { get; set; }
        public DateTime OperationDate { get; set; }
        public string Status { get; set; } = "posted";
        public decimal CoverageValue { get; set; }
        public string CoverageUnit { get; set; }
        public decimal? LabourHours { get; set; }
        public int? NumberOfLabourers { get; set; }
        public string EquipmentJson { get; set; }
        public decimal? EquipmentHours { get; set; }
        public decimal? Cost { get; set; }
        public string Currency { get; set; }
        public string MaterialName { get; set; }
        public decimal? MaterialQuantity { get; set; }
        public string MaterialUnit { get; set; }
        public string Target { get; set; }
        public decimal? DoseValue { get; set; }
        public string DoseUnit { get; set; }
        public decimal? MixtureTotalLitres { get; set; }
        public decimal? IrrigationTotalLitres { get; set; }
        public string PruningType { get; set; }
        public decimal? HarvestQuantityKg { get; set; }
        public string Notes { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset UpdatedOn { get; set; }
        public DateTimeOffset? CompletedOn { get; set; }
        public DateTimeOffset? VoidedOn { get; set; }
        public long? VoidedById { get; set; }
        public string VoidReason { get; set; }
    }

    public class FarmHarvestSaleLot : IEntityWithTypedId<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OperationId { get; set; }
        public FarmOperation Operation { get; set; }
        public decimal QuantityKg { get; set; }
        public decimal? PricePerKg { get; set; }
        public decimal? Income { get; set; }
        public bool IsSold { get; set; }
        public string BuyerOrMandi { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
    }

    public class FarmLabSample : IEntityWithTypedId<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public long OrchardProfileId { get; set; }
        public long FarmerId { get; set; }
        public string SampleType { get; set; }
        public DateTime SampledAt { get; set; }
        public string LabName { get; set; }
        public string Status { get; set; } = "requested";
        public DateTimeOffset CreatedOn { get; set; }
        public long CreatedById { get; set; }
    }

    public class FarmLabReport : IEntityWithTypedId<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SampleId { get; set; }
        public FarmLabSample Sample { get; set; }
        public int VersionNumber { get; set; }
        public string Status { get; set; } = "draft";
        public DateTime ReportedAt { get; set; }
        public string Source { get; set; }
        public string MethodNotes { get; set; }
        public Guid? SupersedesReportId { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset? VerifiedAt { get; set; }
        public long? VerifiedById { get; set; }
        public string ReviewNotes { get; set; }
    }

    public class FarmLabReading : IEntityWithTypedId<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ReportId { get; set; }
        public FarmLabReport Report { get; set; }
        public string AnalyteCode { get; set; }
        public decimal Value { get; set; }
        public string Unit { get; set; }
        public string Method { get; set; }
    }

    public class FarmLabAttachment : IEntityWithTypedId<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ReportId { get; set; }
        public FarmLabReport Report { get; set; }
        public long BlobFileReferenceId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long SizeBytes { get; set; }
        public string ScanStatus { get; set; } = "pending";
        public DateTimeOffset CreatedOn { get; set; }
    }

    public class FarmNutrientRangeRule : IEntityWithTypedId<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid Version { get; set; } = Guid.NewGuid();
        public string AnalyteCode { get; set; }
        public string Crop { get; set; }
        public string Variety { get; set; }
        public string GrowthStage { get; set; }
        public string Unit { get; set; }
        public decimal? Minimum { get; set; }
        public decimal? Maximum { get; set; }
        public string Status { get; set; } = "draft";
        public string Evidence { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset? ApprovedOn { get; set; }
        public long? ApprovedById { get; set; }
    }

    public class FarmVendorDelegation : IEntityWithTypedId<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid Version { get; set; } = Guid.NewGuid();
        public long VendorId { get; set; }
        public long FarmerId { get; set; }
        public long OrchardProfileId { get; set; }
        public string PermissionsJson { get; set; }
        public string Status { get; set; } = "active";
        public DateTimeOffset CreatedOn { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset? ExpiresOn { get; set; }
        public DateTimeOffset? RevokedOn { get; set; }
        public long? RevokedById { get; set; }
        public string RevocationReason { get; set; }
    }

    public class FarmSupportCase : IEntityWithTypedId<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid Version { get; set; } = Guid.NewGuid();
        public long OrchardProfileId { get; set; }
        public long FarmerId { get; set; }
        public string Kind { get; set; }
        public string Status { get; set; } = "open";
        public string Title { get; set; }
        public string Detail { get; set; }
        public string ExpertResponse { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset? ScheduledOn { get; set; }
        public DateTimeOffset? FirstResponseOn { get; set; }
        public DateTimeOffset? ResolvedOn { get; set; }
        public DateTimeOffset? CancelledOn { get; set; }
        public long CreatedById { get; set; }
        public long? AssignedToId { get; set; }
    }

    public class FarmArticleRead : IEntityWithTypedId<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public long UserId { get; set; }
        public string ArticleId { get; set; }
        public DateTimeOffset ReadOn { get; set; }
    }

    public class FarmAutomationRule : IEntityWithTypedId<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid Version { get; set; } = Guid.NewGuid();
        public string Kind { get; set; }
        public string ConfigurationJson { get; set; }
        public string Evidence { get; set; }
        public string Status { get; set; } = "draft";
        public DateTimeOffset CreatedOn { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset? ApprovedOn { get; set; }
        public long? ApprovedById { get; set; }
    }

    public class FarmWorkflowAudit
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SubjectId { get; set; }
        public long ActorId { get; set; }
        public string Action { get; set; }
        public string Detail { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
    }

    public class FarmWorkflowRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public long ActorId { get; set; }
        public string Scope { get; set; }
        public string Key { get; set; }
        public string BodyHash { get; set; }
        public string ResponseJson { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
    }

    public class FarmDeliveryEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RecipientId { get; set; }
        public string Kind { get; set; }
        public Guid Version { get; set; } = Guid.NewGuid();
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset NextAttemptOn { get; set; }
        public DateTimeOffset? DeliveredOn { get; set; }
        public DateTimeOffset? LeaseUntil { get; set; }
        public int Attempts { get; set; }
        public string LastError { get; set; }
        public string Outcome { get; set; }
    }
}
