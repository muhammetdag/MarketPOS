using Microsoft.Data.Sqlite;
using MarketPOS.Models;

namespace MarketPOS.Data.Repositories;

public class ProductRepository
{
    public static event Action? OnProductsChanged;

    public Product? GetByBarcode(string barcode)
    {
        using var conn = DatabaseContext.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, Barcode, Name, BuyPrice, SellPrice, StockQuantity, Unit, KdvRate, IsFastProduct, CreatedAt, UpdatedAt
            FROM Products 
            WHERE Barcode = @b
            LIMIT 1;
        ";
        cmd.Parameters.AddWithValue("@b", barcode.Trim());

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapProduct(reader);
        }
        return null;
    }

    public Product? GetById(int id)
    {
        using var conn = DatabaseContext.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, Barcode, Name, BuyPrice, SellPrice, StockQuantity, Unit, KdvRate, IsFastProduct, CreatedAt, UpdatedAt
            FROM Products 
            WHERE Id = @id
            LIMIT 1;
        ";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapProduct(reader);
        }
        return null;
    }

    public List<Product> Search(string query, int limit = 50)
    {
        var list = new List<Product>();
        using var conn = DatabaseContext.CreateConnection();
        using var cmd = conn.CreateCommand();

        if (string.IsNullOrWhiteSpace(query))
        {
            cmd.CommandText = @"
                SELECT Id, Barcode, Name, BuyPrice, SellPrice, StockQuantity, Unit, KdvRate, IsFastProduct, CreatedAt, UpdatedAt
                FROM Products 
                ORDER BY Name ASC
                LIMIT @lim;
            ";
        }
        else
        {
            cmd.CommandText = @"
                SELECT Id, Barcode, Name, BuyPrice, SellPrice, StockQuantity, Unit, KdvRate, IsFastProduct, CreatedAt, UpdatedAt
                FROM Products 
                WHERE Barcode LIKE @q OR Name LIKE @q
                ORDER BY Name ASC
                LIMIT @lim;
            ";
            cmd.Parameters.AddWithValue("@q", $"%{query.Trim()}%");
        }

        cmd.Parameters.AddWithValue("@lim", limit);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapProduct(reader));
        }
        return list;
    }

    public int Add(Product product)
    {
        using var conn = DatabaseContext.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Products (Barcode, Name, BuyPrice, SellPrice, StockQuantity, Unit, KdvRate, IsFastProduct, CreatedAt, UpdatedAt)
            VALUES (@b, @n, @bp, @sp, @st, @unit, @kdv, @fast, @created, @updated);
            SELECT last_insert_rowid();
        ";
        string nowStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        cmd.Parameters.AddWithValue("@b", product.Barcode.Trim());
        cmd.Parameters.AddWithValue("@n", product.Name.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("@bp", product.BuyPrice);
        cmd.Parameters.AddWithValue("@sp", product.SellPrice);
        cmd.Parameters.AddWithValue("@st", product.StockQuantity);
        cmd.Parameters.AddWithValue("@unit", product.Unit);
        cmd.Parameters.AddWithValue("@kdv", product.KdvRate);
        cmd.Parameters.AddWithValue("@fast", product.IsFastProduct ? 1 : 0);
        cmd.Parameters.AddWithValue("@created", nowStr);
        cmd.Parameters.AddWithValue("@updated", nowStr);

        product.Id = Convert.ToInt32(cmd.ExecuteScalar());

        OnProductsChanged?.Invoke();
        return product.Id;
    }

    public void Update(Product product)
    {
        using var conn = DatabaseContext.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Products 
            SET Barcode = @b, 
                Name = @n, 
                BuyPrice = @bp, 
                SellPrice = @sp, 
                StockQuantity = @st, 
                Unit = @unit, 
                KdvRate = @kdv, 
                IsFastProduct = @fast, 
                UpdatedAt = @updated
            WHERE Id = @id;
        ";
        cmd.Parameters.AddWithValue("@id", product.Id);
        cmd.Parameters.AddWithValue("@b", product.Barcode.Trim());
        cmd.Parameters.AddWithValue("@n", product.Name.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("@bp", product.BuyPrice);
        cmd.Parameters.AddWithValue("@sp", product.SellPrice);
        cmd.Parameters.AddWithValue("@st", product.StockQuantity);
        cmd.Parameters.AddWithValue("@unit", product.Unit);
        cmd.Parameters.AddWithValue("@kdv", product.KdvRate);
        cmd.Parameters.AddWithValue("@fast", product.IsFastProduct ? 1 : 0);
        cmd.Parameters.AddWithValue("@updated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        cmd.ExecuteNonQuery();

        OnProductsChanged?.Invoke();
    }

    public void Delete(int id)
    {
        using var conn = DatabaseContext.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Products WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();

        OnProductsChanged?.Invoke();
    }

    public void AddStock(int id, decimal amountToAdd)
    {
        using var conn = DatabaseContext.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Products 
            SET StockQuantity = StockQuantity + @add,
                UpdatedAt = @updated
            WHERE Id = @id;
        ";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@add", amountToAdd);
        cmd.Parameters.AddWithValue("@updated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.ExecuteNonQuery();

        OnProductsChanged?.Invoke();
    }

    public List<Product> GetLowStockProducts(decimal threshold = 5)
    {
        var list = new List<Product>();
        using var conn = DatabaseContext.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, Barcode, Name, BuyPrice, SellPrice, StockQuantity, Unit, KdvRate, IsFastProduct, CreatedAt, UpdatedAt
            FROM Products 
            WHERE StockQuantity <= @th
            ORDER BY StockQuantity ASC;
        ";
        cmd.Parameters.AddWithValue("@th", threshold);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapProduct(reader));
        }
        return list;
    }

    public List<Product> GetNegativeStockProducts()
    {
        var list = new List<Product>();
        using var conn = DatabaseContext.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, Barcode, Name, BuyPrice, SellPrice, StockQuantity, Unit, KdvRate, IsFastProduct, CreatedAt, UpdatedAt
            FROM Products 
            WHERE StockQuantity < 0
            ORDER BY StockQuantity ASC;
        ";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapProduct(reader));
        }
        return list;
    }

    public int ResetNegativeStocksToZero()
    {
        using var conn = DatabaseContext.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Products 
            SET StockQuantity = 0,
                UpdatedAt = @updated
            WHERE StockQuantity < 0;
        ";
        cmd.Parameters.AddWithValue("@updated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        int affected = cmd.ExecuteNonQuery();

        if (affected > 0)
        {
            OnProductsChanged?.Invoke();
        }

        return affected;
    }

    public List<QuickButton> GetQuickButtons()
    {
        var list = new List<QuickButton>();
        using var conn = DatabaseContext.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, Barcode, Name, SellPrice
            FROM Products
            WHERE IsFastProduct = 1
            ORDER BY Id ASC;
        ";

        string[] palette = new[] {
            "#27ae60", // Green
            "#2980b9", // Blue
            "#16a085", // Teal
            "#d35400", // Orange
            "#8e44ad", // Purple
            "#c0392b", // Red
            "#00897B", // Dark Cyan
            "#3949AB", // Indigo
            "#D81B60", // Pink
            "#F4511E", // Deep Orange
            "#7CB342", // Light Green
            "#00ACC1"  // Cyan
        };

        using var reader = cmd.ExecuteReader();
        int slot = 0;
        while (reader.Read())
        {
            int id = reader.GetInt32(0);
            string barcode = reader.GetString(1);
            string name = reader.GetString(2);
            decimal price = reader.GetDecimal(3);
            string color = palette[slot % palette.Length];

            list.Add(new QuickButton
            {
                SlotIndex = slot++,
                ProductId = id,
                Title = name.Length > 16 ? name.Substring(0, 16) : name,
                Price = price,
                Barcode = barcode,
                ColorHex = color
            });
        }
        return list;
    }

    private static Product MapProduct(SqliteDataReader reader)
    {
        return new Product
        {
            Id = reader.GetInt32(0),
            Barcode = reader.GetString(1),
            Name = reader.GetString(2),
            BuyPrice = reader.GetDecimal(3),
            SellPrice = reader.GetDecimal(4),
            StockQuantity = reader.GetDecimal(5),
            Unit = reader.IsDBNull(6) ? "Adet" : reader.GetString(6),
            KdvRate = reader.IsDBNull(7) ? 1 : reader.GetInt32(7),
            IsFastProduct = !reader.IsDBNull(8) && reader.GetInt32(8) == 1,
            CreatedAt = DateTime.TryParse(reader.GetString(9), out var dt) ? dt : DateTime.Now,
            UpdatedAt = DateTime.TryParse(reader.GetString(10), out var dtU) ? dtU : DateTime.Now
        };
    }
}
