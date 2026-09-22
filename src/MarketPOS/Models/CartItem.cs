namespace MarketPOS.Models;

public class CartItem
{
    public int LineNumber { get; set; }
    public int ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; } = 1;
    public string Unit { get; set; } = "Adet";
    public decimal TotalPrice => UnitPrice * Quantity;
}
