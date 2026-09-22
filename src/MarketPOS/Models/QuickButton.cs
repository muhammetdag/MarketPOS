namespace MarketPOS.Models;

public class QuickButton
{
    public int SlotIndex { get; set; }
    public int? ProductId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#2C3E50";
}
