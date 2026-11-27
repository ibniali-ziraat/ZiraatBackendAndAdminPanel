using System;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Module.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    public class FarmWorkflowErrorsAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            if (context.Exception is FarmWorkflowException error)
            {
                context.Result = new ObjectResult(
                    new ProblemDetails { Status = error.StatusCode, Title = error.Message }
                )
                {
                    StatusCode = error.StatusCode,
                };
                context.ExceptionHandled = true;
            }
        }
    }

    [ApiController, FarmWorkflowErrors]
    [Authorize(AuthenticationSchemes = "Identity.Application", Roles = "admin")]
    [AutoValidateAntiforgeryToken]
    [Route("api/admin/farm-management")]
    public class FarmManagementController : ControllerBase
    {
        private readonly FarmWorkflowService _service;

        public FarmManagementController(FarmWorkflowService service) => _service = service;

        private string Key => Request.Headers["Idempotency-Key"].ToString();

        [HttpGet("orchards")]
        public Task<object> Orchards(
            CancellationToken ct,
            string search = null,
            string district = null,
            int page = 1,
            int pageSize = 25
        ) => _service.Orchards(User, search, page, pageSize, ct, district);

        [HttpGet("advisories")]
        public Task<object> List(
            CancellationToken ct,
            string status = null,
            int page = 1,
            int pageSize = 25
        ) => _service.Advisories(User, status, page, pageSize, ct);

        [HttpGet("advisories/{id:guid}")]
        public Task<object> Detail(Guid id, CancellationToken ct) =>
            _service.Advisory(User, id, ct);

        [HttpPost("advisories")]
        public Task<WorkflowReceipt> Create(AdvisoryDraftRequest request, CancellationToken ct) =>
            _service.SaveDraft(User, null, request, Key, ct);

        [HttpPut("advisories/{id:guid}")]
        public Task<WorkflowReceipt> Update(
            Guid id,
            AdvisoryDraftRequest request,
            CancellationToken ct
        ) => _service.SaveDraft(User, id, request, Key, ct);

        [HttpGet("advisories/{id:guid}/recipient-preview")]
        public Task<object> Preview(Guid id, CancellationToken ct) =>
            _service.Preview(User, id, ct);

        [HttpPost("advisories/{id:guid}/publish")]
        public Task<WorkflowReceipt> Publish(
            Guid id,
            WorkflowVersionRequest request,
            CancellationToken ct
        ) => _service.Publish(User, id, request, Key, ct);

        [HttpPost("advisories/{id:guid}/cancel")]
        public Task<WorkflowReceipt> Cancel(
            Guid id,
            AdvisoryCancelRequest request,
            CancellationToken ct
        ) => _service.Cancel(User, id, request, Key, ct);

        [HttpGet("advisories/{id:guid}/recipients")]
        public Task<object> Recipients(
            Guid id,
            CancellationToken ct,
            int page = 1,
            int pageSize = 25
        ) => _service.Recipients(User, id, page, pageSize, ct);

        [HttpGet("advisories/{id:guid}/audit")]
        public Task<object> Audit(Guid id, CancellationToken ct, int page = 1, int pageSize = 25) =>
            _service.AuditTrail(User, id, page, pageSize, ct);

        [HttpGet("advisories/{id:guid}/deliveries")]
        public Task<object> Deliveries(
            Guid id,
            CancellationToken ct,
            int page = 1,
            int pageSize = 25
        ) => _service.Deliveries(User, id, page, pageSize, ct);
    }

    [ApiController, FarmWorkflowErrors]
    [Authorize(AuthenticationSchemes = "Identity.Application", Roles = "admin")]
    [AutoValidateAntiforgeryToken]
    [Route("api/admin/farm-management/lab")]
    public class FarmLabAdminController : ControllerBase
    {
        private readonly FarmLabService _service;

        public FarmLabAdminController(FarmLabService service) => _service = service;

        [HttpGet("orchards/{orchardId:long}/reports")]
        public Task<object> History(
            long orchardId,
            CancellationToken ct,
            string type = null,
            int page = 1,
            int pageSize = 25
        ) => _service.History(User, orchardId, type, page, pageSize, true, ct);

        [HttpGet("reports/{reportId:guid}")]
        public Task<object> Detail(Guid reportId, CancellationToken ct) =>
            _service.Detail(User, reportId, true, ct);

        [HttpPost("orchards/{orchardId:long}/samples")]
        public Task<LabReceipt> Sample(
            long orchardId,
            LabSampleRequest request,
            CancellationToken ct
        ) => _service.CreateSample(User, orchardId, request, true, ct);

        [HttpPost("samples/{sampleId:guid}/reports")]
        public Task<LabReceipt> Report(
            Guid sampleId,
            LabReportRequest request,
            CancellationToken ct
        ) => _service.AddReport(User, sampleId, request, ct);

        [HttpPost("reports/{reportId:guid}/review")]
        public Task<LabReceipt> Review(
            Guid reportId,
            LabReviewRequest request,
            CancellationToken ct
        ) => _service.Review(User, reportId, request, ct);

        [HttpGet("rules")]
        public Task<object> Rules(CancellationToken ct) => _service.Rules(User, ct);

        [HttpPost("rules")]
        public Task<LabReceipt> CreateRule(
            NutrientRangeRuleRequest request,
            CancellationToken ct
        ) => _service.SaveRule(User, null, request, ct);

        [HttpPut("rules/{id:guid}")]
        public Task<LabReceipt> UpdateRule(
            Guid id,
            NutrientRangeRuleRequest request,
            CancellationToken ct
        ) => _service.SaveRule(User, id, request, ct);

        [HttpPost("rules/{id:guid}/approve")]
        public Task<LabReceipt> ApproveRule(
            Guid id,
            WorkflowVersionRequest request,
            CancellationToken ct
        ) => _service.ApproveRule(User, id, request, ct);
    }

    [ApiController, FarmWorkflowErrors, Authorize]
    [Route("api/core/farmer-dashboard/orchards/{orchardId:long}/lab")]
    public class FarmerLabController : ControllerBase
    {
        private readonly FarmLabService _service;

        public FarmerLabController(FarmLabService service) => _service = service;

        [HttpPost("samples")]
        public Task<LabReceipt> RequestSample(
            long orchardId,
            LabSampleRequest request,
            CancellationToken ct
        ) => _service.CreateSample(User, orchardId, request, false, ct);

        [HttpGet("reports")]
        public Task<object> History(
            long orchardId,
            CancellationToken ct,
            string type = null,
            int page = 1,
            int pageSize = 25
        ) => _service.History(User, orchardId, type, page, pageSize, false, ct);

        [HttpGet("reports/{reportId:guid}")]
        public Task<object> Detail(long orchardId, Guid reportId, CancellationToken ct) =>
            _service.Detail(User, reportId, false, ct, orchardId);

        [HttpGet("latest")]
        public Task<object> Latest(
            long orchardId,
            string type,
            CancellationToken ct,
            string crop = null,
            string variety = null,
            string growthStage = null
        ) => _service.Latest(User, orchardId, type, ct, crop, variety, growthStage);

        [HttpGet("trends")]
        public Task<object> Trends(
            long orchardId,
            string type,
            string analyte,
            DateTime? from,
            DateTime? to,
            CancellationToken ct
        ) => _service.Trends(User, orchardId, type, analyte, from, to, ct);
    }

    [ApiController, FarmWorkflowErrors]
    [Authorize(AuthenticationSchemes = "Identity.Application", Roles = "admin")]
    [AutoValidateAntiforgeryToken]
    [Route("api/admin/farm-management/expansion")]
    public class FarmExpansionAdminController : ControllerBase
    {
        private readonly FarmExpansionService _service;

        public FarmExpansionAdminController(FarmExpansionService service) => _service = service;

        [HttpGet("vendors")]
        public Task<object> Vendors(CancellationToken ct) => _service.VendorOptions(User, ct);

        [HttpGet("delegations")]
        public Task<object> Delegations(
            CancellationToken ct,
            long? vendorId = null,
            long? orchardId = null
        ) => _service.Delegations(User, vendorId, orchardId, ct);

        [HttpPost("delegations")]
        public Task<WorkflowReceipt> Grant(VendorDelegationRequest request, CancellationToken ct) =>
            _service.Grant(User, request, ct);

        [HttpPost("delegations/{id:guid}/revoke")]
        public Task<WorkflowReceipt> Revoke(
            Guid id,
            RevokeDelegationRequest request,
            CancellationToken ct
        ) => _service.Revoke(User, id, request, ct);

        [HttpGet("cases")]
        public Task<object> Cases(
            CancellationToken ct,
            long? orchardId = null,
            string kind = null,
            string status = null
        ) => _service.Cases(User, orchardId, kind, status, true, ct);

        [HttpPost("cases/{id:guid}/transition")]
        public Task<WorkflowReceipt> Transition(
            Guid id,
            FarmCaseTransitionRequest request,
            CancellationToken ct
        ) => _service.Transition(User, id, request, ct);

        [HttpGet("automation-rules")]
        public Task<object> Rules(CancellationToken ct) => _service.AutomationRules(User, ct);

        [HttpPost("automation-rules")]
        public Task<WorkflowReceipt> AddRule(AutomationRuleRequest request, CancellationToken ct) =>
            _service.SaveAutomation(User, request, ct);

        [HttpPost("automation-rules/{id:guid}/approve")]
        public Task<WorkflowReceipt> Approve(
            Guid id,
            WorkflowVersionRequest request,
            CancellationToken ct
        ) => _service.ApproveAutomation(User, id, request, ct);
    }

    [ApiController, FarmWorkflowErrors, Authorize]
    [Route("api/core/farm-support")]
    public class FarmerSupportController : ControllerBase
    {
        private readonly FarmExpansionService _service;

        public FarmerSupportController(FarmExpansionService service) => _service = service;

        [HttpGet("cases")]
        public Task<object> Cases(
            CancellationToken ct,
            long? orchardId = null,
            string kind = null,
            string status = null
        ) => _service.Cases(User, orchardId, kind, status, false, ct);

        [HttpPost("orchards/{orchardId:long}/cases")]
        public Task<WorkflowReceipt> Create(
            long orchardId,
            FarmCaseRequest request,
            CancellationToken ct
        ) => _service.CreateCase(User, orchardId, request, ct);

        [HttpPost("article-reads")]
        public Task<WorkflowReceipt> Read(ArticleReadRequest request, CancellationToken ct) =>
            _service.ArticleRead(User, request, ct);

        [HttpGet("capabilities")]
        public object Capabilities() =>
            new
            {
                tabs = new[]
                {
                    "alerts",
                    "pest-disease",
                    "nutrients",
                    "resources",
                    "financial",
                    "advisory",
                },
                historicalWeather = new
                {
                    dataStatus = "unavailable",
                    reason = "No approved historical observation provider is configured; forecast data is not used as observed history.",
                },
                hail = new
                {
                    dataStatus = "unavailable",
                    reason = "No verified hail-event source is configured.",
                },
                gdd = new
                {
                    dataStatus = "unavailable",
                    reason = "An approved crop base/cap/start-date rule and observed history are required.",
                },
                diseaseRisk = new
                {
                    dataStatus = "unavailable",
                    reason = "No approved risk model or provider entitlement is configured.",
                },
                resistance = new
                {
                    dataStatus = "unavailable",
                    reason = "Repeat use is not proof; an approved active-ingredient model or expert confirmation is required.",
                },
                automation = new
                {
                    dataStatus = "gated",
                    reason = "Only explicitly approved, versioned rules may be evaluated by a deployment worker.",
                },
            };
    }

    [ApiController, FarmWorkflowErrors, Authorize]
    [Route("api/vendor/farm-management")]
    public class VendorFarmManagementController : ControllerBase
    {
        private readonly FarmExpansionService _service;

        public VendorFarmManagementController(FarmExpansionService service) => _service = service;

        [HttpGet("orchards/{orchardId:long}")]
        public Task<object> Overview(long orchardId, CancellationToken ct) =>
            _service.VendorOverview(User, orchardId, ct);

        [HttpGet("orchards/{orchardId:long}/labs")]
        public Task<object> Labs(long orchardId, CancellationToken ct) =>
            _service.VendorLabs(User, orchardId, ct);

        [HttpPost("orchards/{orchardId:long}/support-cases")]
        public Task<WorkflowReceipt> Support(
            long orchardId,
            FarmCaseRequest request,
            CancellationToken ct
        ) => _service.CreateCase(User, orchardId, request, ct);
    }

    [ApiController, FarmWorkflowErrors, Authorize]
    [Route("api/core/farmer-dashboard/advisories")]
    public class FarmerAdvisoriesController : ControllerBase
    {
        private readonly FarmWorkflowService _service;
        private readonly FarmOperationService _operations;

        public FarmerAdvisoriesController(
            FarmWorkflowService service,
            FarmOperationService operations
        )
        {
            _service = service;
            _operations = operations;
        }

        private string Key => Request.Headers["Idempotency-Key"].ToString();

        [HttpGet]
        public Task<object> List(
            CancellationToken ct,
            long? orchardProfileId = null,
            int page = 1,
            int pageSize = 25
        ) => _service.Inbox(User, orchardProfileId, page, pageSize, ct);

        [HttpGet("{recipientId:guid}")]
        public Task<object> Detail(Guid recipientId, CancellationToken ct) =>
            _service.MyAdvisory(User, recipientId, ct);

        [HttpPost("{recipientId:guid}/read")]
        public Task<WorkflowReceipt> Read(Guid recipientId, CancellationToken ct) =>
            _service.Respond(User, recipientId, new AdvisoryAcknowledgeRequest(), Key, false, ct);

        [HttpPost("{recipientId:guid}/acknowledge")]
        public Task<WorkflowReceipt> Acknowledge(
            Guid recipientId,
            AdvisoryAcknowledgeRequest request,
            CancellationToken ct
        ) => _service.Respond(User, recipientId, request, Key, true, ct);

        [HttpPost("{recipientId:guid}/operations/spray")]
        public Task<OperationReceipt> Spray(
            Guid recipientId,
            SprayOperationRequest request,
            CancellationToken ct
        ) => _operations.Create(User, recipientId, "spray", request, Key, ct);

        [HttpPost("{recipientId:guid}/operations/fertigation")]
        public Task<OperationReceipt> Fertigation(
            Guid recipientId,
            FertigationOperationRequest request,
            CancellationToken ct
        ) => _operations.Create(User, recipientId, "fertigation", request, Key, ct);

        [HttpPost("{recipientId:guid}/operations/pruning")]
        public Task<OperationReceipt> Pruning(
            Guid recipientId,
            PruningOperationRequest request,
            CancellationToken ct
        ) => _operations.Create(User, recipientId, "pruning", request, Key, ct);

        [HttpPost("{recipientId:guid}/operations/harvest")]
        public Task<OperationReceipt> Harvest(
            Guid recipientId,
            HarvestOperationRequest request,
            CancellationToken ct
        ) => _operations.Create(User, recipientId, "harvest", request, Key, ct);
    }

    [ApiController, FarmWorkflowErrors, Authorize]
    [Route("api/core/farmer-dashboard/orchards/{orchardProfileId:long}/operations")]
    public class FarmerOperationsController : ControllerBase
    {
        private readonly FarmOperationService _service;

        public FarmerOperationsController(FarmOperationService service) => _service = service;

        private string Key => Request.Headers["Idempotency-Key"].ToString();

        [HttpGet]
        public Task<object> List(
            long orchardProfileId,
            CancellationToken ct,
            int? year = null,
            int page = 1,
            int pageSize = 25
        ) => _service.ListOwned(User, orchardProfileId, year, page, pageSize, ct);

        [HttpGet("{operationId:guid}")]
        public Task<object> Detail(long orchardProfileId, Guid operationId, CancellationToken ct) =>
            _service.Detail(User, orchardProfileId, operationId, ct);

        [HttpPost("{operationId:guid}/void")]
        public Task<OperationReceipt> Void(
            long orchardProfileId,
            Guid operationId,
            OperationVoidRequest request,
            CancellationToken ct
        ) => _service.Void(User, orchardProfileId, operationId, request, Key, ct);
    }

    [ApiController, FarmWorkflowErrors]
    [Authorize(AuthenticationSchemes = "Identity.Application", Roles = "admin")]
    [Route("api/admin/farm-management/operations")]
    public class FarmOperationsAdminController : ControllerBase
    {
        private readonly FarmOperationService _service;

        public FarmOperationsAdminController(FarmOperationService service) => _service = service;

        [HttpGet]
        public Task<object> List(
            CancellationToken ct,
            long? orchardProfileId = null,
            string kind = null,
            string status = null,
            int page = 1,
            int pageSize = 25
        ) => _service.ListAdmin(User, orchardProfileId, kind, status, page, pageSize, ct);

        [HttpGet("{operationId:guid}")]
        public Task<object> Detail(
            Guid operationId,
            [FromQuery] long orchardProfileId,
            CancellationToken ct
        ) => _service.Detail(User, orchardProfileId, operationId, ct, true);
    }
}
