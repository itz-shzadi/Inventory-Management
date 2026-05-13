// Services/AppNotificationService.cs - FIXED VERSION
using Inventory.Data;
using Inventory.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Inventory.Services
{
    public class AppNotificationService : IAppNotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        public AppNotificationService(ApplicationDbContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        private void CreateNotification(int userId, string title, string message, string type)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            _context.SaveChanges();
        }

        private int? GetAdminUserId()
        {
            // Try different property names
            var admin = _context.Users
                .FirstOrDefault(u => u.Role == "Admin" && u.isActive == true);

            if (admin == null) return null;

            // Check which property exists
            var userIdProperty = admin.GetType().GetProperty("UserId");
            if (userIdProperty != null)
                return (int)userIdProperty.GetValue(admin);

            var idProperty = admin.GetType().GetProperty("Id");
            if (idProperty != null)
                return (int)idProperty.GetValue(admin);

            var userIDProperty = admin.GetType().GetProperty("UserID");
            if (userIDProperty != null)
                return (int)userIDProperty.GetValue(admin);

            return null;
        }

        private int? GetManagerUserId()
        {
            var manager = _context.Users
                .FirstOrDefault(u => u.Role == "Manager" && u.isActive == true);

            if (manager == null) return null;

            var userIdProperty = manager.GetType().GetProperty("UserId");
            if (userIdProperty != null)
                return (int)userIdProperty.GetValue(manager);

            var idProperty = manager.GetType().GetProperty("Id");
            if (idProperty != null)
                return (int)idProperty.GetValue(manager);

            var userIDProperty = manager.GetType().GetProperty("UserID");
            if (userIDProperty != null)
                return (int)userIDProperty.GetValue(manager);

            return null;
        }

        public void LowStockAlert(string productName, int productId, int currentStock, int threshold)
        {
            var adminId = GetAdminUserId();
            var managerId = GetManagerUserId();

            var message = $"Product '{productName}' has low stock! Only {currentStock} units remaining (Threshold: {threshold})";

            if (adminId.HasValue)
                CreateNotification(adminId.Value, "Low Stock Alert", message, "LowStock");

            if (managerId.HasValue)
                CreateNotification(managerId.Value, "Low Stock Alert", message, "LowStock");
        }

        public void OutOfStockAlert(string productName, int productId)
        {
            var adminId = GetAdminUserId();
            var managerId = GetManagerUserId();

            var message = $"Product '{productName}' is OUT OF STOCK! Please restock immediately.";

            if (adminId.HasValue)
                CreateNotification(adminId.Value, "Out of Stock Alert", message, "OutOfStock");

            if (managerId.HasValue)
                CreateNotification(managerId.Value, "Out of Stock Alert", message, "OutOfStock");
        }

        public void StockInNotification(string productName, int productId, int quantity, string userName)
        {
            var adminId = GetAdminUserId();
            var managerId = GetManagerUserId();

            var message = $"{userName} added {quantity} units of '{productName}' to stock.";

            if (adminId.HasValue)
                CreateNotification(adminId.Value, "Stock In", message, "StockIn");

            if (managerId.HasValue)
                CreateNotification(managerId.Value, "Stock In", message, "StockIn");
        }

        public void StockOutNotification(string productName, int productId, int quantity, string userName, string reason)
        {
            var adminId = GetAdminUserId();
            var managerId = GetManagerUserId();

            var message = $"{userName} removed {quantity} units of '{productName}' (Reason: {reason})";

            if (adminId.HasValue)
                CreateNotification(adminId.Value, "Stock Out", message, "StockOut");

            if (managerId.HasValue)
                CreateNotification(managerId.Value, "Stock Out", message, "StockOut");
        }

        public void InvalidStockAlert(string productName, int productId, int requestedQty, int availableQty, string userName)
        {
            var adminId = GetAdminUserId();

            var message = $"{userName} attempted to remove {requestedQty} units of '{productName}' but only {availableQty} available.";

            if (adminId.HasValue)
                CreateNotification(adminId.Value, "Invalid Stock Operation", message, "InvalidStock");
        }

        public void NewProductNotification(string productName, int productId, string userName)
        {
            var adminId = GetAdminUserId();
            var managerId = GetManagerUserId();

            var message = $"{userName} added new product: '{productName}'";

            if (adminId.HasValue)
                CreateNotification(adminId.Value, "New Product", message, "NewProduct");

            if (managerId.HasValue)
                CreateNotification(managerId.Value, "New Product", message, "NewProduct");
        }

        public void UserActivityNotification(string action, string userName, string details)
        {
            var adminId = GetAdminUserId();

            var message = $"{userName} performed '{action}': {details}";

            if (adminId.HasValue)
                CreateNotification(adminId.Value, "User Activity", message, "UserActivity");
        }
    }
}