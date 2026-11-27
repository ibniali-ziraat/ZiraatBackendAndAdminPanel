using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Module.Notifications.Areas.Notifications.ViewModels;
using ECommerce.Module.Notifications.Models;
using ECommerce.Module.Notifications.Notifiers;

namespace ECommerce.Module.Notifications.Areas.Notifications.Controllers
{
    [Area("Notifications")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class NotificationsController : Controller
    {
        private readonly ITestNotifier _testNotifier;

        public NotificationsController(ITestNotifier testNotifier)
        {
            _testNotifier = testNotifier;
        }

        [HttpGet("notifications")]
        public IActionResult Index()
        {
            return View();
        }

        #region Etc
        [HttpPost]
        public async Task<ActionResult> TestNotification(TestNotificationVm inputDto)
        {
            if (string.IsNullOrWhiteSpace(inputDto.Message))
            {
                inputDto.Message = "This is a test notification, created at " + DateTime.Now;
            }

            if (!Enum.TryParse<NotificationSeverity>(inputDto.Severity, true, out var severity))
            {
                severity = NotificationSeverity.Info;
            }

            await _testNotifier.SendMessageAsync(
                inputDto.UserId,
                inputDto.Message,
                severity
                );

            return RedirectToAction("Index");
        }

        #endregion
    }
}
