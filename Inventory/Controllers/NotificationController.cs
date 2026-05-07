using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using InventoryMS.Services;
using System;
using System.Linq;

namespace InventoryMS.Controllers
{
    public class NotificationController : Controller
    {
        private readonly IAppNotificationService _notificationService;

        public NotificationController(IAppNotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public IActionResult GetNotifications(int limit = 50)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var userRole = HttpContext.Session.GetString("UserRole") ?? "All";

                var notifications = _notificationService.GetNotifications(userId, userRole, limit);
                var unreadCount = _notificationService.GetUnreadCount(userId, userRole);

                return Json(new
                {
                    success = true,
                    notifications = notifications.Select(n => new {
                        n.Id,
                        n.Title,
                        n.Message,
                        n.Type,
                        n.Severity,
                        n.Icon,
                        CreatedAt = n.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        n.IsRead,
                        n.ActionUrl
                    }),
                    unreadCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message, notifications = new object[0], unreadCount = 0 });
            }
        }

        [HttpPost]
        public IActionResult MarkAsRead(int id)
        {
            try
            {
                _notificationService.MarkAsRead(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult MarkAllAsRead()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var userRole = HttpContext.Session.GetString("UserRole");
                _notificationService.MarkAllAsRead(userId, userRole);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeleteNotification(int id)
        {
            try
            {
                _notificationService.DeleteNotification(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}