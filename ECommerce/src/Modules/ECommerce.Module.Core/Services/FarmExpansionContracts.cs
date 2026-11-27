using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Module.Core.Services
{
    public class VendorDelegationRequest
    {
        public Guid Version { get; set; }
        [Range(1, long.MaxValue)] public long VendorId { get; set; }
        [Range(1, long.MaxValue)] public long OrchardProfileId { get; set; }
        [Required, MinLength(1)] public string[] Permissions { get; set; }
        public DateTimeOffset? ExpiresOn { get; set; }
    }
    public class RevokeDelegationRequest : WorkflowVersionRequest { [Required, StringLength(1000)] public string Reason { get; set; } }
    public class FarmCaseRequest
    {
        public Guid Version { get; set; }
        [Required, RegularExpression("^(diagnosis|consultation|complaint|service-booking)$")] public string Kind { get; set; }
        [Required, StringLength(160)] public string Title { get; set; }
        [StringLength(4000)] public string Detail { get; set; }
        public DateTimeOffset? ScheduledOn { get; set; }
    }
    public class FarmCaseTransitionRequest : WorkflowVersionRequest
    {
        [Required, RegularExpression("^(respond|resolve|reopen|cancel|complete)$")] public string Action { get; set; }
        [StringLength(4000)] public string Response { get; set; }
        public long? AssignedToId { get; set; }
    }
    public class ArticleReadRequest { [Required, StringLength(200)] public string ArticleId { get; set; } }
    public class AutomationRuleRequest
    {
        public Guid Version { get; set; }
        [Required, RegularExpression("^(gdd|disease-risk|resistance|recommendation|reminder|harvest-escalation)$")] public string Kind { get; set; }
        [Required] public string ConfigurationJson { get; set; }
        [Required, StringLength(2000)] public string Evidence { get; set; }
    }
}
