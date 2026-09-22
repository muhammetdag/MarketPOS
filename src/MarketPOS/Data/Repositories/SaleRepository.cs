using Microsoft.Data.Sqlite;
using MarketPOS.Models;

namespace MarketPOS.Data.Repositories;

public class DailySummary
{
    public decimal TotalTurnover { get; set; }
    public decimal CashTotal { get; set; }
    public decimal CardTotal { get; set; }
    public decimal OtherTotal { get; set; }
    public int ReceiptCount { get; set; }
    public decimal TotalItemsSold { get; set; }
}

public class TopSellingItem
{
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
}

public class SaleRepository
{
    public string GenerateReceiptNumber()
    {
        string datePrefix = DateTime.Now.ToString("yyyyMMdd");
        using var conn = DatabaseContext.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*) FROM Sales 
            WHERE ReceiptNumber LIKE @prefix;
        ";
        cmd.Parameters.AddWithValue("@prefix", $"F-{datePrefix}-%");
        long count = (long)(cmd.ExecuteScalar() ?? 0L);
        return $"F-{datePrefix}-{(count + 1):D4}";
    }

    public int CompleteSale(Sale sale)
    {
        using var conn = DatabaseContext.CreateConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            if (string.IsNullOrWhiteSpace(sale.ReceiptNumber))
            {
                sale.ReceiptNumber = GenerateReceiptNumber();
            }

            // 1. Insert Sale record
            using var saleCmd = conn.CreateCommand();
            saleCmd.Transaction = tx;
            saleCmd.CommandText = @"
                INSERT INTO Sales (ReceiptNumber, TotalAmount, DiscountAmount, FinalAmount, PaymentType, CashReceived, ChangeAmount, CustomerName, SaleDate)
                VALUES (@rcp, @tot, @disc, @fin, @pay, @cash, @chg, @cust, @date);
                SELECT last_insert_rowid();
            ";
            saleCmd.Parameters.AddWithValue("@rcp", sale.ReceiptNumber);
            saleCmd.Parameters.AddWithValue("@tot", sale.TotalAmount);
            saleCmd.Parameters.AddWithValue("@disc", sale.DiscountAmount);
            saleCmd.Parameters.AddWithValue("@fin", sale.FinalAmount);
            saleCmd.Parameters.AddWithValue("@pay", sale.PaymentType);
            saleCmd.Parameters.AddWithValue("@cash", sale.CashReceived);
            saleCmd.Parameters.AddWithValue("@chg", sale.ChangeAmount);
            saleCmd.Parameters.AddWithValue("@cust", (object?)sale.CustomerName ?? DBNull.Value);
            saleCmd.Parameters.AddWithValue("@date", sale.SaleDate.ToString("yyyy-MM-dd HH:mm:ss"));

            int saleId = Convert.ToInt32(saleCmd.ExecuteScalar());
            sale.Id = saleId;

            // 2. Insert Sale Items and deduct stock
            foreach (var item in sale.Items)
            {
                using var itemCmd = conn.CreateCommand();
                itemCmd.Transaction = tx;
                itemCmd.CommandText = @"
                    INSERT INTO SaleItems (SaleId, ProductId, Barcode, ProductName, UnitPrice, Quantity, TotalPrice)
                    VALUES (@sid, @pid, @bcode, @pname, @uprice, @qty, @tprice);
                ";
                itemCmd.Parameters.AddWithValue("@sid", saleId);
                itemCmd.Parameters.AddWithValue("@pid", (object?)item.ProductId ?? DBNull.Value);
                itemCmd.Parameters.AddWithValue("@bcode", item.Barcode);
                itemCmd.Parameters.AddWithValue("@pname", item.ProductName);
                itemCmd.Parameters.AddWithValue("@uprice", item.UnitPrice);
                itemCmd.Parameters.AddWithValue("@qty", item.Quantity);
                itemCmd.Parameters.AddWithValue("@tprice", item.TotalPrice);
                itemCmd.ExecuteNonQuery();

                // Deduct stock if product ID is present
                if (item.ProductId.HasValue && item.ProductId.Value > 0)
                {
                    using var stockCmd = conn.CreateCommand();
                    stockCmd.Transaction = tx;
                    stockCmd.CommandText = @"
                        UPDATE Products 
                        SET StockQuantity = StockQuantity - @qty,
                            UpdatedAt = datetime('now')
                        WHERE Id = @pid;
                    ";
                    stockCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    stockCmd.Parameters.AddWithValue("@pid", item.ProductId.Value);
                    stockCmd.ExecuteNonQuery();
                }
            }

            tx.Commit();
            return saleId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public DailySummary GetDailySummary(DateTime date)
    {
        var summary = new DailySummary();
        string start = date.Date.ToString("yyyy-MM-dd 00:00:00");
        string end = date.Date.ToString("yyyy-MM-dd 23:59:59");

        using var conn = DatabaseContext.CreateConnection();

        // Totals by payment type
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT 
                    COALESCE(SUM(FinalAmount), 0),
                    COALESCE(SUM(CASE WHEN PaymentType = 'Nakit' THEN FinalAmount ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN PaymentType = 'KrediKarti' THEN FinalAmount ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN PaymentType NOT IN ('Nakit', 'KrediKarti') THEN FinalAmount ELSE 0 END), 0),
                    COUNT(*)
                FROM Sales
                WHERE SaleDate BETWEEN @start AND @end;
            ";
            cmd.Parameters.AddWithValue("@start", start);
            cmd.Parameters.AddWithValue("@end", end);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                summary.TotalTurnover = reader.GetDecimal(0);
                summary.CashTotal = reader.GetDecimal(1);
                summary.CardTotal = reader.GetDecimal(2);
                summary.OtherTotal = reader.GetDecimal(3);
                summary.ReceiptCount = reader.GetInt32(4);
            }
        }

        // Total items sold
        using (var itemCmd = conn.CreateCommand())
        {
            itemCmd.CommandText = @"
                SELECT COALESCE(SUM(si.Quantity), 0)
                FROM SaleItems si
                INNER JOIN Sales s ON si.SaleId = s.Id
                WHERE s.SaleDate BETWEEN @start AND @end;
            ";
            itemCmd.Parameters.AddWithValue("@start", start);
            itemCmd.Parameters.AddWithValue("@end", end);
            summary.TotalItemsSold = Convert.ToDecimal(itemCmd.ExecuteScalar() ?? 0m);
        }

        return summary;
    }

    public List<Sale> GetSalesByDateRange(DateTime start, DateTime end)
    {
        var list = new List<Sale>();
        string startStr = start.ToString("yyyy-MM-dd 00:00:00");
        string endStr = end.ToString("yyyy-MM-dd 23:59:59");

        using var conn = DatabaseContext.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, ReceiptNumber, TotalAmount, DiscountAmount, FinalAmount, PaymentType, CashReceived, ChangeAmount, CustomerName, SaleDate
            FROM Sales
            WHERE SaleDate BETWEEN @start AND @end
            ORDER BY SaleDate DESC;
        ";
        cmd.Parameters.AddWithValue("@start", startStr);
        cmd.Parameters.AddWithValue("@end", endStr);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Sale
            {
                Id = reader.GetInt32(0),
                ReceiptNumber = reader.GetString(1),
                TotalAmount = reader.GetDecimal(2),
                DiscountAmount = reader.GetDecimal(3),
                FinalAmount = reader.GetDecimal(4),
                PaymentType = reader.GetString(5),
                CashReceived = reader.GetDecimal(6),
                ChangeAmount = reader.GetDecimal(7),
                CustomerName = reader.IsDBNull(8) ? null : reader.GetString(8),
                SaleDate = DateTime.TryParse(reader.GetString(9), out var dt) ? dt : DateTime.Now
            });
        }
        return list;
    }

    public List<SaleItem> GetSaleItems(int saleId)
    {
        var items = new List<SaleItem>();
        using var conn = DatabaseContext.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, SaleId, ProductId, Barcode, ProductName, UnitPrice, Quantity, TotalPrice
            FROM SaleItems
            WHERE SaleId = @sid;
        ";
        cmd.Parameters.AddWithValue("@sid", saleId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new SaleItem
            {
                Id = reader.GetInt32(0),
                SaleId = reader.GetInt32(1),
                ProductId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Barcode = reader.IsDBNull(3) ? "" : reader.GetString(3),
                ProductName = reader.GetString(4),
                UnitPrice = reader.GetDecimal(5),
                Quantity = reader.GetDecimal(6),
                TotalPrice = reader.GetDecimal(7)
            });
        }
        return items;
    }

    public List<TopSellingItem> GetTopSellingProducts(DateTime start, DateTime end, int limit = 10)
    {
        var list = new List<TopSellingItem>();
        string startStr = start.ToString("yyyy-MM-dd 00:00:00");
        string endStr = end.ToString("yyyy-MM-dd 23:59:59");

        using var conn = DatabaseContext.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT si.Barcode, si.ProductName, SUM(si.Quantity) as TotalQty, SUM(si.TotalPrice) as TotalAmt
            FROM SaleItems si
            INNER JOIN Sales s ON si.SaleId = s.Id
            WHERE s.SaleDate BETWEEN @start AND @end
            GROUP BY si.Barcode, si.ProductName
            ORDER BY TotalQty DESC
            LIMIT @lim;
        ";
        cmd.Parameters.AddWithValue("@start", startStr);
        cmd.Parameters.AddWithValue("@end", endStr);
        cmd.Parameters.AddWithValue("@lim", limit);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new TopSellingItem
            {
                Barcode = reader.IsDBNull(0) ? "" : reader.GetString(0),
                ProductName = reader.GetString(1),
                TotalQuantity = reader.GetDecimal(2),
                TotalAmount = reader.GetDecimal(3)
            });
        }
        return list;
    }

    public void CancelSale(int saleId)
    {
        using var conn = DatabaseContext.CreateConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            var items = GetSaleItems(saleId);
            // Refund stock
            foreach (var item in items)
            {
                if (item.ProductId.HasValue && item.ProductId.Value > 0)
                {
                    using var stockCmd = conn.CreateCommand();
                    stockCmd.Transaction = tx;
                    stockCmd.CommandText = @"
                        UPDATE Products 
                        SET StockQuantity = StockQuantity + @qty,
                            UpdatedAt = datetime('now')
                        WHERE Id = @pid;
                    ";
                    stockCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    stockCmd.Parameters.AddWithValue("@pid", item.ProductId.Value);
                    stockCmd.ExecuteNonQuery();
                }
            }

            // Delete sale (cascade deletes SaleItems)
            using var delCmd = conn.CreateCommand();
            delCmd.Transaction = tx;
            delCmd.CommandText = "DELETE FROM Sales WHERE Id = @sid;";
            delCmd.Parameters.AddWithValue("@sid", saleId);
            delCmd.ExecuteNonQuery();

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
