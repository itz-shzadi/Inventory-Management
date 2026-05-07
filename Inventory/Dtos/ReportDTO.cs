using System;
using System.Collections.Generic;

namespace Inventory.DTOs
{
    public class LowStockReportDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public int CurrentStock { get; set; }
        public string Unit { get; set; }
        public int Threshold { get; set; } = 10;
    }

    public class StockSummaryDTO
    {
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal TotalStockValue { get; set; }
    }

    public class StockHistoryDTO
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } // StockIn or StockOut
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string Reference { get; set; } // Supplier or Reason
    }
}