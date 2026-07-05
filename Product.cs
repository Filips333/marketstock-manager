namespace MarketStockManager.Models;

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = "kom";
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal MinStock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public decimal StockValue => Math.Round(StockQuantity * PurchasePrice, 2);
    public decimal Margin => SalePrice - PurchasePrice;
    public decimal MarginPercent => SalePrice == 0 ? 0 : Math.Round((Margin / SalePrice) * 100, 2);
    public bool IsLowStock => StockQuantity <= MinStock;
}
