using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Module.Core.Services
{
    public class AdvisoryDraftRequest
    {
        public Guid Version { get; set; }

        [Required, RegularExpression("^(general|spray|fertigation|pruning|harvest)$")]
        public string Kind { get; set; }

        [Required, StringLength(160)]
        public string Title { get; set; }

        [Required, StringLength(4000)]
        public string Recommendation { get; set; }

        [StringLength(100)]
        public string Crop { get; set; }

        [StringLength(100)]
        public string Variety { get; set; }

        [StringLength(100)]
        public string GrowthStage { get; set; }

        [Required, MinLength(1), MaxLength(500)]
        public long[] OrchardProfileIds { get; set; }
        public DateTimeOffset WindowStart { get; set; }
        public DateTimeOffset WindowEnd { get; set; }
    }

    public class WorkflowVersionRequest
    {
        public Guid Version { get; set; }
    }

    public class AdvisoryCancelRequest : WorkflowVersionRequest
    {
        [Required, StringLength(1000)]
        public string Reason { get; set; }
    }

    public class AdvisoryAcknowledgeRequest
    {
        public bool ActionCompleted { get; set; }
    }

    public record WorkflowReceipt(
        Guid Id,
        Guid Version,
        string Status,
        int RecipientCount = 0,
        bool OpenOperationForm = false
    );

    public class FarmWorkflowException : Exception
    {
        public int StatusCode { get; }

        public FarmWorkflowException(int statusCode, string message)
            : base(message) => StatusCode = statusCode;
    }
}
