using System;

namespace Inventory.DTOs
{
    public class StockOutDTO
    {
        public int StockOutId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int QuantityRemoved { get; set; }
        public DateTime Date { get; set; }
        public string Reason { get; set; }
        public string Remarks { get; set; }
    }
}

namespace Inventory.DTOs
    {
        public class CreateStockOutDTO
        {
            public int ProductId { get; set; }
            public int QuantityRemoved { get; set; }
            public DateTime Date { get; set; }
            public string? Reason { get; set; }
            public string? Remarks { get; set; }
        }
    }
