using MarketPOS.Data;
using MarketPOS.Data.Repositories;
using MarketPOS.Models;
using MarketPOS.Services;

namespace MarketPOS.Tests;

public class PosBusinessTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly ProductRepository _productRepo;
    private readonly SaleRepository _saleRepo;
    private readonly SettingsRepository _settingsRepo;
    private readonly ReceiptService _receiptService;

    public PosBusinessTests()
    {
        // Use an isolated temporary database for tests
        _testDbPath = Path.Combine(Path.GetTempPath(), $"market_test_{Guid.NewGuid():N}.db");
        DatabaseContext.DatabasePath = _testDbPath;
        DatabaseContext.InitializeDatabase();

        _productRepo = new ProductRepository();
        _saleRepo = new SaleRepository();
        _settingsRepo = new SettingsRepository();
        _receiptService = new ReceiptService();
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
        }
        catch { }
    }

    [Fact]
    public void Database_Initializes_And_Seeds_Default_Quick_Products()
    {
        var quickButtons = _productRepo.GetQuickButtons();
        Assert.NotEmpty(quickButtons);
        Assert.Contains(quickButtons, q => q.Barcode == "EKMEK01");
        Assert.Contains(quickButtons, q => q.Barcode == "POSET01");

        var ekmek = _productRepo.GetByBarcode("EKMEK01");
        Assert.NotNull(ekmek);
        Assert.Equal(10.00m, ekmek.SellPrice);
        Assert.True(ekmek.StockQuantity > 0);
    }

    [Fact]
    public void QuickButtons_Removes_Product_Immediately_When_IsFastProduct_Toggled_Off()
    {
        var su = _productRepo.GetByBarcode("SU500");
        Assert.NotNull(su);
        Assert.True(su.IsFastProduct);

        // Verify it is in quick buttons
        var buttonsBefore = _productRepo.GetQuickButtons();
        Assert.Contains(buttonsBefore, b => b.Barcode == "SU500");

        // Toggle IsFastProduct to false
        bool eventFired = false;
        ProductRepository.OnProductsChanged += () => eventFired = true;

        su.IsFastProduct = false;
        _productRepo.Update(su);

        // Verify event fired and SU500 is no longer in quick buttons
        Assert.True(eventFired);
        var buttonsAfter = _productRepo.GetQuickButtons();
        Assert.DoesNotContain(buttonsAfter, b => b.Barcode == "SU500");
    }

    [Fact]
    public void Telegram_Settings_Store_And_Retrieve_Correctly()
    {
        _settingsRepo.Set("TelegramEnabled", "1");
        _settingsRepo.Set("TelegramBotToken", "TEST_TOKEN_123");
        _settingsRepo.Set("TelegramChatId", "987654321");
        _settingsRepo.Set("TelegramPeriodMinutes", "120");

        Assert.Equal("1", _settingsRepo.Get("TelegramEnabled"));
        Assert.Equal("TEST_TOKEN_123", _settingsRepo.Get("TelegramBotToken"));
        Assert.Equal("987654321", _settingsRepo.Get("TelegramChatId"));
        Assert.Equal("120", _settingsRepo.Get("TelegramPeriodMinutes"));
    }

    [Fact]
    public void Product_Add_And_Retrieve_By_Barcode_Works()
    {
        string barcode = "8690123456789";
        var product = new Product
        {
            Barcode = barcode,
            Name = "TEST ÇİKOLATA 100GR",
            BuyPrice = 15.00m,
            SellPrice = 25.50m,
            StockQuantity = 40m,
            Unit = "Adet",
            KdvRate = 10,
            IsFastProduct = false
        };

        int id = _productRepo.Add(product);
        Assert.True(id > 0);

        var retrieved = _productRepo.GetByBarcode(barcode);
        Assert.NotNull(retrieved);
        Assert.Equal("TEST ÇİKOLATA 100GR", retrieved.Name);
        Assert.Equal(25.50m, retrieved.SellPrice);
        Assert.Equal(40m, retrieved.StockQuantity);
    }

    [Fact]
    public void Product_Stock_Increment_Updates_Inventory()
    {
        string barcode = "8690999888777";
        var product = new Product
        {
            Barcode = barcode,
            Name = "TEST ŞEKER 1KG",
            BuyPrice = 30m,
            SellPrice = 45m,
            StockQuantity = 10m
        };
        int id = _productRepo.Add(product);

        _productRepo.AddStock(id, 25m);

        var updated = _productRepo.GetById(id);
        Assert.NotNull(updated);
        Assert.Equal(35m, updated.StockQuantity);
    }

    [Fact]
    public void Sale_Completion_Deducts_Stock_Atomically()
    {
        string barcode = "8691112223334";
        var product = new Product
        {
            Barcode = barcode,
            Name = "TEST SÜT 1L",
            BuyPrice = 20m,
            SellPrice = 32m,
            StockQuantity = 50m
        };
        int productId = _productRepo.Add(product);

        var sale = new Sale
        {
            TotalAmount = 96m,
            FinalAmount = 96m,
            PaymentType = "Nakit",
            CashReceived = 100m,
            ChangeAmount = 4m,
            SaleDate = DateTime.Now
        };
        sale.Items.Add(new SaleItem
        {
            ProductId = productId,
            Barcode = barcode,
            ProductName = product.Name,
            UnitPrice = 32m,
            Quantity = 3m,
            TotalPrice = 96m
        });

        int saleId = _saleRepo.CompleteSale(sale);
        Assert.True(saleId > 0);
        Assert.False(string.IsNullOrEmpty(sale.ReceiptNumber));

        // Verify stock reduced from 50 to 47
        var afterSale = _productRepo.GetById(productId);
        Assert.NotNull(afterSale);
        Assert.Equal(47m, afterSale.StockQuantity);

        // Verify sale items stored
        var items = _saleRepo.GetSaleItems(saleId);
        Assert.Single(items);
        Assert.Equal(3m, items[0].Quantity);
        Assert.Equal(96m, items[0].TotalPrice);
    }

    [Fact]
    public void Sale_Cancellation_Refunds_Stock_To_Inventory()
    {
        string barcode = "8694445556667";
        var product = new Product
        {
            Barcode = barcode,
            Name = "TEST DETERJAN",
            BuyPrice = 50m,
            SellPrice = 80m,
            StockQuantity = 20m
        };
        int productId = _productRepo.Add(product);

        var sale = new Sale
        {
            TotalAmount = 160m,
            FinalAmount = 160m,
            PaymentType = "KrediKarti",
            SaleDate = DateTime.Now
        };
        sale.Items.Add(new SaleItem
        {
            ProductId = productId,
            Barcode = barcode,
            ProductName = product.Name,
            UnitPrice = 80m,
            Quantity = 2m,
            TotalPrice = 160m
        });

        int saleId = _saleRepo.CompleteSale(sale);

        // Stock should be 18
        Assert.Equal(18m, _productRepo.GetById(productId)!.StockQuantity);

        // Cancel sale
        _saleRepo.CancelSale(saleId);

        // Stock refunded back to 20
        Assert.Equal(20m, _productRepo.GetById(productId)!.StockQuantity);
    }

    [Fact]
    public void Daily_Summary_Aggregates_Cash_And_Card_Correctly()
    {
        var now = DateTime.Now;

        // Sale 1: Cash 150 TL
        _saleRepo.CompleteSale(new Sale
        {
            TotalAmount = 150m,
            FinalAmount = 150m,
            PaymentType = "Nakit",
            CashReceived = 200m,
            ChangeAmount = 50m,
            SaleDate = now
        });

        // Sale 2: Credit Card 250 TL
        _saleRepo.CompleteSale(new Sale
        {
            TotalAmount = 250m,
            FinalAmount = 250m,
            PaymentType = "KrediKarti",
            CashReceived = 0m,
            ChangeAmount = 0m,
            SaleDate = now
        });

        var summary = _saleRepo.GetDailySummary(now);
        Assert.True(summary.TotalTurnover >= 400m);
        Assert.True(summary.CashTotal >= 150m);
        Assert.True(summary.CardTotal >= 250m);
        Assert.True(summary.ReceiptCount >= 2);
    }

    [Fact]
    public void Receipt_Service_Builds_Formatted_Receipt_Text()
    {
        var sale = new Sale
        {
            ReceiptNumber = "F-20260922-0001",
            TotalAmount = 143.50m,
            FinalAmount = 143.50m,
            PaymentType = "Nakit",
            CashReceived = 200.00m,
            ChangeAmount = 56.50m,
            SaleDate = new DateTime(2026, 9, 22, 14, 30, 0)
        };
        sale.Items.Add(new SaleItem
        {
            ProductName = "ÇAYKUR RİZE TURİST",
            Quantity = 1,
            UnitPrice = 115.00m,
            TotalPrice = 115.00m
        });
        sale.Items.Add(new SaleItem
        {
            ProductName = "SÜT 1L",
            Quantity = 1,
            UnitPrice = 28.50m,
            TotalPrice = 28.50m
        });

        string receipt = _receiptService.BuildReceiptText(sale);

        Assert.Contains("F-20260922-0001", receipt);
        Assert.Contains("ÇAYKUR RİZE TURİST", receipt);
        Assert.Contains("143,50 TL", receipt.Replace(".", ","));
        Assert.Contains("56,50 TL", receipt.Replace(".", ","));
        Assert.Contains("PARA ÜSTÜ:", receipt);
    }

    [Fact]
    public void CartItem_Calculates_Total_Price_Correctly()
    {
        var item = new CartItem
        {
            ProductName = "TEST",
            UnitPrice = 17.50m,
            Quantity = 4m
        };

        Assert.Equal(70.00m, item.TotalPrice);
    }

    [Fact]
    public void CalculateChange_Setting_CanBeToggled_AndPersisted()
    {
        // Default should be "1" (true)
        string defaultSetting = _settingsRepo.Get("CalculateChange", "1");
        Assert.Equal("1", defaultSetting);

        // Turn off
        _settingsRepo.Set("CalculateChange", "0");
        Assert.Equal("0", _settingsRepo.Get("CalculateChange", "1"));

        // Turn back on
        _settingsRepo.Set("CalculateChange", "1");
        Assert.Equal("1", _settingsRepo.Get("CalculateChange", "1"));
    }
}
