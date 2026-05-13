// Controllers/NotificationController.cs
using Inventory.Data;
using Inventory.DTOs;
using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Inventory.Controllers
{
    //[Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get current user ID from session
        [HttpGet]
        public IActionResult GetCurrentUserId()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                return Ok(userId.Value);
            }
            return Unauthorized(new { success = false, message = "User not logged in" });
        }

        // Get all notifications for current user
        [HttpGet]
        public async Task<IActionResult> GetUserNotifications()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "User not logged in" });
                }

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId.Value)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new NotificationDTO
                    {
                        NotificationId = n.NotificationId,
                        UserId = n.UserId,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt
                    })
                    .Take(50) // Limit to last 50 notifications
                    .ToListAsync();

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Get latest notifications (for polling)
        [HttpGet]
        public async Task<IActionResult> GetLatestNotifications(int lastId = 0)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Ok(new List<NotificationDTO>()); // Return empty list instead of error
                }

                var query = _context.Notifications
                    .Where(n => n.UserId == userId.Value);

                if (lastId > 0)
                {
                    query = query.Where(n => n.NotificationId > lastId);
                }

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new NotificationDTO
                    {
                        NotificationId = n.NotificationId,
                        UserId = n.UserId,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt
                    })
                    .Take(20)
                    .ToListAsync();

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return Ok(new List<NotificationDTO>()); // Return empty list on error
            }
        }

        // Get unread count
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Ok(0);
                }

                var count = await _context.Notifications
                    .CountAsync(n => n.UserId == userId.Value && n.IsRead == false);

                return Ok(count);
            }
            catch (Exception)
            {
                return Ok(0);
            }
        }

        // Mark single notification as read
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Unauthorized(new { success = false });
                }

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId.Value);

                if (notification != null)
                {
                    notification.IsRead = true;
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true });
            }
            catch (Exception)
            {
                return BadRequest(new { success = false });
            }
        }

        // Mark all as read
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Unauthorized(new { success = false });
                }

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId.Value && n.IsRead == false)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception)
            {
                return BadRequest(new { success = false });
            }
        }

        // Create notification (called from other services)
        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDTO model)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = model.UserId,
                    Title = model.Title,
                    Message = model.Message,
                    Type = model.Type,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, notificationId = notification.NotificationId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}