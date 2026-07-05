namespace MarketStockManager.Models;

public class DashboardStats
{
    public int ProductCount { get; set; }
    public int LowStockCount { get; set; }
    public decimal StockValue { get; set; }
    public decimal TodaySales { get; set; }
    public int TodayReceipts { get; set; }
    public decimal TotalSales { get; set; }
}
