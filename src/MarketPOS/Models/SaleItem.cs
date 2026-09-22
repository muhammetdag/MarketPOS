namespace MarketPOS.Models;

public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int? ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}
