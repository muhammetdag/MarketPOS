namespace MarketPOS.Models;

public class Sale
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string PaymentType { get; set; } = "Nakit"; // "Nakit", "KrediKarti", "Veresiye"
    public decimal CashReceived { get; set; }
    public decimal ChangeAmount { get; set; }
    public string? CustomerName { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.Now;

    public List<SaleItem> Items { get; set; } = new();
}
