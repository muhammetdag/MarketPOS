using System.Text;
using MarketPOS.Data.Repositories;
using MarketPOS.Models;

namespace MarketPOS.Services;

public class ReceiptService
{
    private readonly SettingsRepository _settingsRepo = new();

    public string BuildReceiptText(Sale sale)
    {
        string marketName = _settingsRepo.Get("MarketName", "MARKET & ŞARKÜTERİ");
        string phone = _settingsRepo.Get("Phone", "");
        string address = _settingsRepo.Get("Address", "");
        string header = _settingsRepo.Get("ReceiptHeader", "HOŞGELDİNİZ");
        string footer = _settingsRepo.Get("ReceiptFooter", "BİZİ TERCİH ETTİĞİNİZ İÇİN TEŞEKKÜR EDERİZ");

        var sb = new StringBuilder();
        int width = 38; // Standard 58mm / 80mm column width

        sb.AppendLine(CenterText(marketName, width));
        if (!string.IsNullOrWhiteSpace(address)) sb.AppendLine(CenterText(address, width));
        if (!string.IsNullOrWhiteSpace(phone)) sb.AppendLine(CenterText($"TEL: {phone}", width));
        sb.AppendLine(new string('=', width));
        sb.AppendLine(CenterText(header, width));
        sb.AppendLine(new string('-', width));

        sb.AppendLine($"FİŞ NO : {sale.ReceiptNumber}");
        sb.AppendLine($"TARİH  : {sale.SaleDate:dd.MM.yyyy HH:mm:ss}");
        sb.AppendLine($"ÖDEME  : {sale.PaymentType.ToUpperInvariant()}");
        if (!string.IsNullOrWhiteSpace(sale.CustomerName))
        {
            sb.AppendLine($"MÜŞTERİ: {sale.CustomerName}");
        }
        sb.AppendLine(new string('-', width));

        sb.AppendLine(FormatLine("ÜRÜN ADI", "TUTAR", width));
        sb.AppendLine(new string('-', width));

        foreach (var item in sale.Items)
        {
            string itemLine = $"{item.ProductName}";
            if (itemLine.Length > width - 10) itemLine = itemLine.Substring(0, width - 10);
            sb.AppendLine(FormatLine(itemLine, $"{item.TotalPrice:N2} TL", width));

            if (item.Quantity != 1)
            {
                sb.AppendLine($"  {item.Quantity:0.##} x {item.UnitPrice:N2} TL");
            }
        }

        sb.AppendLine(new string('=', width));
        sb.AppendLine(FormatLine("TOPLAM TUTAR:", $"{sale.TotalAmount:N2} TL", width));

        if (sale.DiscountAmount > 0)
        {
            sb.AppendLine(FormatLine("İNDİRİM:", $"-{sale.DiscountAmount:N2} TL", width));
            sb.AppendLine(FormatLine("ÖDENEN NET:", $"{sale.FinalAmount:N2} TL", width));
        }

        if (sale.PaymentType == "Nakit" && sale.CashReceived > 0)
        {
            sb.AppendLine(FormatLine("ALINAN NAKİT:", $"{sale.CashReceived:N2} TL", width));
            sb.AppendLine(FormatLine("PARA ÜSTÜ:", $"{sale.ChangeAmount:N2} TL", width));
        }

        sb.AppendLine(new string('-', width));
        sb.AppendLine(CenterText(footer, width));
        sb.AppendLine(CenterText("YİNE BEKLERİZ", width));
        sb.AppendLine(new string('-', width));

        return sb.ToString();
    }

    private static string CenterText(string text, int width)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (text.Length >= width) return text;
        int pad = (width - text.Length) / 2;
        return text.PadLeft(pad + text.Length).PadRight(width);
    }

    private static string FormatLine(string left, string right, int width)
    {
        int spaces = width - left.Length - right.Length;
        if (spaces < 1) spaces = 1;
        return left + new string(' ', spaces) + right;
    }
}
