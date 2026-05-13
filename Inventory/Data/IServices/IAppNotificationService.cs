// Services/IAppNotificationService.cs
namespace Inventory.Services
{
    public interface IAppNotificationService
    {
        void LowStockAlert(string productName, int productId, int currentStock, int threshold);
        void OutOfStockAlert(string productName, int productId);
        void StockInNotification(string productName, int productId, int quantity, string userName);
        void StockOutNotification(string productName, int productId, int quantity, string userName, string reason);
        void InvalidStockAlert(string productName, int productId, int requestedQty, int availableQty, string userName);
        void NewProductNotification(string productName, int productId, string userName);
        void UserActivityNotification(string action, string userName, string details);
    }
}