// DTO Classes
public class DashboardStatsDto
{
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public decimal TotalValue { get; set; }
    public int CategoryCount { get; set; }
}

public class CategoryStockDto
{
    public string CategoryName { get; set; }
    public int TotalStock { get; set; }
}

public class StockStatusDto
{
    public int InStock { get; set; }
    public int LowStock { get; set; }
    public int OutOfStock { get; set; }
}

public class RecentProductDto
{
    public string Name { get; set; }
    public string CategoryName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; }
}
