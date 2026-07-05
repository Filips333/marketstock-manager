namespace MarketStockManager.Models;

public class SaleCartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal AvailableStock { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Math.Round(Quantity * UnitPrice, 2);
}
