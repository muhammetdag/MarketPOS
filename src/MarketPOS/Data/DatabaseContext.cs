using System.Data;
using Microsoft.Data.Sqlite;

namespace MarketPOS.Data;

public class DatabaseContext
{
    private static string? _customDbPath;

    public static string DatabasePath
    {
        get
        {
            if (!string.IsNullOrEmpty(_customDbPath))
                return _customDbPath;

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appDir, "market_pos.db");
        }
        set => _customDbPath = value;
    }

    public static string ConnectionString => $"Data Source={DatabasePath};";

    public static SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    public static void InitializeDatabase()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;

            CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Barcode TEXT UNIQUE NOT NULL,
                Name TEXT NOT NULL,
                BuyPrice REAL DEFAULT 0.0,
                SellPrice REAL NOT NULL,
                StockQuantity REAL DEFAULT 0.0,
                Unit TEXT DEFAULT 'Adet',
                KdvRate INTEGER DEFAULT 1,
                IsFastProduct INTEGER DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_products_barcode ON Products(Barcode);
            CREATE INDEX IF NOT EXISTS idx_products_name ON Products(Name);

            CREATE TABLE IF NOT EXISTS Sales (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ReceiptNumber TEXT UNIQUE NOT NULL,
                TotalAmount REAL NOT NULL,
                DiscountAmount REAL DEFAULT 0.0,
                FinalAmount REAL NOT NULL,
                PaymentType TEXT NOT NULL,
                CashReceived REAL DEFAULT 0.0,
                ChangeAmount REAL DEFAULT 0.0,
                CustomerName TEXT,
                SaleDate TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_sales_date ON Sales(SaleDate);

            CREATE TABLE IF NOT EXISTS SaleItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SaleId INTEGER NOT NULL,
                ProductId INTEGER,
                Barcode TEXT,
                ProductName TEXT NOT NULL,
                UnitPrice REAL NOT NULL,
                Quantity REAL NOT NULL,
                TotalPrice REAL NOT NULL,
                FOREIGN KEY(SaleId) REFERENCES Sales(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS QuickButtons (
                SlotIndex INTEGER PRIMARY KEY,
                ProductId INTEGER,
                Title TEXT NOT NULL,
                Price REAL NOT NULL,
                Barcode TEXT NOT NULL,
                ColorHex TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT
            );
        ";
        cmd.ExecuteNonQuery();

        SeedDefaultData(conn);
    }

    private static void SeedDefaultData(SqliteConnection conn)
    {
        // Default Settings
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT OR IGNORE INTO Settings (Key, Value) VALUES 
                ('MarketName', 'ÖRNEK MARKET & ŞARKÜTERİ'),
                ('Phone', '0 (5XX) XXX XX XX'),
                ('Address', 'Cumhuriyet Cad. No: 1'),
                ('ReceiptHeader', 'HOŞGELDİNİZ'),
                ('ReceiptFooter', 'BİZİ TERCİH ETTİĞİNİZ İÇİN TEŞEKKÜR EDERİZ'),
                ('ReceiptAutoPrint', '0');
            ";
            cmd.ExecuteNonQuery();
        }

        // Check if QuickButtons has data
        using (var checkCmd = conn.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(*) FROM QuickButtons;";
            long count = (long)(checkCmd.ExecuteScalar() ?? 0L);

            if (count == 0)
            {
                // Create common fast products if not exist
                InsertDefaultFastProduct(conn, "EKMEK01", "EKMEK 200 GR", 10.00m, 100m, 0, "EKMEK", "#27ae60");
                InsertDefaultFastProduct(conn, "POSET01", "ALIŞVERİŞ POŞETİ", 0.25m, 1000m, 1, "POŞET", "#2980b9");
                InsertDefaultFastProduct(conn, "SU500", "SU 0.5 LT", 5.00m, 200m, 2, "SU 0.5L", "#16a085");
                InsertDefaultFastProduct(conn, "SIMIT01", "TAZE SİMİT", 12.50m, 50m, 3, "SİMİT", "#d35400");
                InsertDefaultFastProduct(conn, "SODA01", "SADE MADEN SUYU", 7.50m, 120m, 4, "SODA", "#8e44ad");
                InsertDefaultFastProduct(conn, "CAY01", "BARDAK ÇAY", 7.50m, 500m, 5, "ÇAY", "#c0392b");
            }
        }
    }

    private static void InsertDefaultFastProduct(SqliteConnection conn, string barcode, string name, decimal price, decimal stock, int slotIndex, string buttonTitle, string colorHex)
    {
        long productId;
        using (var pCmd = conn.CreateCommand())
        {
            pCmd.CommandText = @"
                INSERT OR IGNORE INTO Products (Barcode, Name, BuyPrice, SellPrice, StockQuantity, Unit, KdvRate, IsFastProduct, CreatedAt, UpdatedAt)
                VALUES (@b, @n, @bp, @sp, @st, 'Adet', 1, 1, datetime('now'), datetime('now'));
                SELECT Id FROM Products WHERE Barcode = @b;
            ";
            pCmd.Parameters.AddWithValue("@b", barcode);
            pCmd.Parameters.AddWithValue("@n", name);
            pCmd.Parameters.AddWithValue("@bp", price * 0.7m);
            pCmd.Parameters.AddWithValue("@sp", price);
            pCmd.Parameters.AddWithValue("@st", stock);

            object? res = pCmd.ExecuteScalar();
            productId = res != null ? Convert.ToInt64(res) : 0;
        }

        using (var btnCmd = conn.CreateCommand())
        {
            btnCmd.CommandText = @"
                INSERT OR REPLACE INTO QuickButtons (SlotIndex, ProductId, Title, Price, Barcode, ColorHex)
                VALUES (@slot, @pid, @title, @price, @bcode, @color);
            ";
            btnCmd.Parameters.AddWithValue("@slot", slotIndex);
            btnCmd.Parameters.AddWithValue("@pid", productId);
            btnCmd.Parameters.AddWithValue("@title", buttonTitle);
            btnCmd.Parameters.AddWithValue("@price", price);
            btnCmd.Parameters.AddWithValue("@bcode", barcode);
            btnCmd.Parameters.AddWithValue("@color", colorHex);
            btnCmd.ExecuteNonQuery();
        }
    }
}
