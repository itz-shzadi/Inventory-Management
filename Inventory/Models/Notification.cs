using System;

namespace InventoryMS.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // LowStock, OutOfStock, StockIn, StockOut, InvalidStock, NewProduct, UserActivity, DailySummary
        public string Severity { get; set; } // Success, Warning, Danger, Info
        public string Icon { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public int? UserId { get; set; } // Null for system-wide notifications
        public string UserRole { get; set; } // Admin, Manager, Staff, All
        public string RelatedEntityType { get; set; } // Product, Stock, User
        public int? RelatedEntityId { get; set; }
        public string ActionUrl { get; set; }
    }

    public class NotificationSettings
    {
        public int Id { get; set; }
        public bool EnableLowStockAlerts { get; set; } = true;
        public bool EnableOutOfStockAlerts { get; set; } = true;
        public bool EnableStockInAlerts { get; set; } = true;
        public bool EnableStockOutAlerts { get; set; } = true;
        public bool EnableDailySummary { get; set; } = false;
        public int LowStockThreshold { get; set; } = 10;
        public string DailySummaryTime { get; set; } = "18:00";
    }
}