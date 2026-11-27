using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Module.Core.Services
{
    public class LabSampleRequest
    {
        [Required, RegularExpression("^(soil|water)$")] public string SampleType { get; set; }
        public DateTime? SampledAt { get; set; }
        [StringLength(200)] public string LabName { get; set; }
    }
    public class LabReadingRequest
    {
        [Required, StringLength(64)] public string AnalyteCode { get; set; }
        public decimal Value { get; set; }
        [Required, StringLength(32)] public string Unit { get; set; }
        [StringLength(200)] public string Method { get; set; }
    }
    public class LabAttachmentRequest
    {
        public long BlobFileReferenceId { get; set; }
        [Required, StringLength(260)] public string FileName { get; set; }
        [Required, RegularExpression("^(application/pdf|image/jpeg|image/png)$")] public string ContentType { get; set; }
        [Range(1, 10485760)] public long SizeBytes { get; set; }
    }
    public class LabReportRequest
    {
        public DateTime? ReportedAt { get; set; }
        [Required, StringLength(100)] public string Source { get; set; }
        [StringLength(2000)] public string MethodNotes { get; set; }
        public LabReadingRequest[] Readings { get; set; } = Array.Empty<LabReadingRequest>();
        public LabAttachmentRequest[] Attachments { get; set; } = Array.Empty<LabAttachmentRequest>();
    }
    public class LabReviewRequest
    {
        [Required, RegularExpression("^(approved|rejected)$")] public string Decision { get; set; }
        [StringLength(2000)] public string Notes { get; set; }
    }
    public class NutrientRangeRuleRequest
    {
        public Guid Version { get; set; }
        [Required, StringLength(64)] public string AnalyteCode { get; set; }
        [StringLength(100)] public string Crop { get; set; }
        [StringLength(100)] public string Variety { get; set; }
        [StringLength(100)] public string GrowthStage { get; set; }
        [Required, StringLength(32)] public string Unit { get; set; }
        public decimal? Minimum { get; set; }
        public decimal? Maximum { get; set; }
        [Required, StringLength(2000)] public string Evidence { get; set; }
    }
    public record LabReceipt(Guid Id, string Status, int VersionNumber = 0);
}
