namespace MarketPOS.Models;

public class Product
{
    public int Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal BuyPrice { get; set; }
    public decimal SellPrice { get; set; }
    public decimal StockQuantity { get; set; }
    public string Unit { get; set; } = "Adet";
    public int KdvRate { get; set; } = 1; // %1, %10, %20
    public bool IsFastProduct { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
