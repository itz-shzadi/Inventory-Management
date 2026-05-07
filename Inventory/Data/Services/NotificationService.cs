using System;
using System.Collections.Generic;
using System.Linq;
using InventoryMS.Models;

namespace InventoryMS.Services
{
    public class AppNotificationService : IAppNotificationService  // Renamed
    {
        private static List<Notification> _notifications = new List<Notification>();
        private static int _nextId = 1;

        public void AddNotification(Notification notification)
        {
            notification.Id = _nextId++;
            notification.CreatedAt = DateTime.Now;
            notification.IsRead = false;
            _notifications.Insert(0, notification);

            if (_notifications.Count > 500)
            {
                _notifications = _notifications.Take(500).ToList();
            }
        }

        public List<Notification> GetNotifications(int userId = 0, string userRole = null, int limit = 50)
        {
            var query = _notifications.AsQueryable();

            if (userId > 0)
            {
                query = query.Where(n => n.UserId == null || n.UserId == userId);
            }

            if (!string.IsNullOrEmpty(userRole))
            {
                query = query.Where(n => n.UserRole == "All" || n.UserRole == userRole);
            }

            return query.Take(limit).ToList();
        }

        public int GetUnreadCount(int userId = 0, string userRole = null)
        {
            var query = _notifications.AsQueryable().Where(n => !n.IsRead);

            if (userId > 0)
            {
                query = query.Where(n => n.UserId == null || n.UserId == userId);
            }

            if (!string.IsNullOrEmpty(userRole))
            {
                query = query.Where(n => n.UserRole == "All" || n.UserRole == userRole);
            }

            return query.Count();
        }

        public void MarkAsRead(int notificationId)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
            }
        }

        public void MarkAllAsRead(int userId = 0, string userRole = null)
        {
            var query = _notifications.AsQueryable().Where(n => !n.IsRead);

            if (userId > 0)
            {
                query = query.Where(n => n.UserId == null || n.UserId == userId);
            }

            if (!string.IsNullOrEmpty(userRole))
            {
                query = query.Where(n => n.UserRole == "All" || n.UserRole == userRole);
            }

            foreach (var notification in query.ToList())
            {
                notification.IsRead = true;
            }
        }

        public void DeleteNotification(int notificationId)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                _notifications.Remove(notification);
            }
        }

        public void ClearAllNotifications()
        {
            _notifications.Clear();
        }

        public void LowStockAlert(string productName, int productId, int currentStock, int threshold)
        {
            AddNotification(new Notification
            {
                Title = "⚠️ Low Stock Alert",
                Message = $"Product '{productName}' has only {currentStock} units remaining. Threshold is {threshold} units.",
                Type = "LowStock",
                Severity = "Warning",
                Icon = "fa-exclamation-triangle",
                UserRole = "All",
                RelatedEntityType = "Product",
                RelatedEntityId = productId,
                ActionUrl = $"/Stock/CurrentStock?productId={productId}"
            });
        }

        public void OutOfStockAlert(string productName, int productId)
        {
            AddNotification(new Notification
            {
                Title = "❌ Out of Stock",
                Message = $"Product '{productName}' is now out of stock. Please restock immediately.",
                Type = "OutOfStock",
                Severity = "Danger",
                Icon = "fa-times-circle",
                UserRole = "All",
                RelatedEntityType = "Product",
                RelatedEntityId = productId,
                ActionUrl = $"/Stock/StockIn?productId={productId}"
            });
        }

        public void StockInNotification(string productName, int productId, int quantity, string addedBy)
        {
            AddNotification(new Notification
            {
                Title = "📦 Stock Added",
                Message = $"{quantity} units of '{productName}' have been added to inventory by {addedBy}.",
                Type = "StockIn",
                Severity = "Success",
                Icon = "fa-arrow-down",
                UserRole = "All",
                RelatedEntityType = "Product",
                RelatedEntityId = productId,
                ActionUrl = $"/Stock/CurrentStock"
            });
        }

        public void StockOutNotification(string productName, int productId, int quantity, string removedBy, string reason)
        {
            AddNotification(new Notification
            {
                Title = "📤 Stock Removed",
                Message = $"{quantity} units of '{productName}' have been removed by {removedBy}. Reason: {reason}",
                Type = "StockOut",
                Severity = "Info",
                Icon = "fa-arrow-up",
                UserRole = "All",
                RelatedEntityType = "Product",
                RelatedEntityId = productId,
                ActionUrl = $"/Stock/CurrentStock"
            });
        }

        public void InvalidStockAlert(string productName, int productId, int requestedQty, int availableQty, string attemptedBy)
        {
            AddNotification(new Notification
            {
                Title = "⚠️ Invalid Stock Operation",
                Message = $"{attemptedBy} attempted to remove {requestedQty} units of '{productName}' but only {availableQty} units are available.",
                Type = "InvalidStock",
                Severity = "Warning",
                Icon = "fa-ban",
                UserRole = "Admin",
                RelatedEntityType = "Product",
                RelatedEntityId = productId
            });
        }

        public void NewProductNotification(string productName, int productId, string createdBy)
        {
            AddNotification(new Notification
            {
                Title = "✨ New Product Added",
                Message = $"New product '{productName}' has been added to the system by {createdBy}.",
                Type = "NewProduct",
                Severity = "Success",
                Icon = "fa-box",
                UserRole = "All",
                RelatedEntityType = "Product",
                RelatedEntityId = productId,
                ActionUrl = $"/Product/Details/{productId}"
            });
        }

        public void UserActivityNotification(string action, string username, string details)
        {
            AddNotification(new Notification
            {
                Title = $"👤 User Activity: {action}",
                Message = $"{username} performed '{action}' operation. {details}",
                Type = "UserActivity",
                Severity = "Info",
                Icon = "fa-user-clock",
                UserRole = "Admin",
                ActionUrl = "/User/Index"
            });
        }

        public void DailySummaryNotification(string role, Dictionary<string, object> summaryData)
        {
            var message = $"Daily Summary: {summaryData["date"]}<br>";
            message += $"Total Products: {summaryData["totalProducts"]}<br>";
            message += $"Low Stock Items: {summaryData["lowStockCount"]}<br>";
            message += $"Out of Stock Items: {summaryData["outOfStockCount"]}<br>";
            message += $"Total Stock Value: ${summaryData["totalValue"]}<br>";
            message += $"Stock In Today: {summaryData["stockInToday"]}<br>";
            message += $"Stock Out Today: {summaryData["stockOutToday"]}";

            AddNotification(new Notification
            {
                Title = "📊 Daily Summary Report",
                Message = message,
                Type = "DailySummary",
                Severity = "Info",
                Icon = "fa-chart-line",
                UserRole = role,
                ActionUrl = "/Stock/Reports"
            });
        }
    }
}