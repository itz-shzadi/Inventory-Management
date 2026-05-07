using System;

namespace Inventory.DTOs
{
    public class StockInDTO
    {
        public int StockInId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int QuantityAdded { get; set; }
        public DateTime Date { get; set; }
        public decimal UnitPrice { get; set; }
        public string? DocumentPath { get; set; }
        public string? Remarks { get; set; }
    }

    public class CreateStockInDTO
    {
        public int ProductId { get; set; }
        public int SupplierId { get; set; }
        public int QuantityAdded { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Remarks { get; set; }
        public DateTime Date { get; set; }
    }
}