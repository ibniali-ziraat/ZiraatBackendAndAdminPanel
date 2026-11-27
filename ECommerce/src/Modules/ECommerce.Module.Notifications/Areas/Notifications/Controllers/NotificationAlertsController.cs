using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Notifications.Models;
using ECommerce.Module.Notifications.Services;
using ECommerce.Module.Orders.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Notifications.Areas.Notifications.Controllers
{
    [Area("Notifications")]
    [ApiController]
    [Authorize]
    [Route("api/notifications")]
    public class NotificationAlertsController : ControllerBase
    {
        private const int DefaultCreditDueDays = 30;
        private const int DefaultNearDueDays = 3;

        private readonly IUserNotificationManager _userNotificationManager;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly IWorkContext _workContext;
        private readonly IRepository<VendorCustomerAssociation> _vendorCustomerAssociationRepository;
        private readonly IRepository<CustomerSettlement> _customerSettlementRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly ICurrencyService _currencyService;

        public NotificationAlertsController(
            IUserNotificationManager userNotificationManager,
            INotificationPublisher notificationPublisher,
            IWorkContext workContext,
            IRepository<VendorCustomerAssociation> vendorCustomerAssociationRepository,
            IRepository<CustomerSettlement> customerSettlementRepository,
            IRepository<Order> orderRepository,
            ICurrencyService currencyService)
        {
            _userNotificationManager = userNotificationManager;
            _notificationPublisher = notificationPublisher;
            _workContext = workContext;
            _vendorCustomerAssociationRepository = vendorCustomerAssociationRepository;
            _customerSettlementRepository = customerSettlementRepository;
            _orderRepository = orderRepository;
            _currencyService = currencyService;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] UserNotificationState? state, [FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            var currentUser = await _workContext.GetCurrentUser();
            take = take <= 0 ? 20 : Math.Min(take, 100);

            var notifications = await _userNotificationManager.GetUserNotificationsAsync(currentUser.Id, state, skip, take);
            var unreadCount = await _userNotificationManager.GetUserNotificationCountAsync(currentUser.Id, UserNotificationState.Unread);

            return Ok(new
            {
                unreadCount,
                notifications
            });
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var currentUser = await _workContext.GetCurrentUser();
            var count = await _userNotificationManager.GetUserNotificationCountAsync(currentUser.Id, UserNotificationState.Unread);

            return Ok(new { count });
        }

        [HttpPost("{notificationId:guid}/read")]
        public async Task<IActionResult> MarkRead(Guid notificationId)
        {
            var currentUser = await _workContext.GetCurrentUser();
            var notification = await _userNotificationManager.GetUserNotificationAsync(notificationId);
            if (notification == null)
            {
                return NotFound();
            }

            if (notification.UserId != currentUser.Id && !User.IsInRole("admin"))
            {
                return Forbid();
            }

            await _userNotificationManager.UpdateUserNotificationStateAsync(notificationId, UserNotificationState.Read);
            return Accepted();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var currentUser = await _workContext.GetCurrentUser();
            await _userNotificationManager.UpdateAllUserNotificationStatesAsync(currentUser.Id, UserNotificationState.Read);

            return Accepted();
        }

        [HttpPost("admin/send")]
        [Authorize(Roles = "admin, vendor")]
        public async Task<IActionResult> SendManual([FromBody] SendNotificationRequest model)
        {
            if (model == null || model.UserIds == null || model.UserIds.Length == 0)
            {
                return BadRequest(new { error = "At least one userId is required." });
            }

            if (string.IsNullOrWhiteSpace(model.Message))
            {
                return BadRequest(new { error = "Message is required." });
            }

            await _notificationPublisher.PublishAsync(
                NotificationDefinitions.Names.ManualCustomerNotification,
                new AlertNotificationData
                {
                    Title = string.IsNullOrWhiteSpace(model.Title) ? "Notification" : model.Title,
                    Message = model.Message,
                    AlertType = string.IsNullOrWhiteSpace(model.AlertType) ? "manual" : model.AlertType,
                    TargetApp = string.IsNullOrWhiteSpace(model.TargetApp) ? "customer" : model.TargetApp,
                    OrderId = model.OrderId,
                    VendorId = model.VendorId,
                    CustomerId = model.CustomerId,
                    Balance = model.Balance,
                    DueDate = model.DueDate
                },
                model.OrderId.HasValue ? new EntityIdentifier(typeof(Order), model.OrderId.Value) : null,
                model.Severity,
                model.UserIds.Distinct().ToArray());

            return Accepted(new { sent = model.UserIds.Distinct().Count() });
        }

        [HttpPost("admin/credit-customers/reminders")]
        [Authorize(Roles = "admin, vendor")]
        public async Task<IActionResult> SendCreditReminders([FromBody] CreditReminderRequest model)
        {
            model ??= new CreditReminderRequest();
            var dueDays = model.DueDays <= 0 ? DefaultCreditDueDays : model.DueDays;
            var nearDueDays = model.NearDueDays <= 0 ? DefaultNearDueDays : model.NearDueDays;
            var today = DateTime.Today;

            var associationsQuery = _vendorCustomerAssociationRepository.Query()
                .Where(x => x.AllowCredit && x.IsActive && !x.IsDeleted);

            if (model.VendorId.HasValue)
            {
                associationsQuery = associationsQuery.Where(x => x.VendorId == model.VendorId.Value);
            }

            if (model.CustomerId.HasValue)
            {
                associationsQuery = associationsQuery.Where(x => x.CustomerId == model.CustomerId.Value);
            }

            var associations = await associationsQuery.ToListAsync();
            var sent = new List<CreditReminderResult>();

            foreach (var association in associations)
            {
                var creditState = await BuildCreditState(association.VendorId, association.CustomerId, dueDays, today);
                if (creditState == null || creditState.Outstanding <= 0)
                {
                    continue;
                }

                var reminderType = ResolveReminderType(creditState, today, nearDueDays);
                if (reminderType == null || (model.ReminderType.HasValue && model.ReminderType.Value != reminderType.Value))
                {
                    continue;
                }

                var notificationName = GetCreditNotificationName(reminderType.Value);
                var message = BuildCreditMessage(reminderType.Value, creditState.Outstanding, creditState.DueDate);

                await _notificationPublisher.PublishAsync(
                    notificationName,
                    new AlertNotificationData
                    {
                        Title = GetCreditTitle(reminderType.Value),
                        Message = message,
                        AlertType = GetCreditAlertType(reminderType.Value),
                        TargetApp = "customer",
                        VendorId = association.VendorId,
                        CustomerId = association.CustomerId,
                        Balance = creditState.Outstanding,
                        DueDate = creditState.DueDate
                    },
                    null,
                    reminderType.Value == CreditReminderType.Overdue ? NotificationSeverity.Warn : NotificationSeverity.Info,
                    new[] { association.CustomerId });

                sent.Add(new CreditReminderResult
                {
                    VendorId = association.VendorId,
                    CustomerId = association.CustomerId,
                    ReminderType = reminderType.Value.ToString(),
                    Outstanding = creditState.Outstanding,
                    DueDate = creditState.DueDate
                });
            }

            return Accepted(new
            {
                sent = sent.Count,
                reminders = sent
            });
        }

        private async Task<CreditState> BuildCreditState(long vendorId, long customerId, int dueDays, DateTime today)
        {
            var orderSlices = await _orderRepository.Query()
                .Where(x =>
                    !x.IsMasterOrder &&
                    x.CustomerId == customerId &&
                    x.PaymentMethod == "BuyOnCredit" &&
                    (x.VendorId == vendorId || x.OrderItems.Any(i => i.VendorId == vendorId)))
                .Select(x => new
                {
                    x.Id,
                    x.CreatedOn,
                    x.OrderTotal,
                    x.VendorId,
                    VendorItemsTotal = x.OrderItems
                        .Where(i => i.VendorId == vendorId)
                        .Sum(i => (i.ProductPrice * i.Quantity) + i.TaxAmount - i.DiscountAmount)
                })
                .ToListAsync();

            var orders = orderSlices
                .Select(x => new CreditOrderSlice
                {
                    OrderId = x.Id,
                    OrderedDate = x.CreatedOn.DateTime,
                    Amount = x.VendorId == vendorId ? x.OrderTotal : x.VendorItemsTotal
                })
                .Where(x => x.Amount > 0)
                .OrderBy(x => x.OrderedDate)
                .ToList();

            if (!orders.Any())
            {
                return null;
            }

            var settled = await _customerSettlementRepository.Query()
                .Where(x => x.VendorId == vendorId && x.CustomerId == customerId && !x.IsDeleted)
                .SumAsync(x => x.AmountPaid);

            var outstanding = orders.Sum(x => x.Amount) - settled;
            if (outstanding <= 0)
            {
                return null;
            }

            foreach (var order in orders)
            {
                if (settled >= order.Amount)
                {
                    settled -= order.Amount;
                    continue;
                }

                var unpaidAmount = order.Amount - settled;
                var dueDate = order.OrderedDate.Date.AddDays(dueDays);
                return new CreditState
                {
                    OrderId = order.OrderId,
                    Outstanding = outstanding,
                    OldestUnpaidAmount = unpaidAmount,
                    DueDate = dueDate
                };
            }

            return null;
        }

        private static CreditReminderType? ResolveReminderType(CreditState creditState, DateTime today, int nearDueDays)
        {
            if (creditState.DueDate.Date < today)
            {
                return CreditReminderType.Overdue;
            }

            if (creditState.DueDate.Date == today)
            {
                return CreditReminderType.Due;
            }

            if (creditState.DueDate.Date <= today.AddDays(nearDueDays))
            {
                return CreditReminderType.NearDue;
            }

            return null;
        }

        private static string GetCreditNotificationName(CreditReminderType reminderType)
        {
            return reminderType switch
            {
                CreditReminderType.Overdue => NotificationDefinitions.Names.CustomerCreditPaymentOverdue,
                CreditReminderType.NearDue => NotificationDefinitions.Names.CustomerCreditPaymentNearDue,
                _ => NotificationDefinitions.Names.CustomerCreditPaymentDue
            };
        }

        private static string GetCreditTitle(CreditReminderType reminderType)
        {
            return reminderType switch
            {
                CreditReminderType.Overdue => "Payment overdue",
                CreditReminderType.NearDue => "Payment due soon",
                _ => "Payment due today"
            };
        }

        private static string GetCreditAlertType(CreditReminderType reminderType)
        {
            return reminderType switch
            {
                CreditReminderType.Overdue => "credit-payment-overdue",
                CreditReminderType.NearDue => "credit-payment-near-due",
                _ => "credit-payment-due"
            };
        }

        private string BuildCreditMessage(CreditReminderType reminderType, decimal outstanding, DateTime dueDate)
        {
            var amount = _currencyService.FormatCurrency(outstanding);
            return reminderType switch
            {
                CreditReminderType.Overdue => $"Your credit payment of {amount} was due on {dueDate:dd MMM yyyy}.",
                CreditReminderType.NearDue => $"Your credit payment of {amount} is due on {dueDate:dd MMM yyyy}.",
                _ => $"Your credit payment of {amount} is due today."
            };
        }

        public class SendNotificationRequest
        {
            public long[] UserIds { get; set; }

            public string Title { get; set; }

            public string Message { get; set; }

            public string AlertType { get; set; }

            public string TargetApp { get; set; }

            public long? OrderId { get; set; }

            public long? VendorId { get; set; }

            public long? CustomerId { get; set; }

            public decimal? Balance { get; set; }

            public DateTime? DueDate { get; set; }

            public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
        }

        public class CreditReminderRequest
        {
            public long? VendorId { get; set; }

            public long? CustomerId { get; set; }

            public CreditReminderType? ReminderType { get; set; }

            public int DueDays { get; set; } = DefaultCreditDueDays;

            public int NearDueDays { get; set; } = DefaultNearDueDays;
        }

        public enum CreditReminderType
        {
            NearDue = 1,

            Due = 2,

            Overdue = 3
        }

        private class CreditOrderSlice
        {
            public long OrderId { get; set; }

            public DateTime OrderedDate { get; set; }

            public decimal Amount { get; set; }
        }

        private class CreditState
        {
            public long OrderId { get; set; }

            public decimal Outstanding { get; set; }

            public decimal OldestUnpaidAmount { get; set; }

            public DateTime DueDate { get; set; }
        }

        private class CreditReminderResult
        {
            public long VendorId { get; set; }

            public long CustomerId { get; set; }

            public string ReminderType { get; set; }

            public decimal Outstanding { get; set; }

            public DateTime DueDate { get; set; }
        }
    }
}
