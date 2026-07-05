namespace MarketStockManager.Models;

public class Sale
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
