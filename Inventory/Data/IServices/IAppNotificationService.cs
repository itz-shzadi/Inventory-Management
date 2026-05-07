using System;
using System.Collections.Generic;
using InventoryMS.Models;

namespace InventoryMS.Services
{
    public interface IAppNotificationService  // Renamed from INotificationService
    {
        // ============ BASIC CRUD OPERATIONS ============
        void AddNotification(Notification notification);
        List<Notification> GetNotifications(int userId = 0, string userRole = null, int limit = 50);
        int GetUnreadCount(int userId = 0, string userRole = null);
        void MarkAsRead(int notificationId);
        void MarkAllAsRead(int userId = 0, string userRole = null);
        void DeleteNotification(int notificationId);
        void ClearAllNotifications();

        // ============ STOCK RELATED NOTIFICATIONS ============
        void LowStockAlert(string productName, int productId, int currentStock, int threshold);
        void OutOfStockAlert(string productName, int productId);
        void StockInNotification(string productName, int productId, int quantity, string addedBy);
        void StockOutNotification(string productName, int productId, int quantity, string removedBy, string reason);
        void InvalidStockAlert(string productName, int productId, int requestedQty, int availableQty, string attemptedBy);

        // ============ PRODUCT RELATED NOTIFICATIONS ============
        void NewProductNotification(string productName, int productId, string createdBy);

        // ============ USER ACTIVITY NOTIFICATIONS ============
        void UserActivityNotification(string action, string username, string details);

        // ============ REPORT NOTIFICATIONS ============
        void DailySummaryNotification(string role, Dictionary<string, object> summaryData);
    }
}