using MarketPOS.Data.Repositories;
using MarketPOS.Models;
using MarketPOS.Services;
using MarketPOS.UI;

namespace MarketPOS.Forms;

public class ReportForm : Form
{
    private readonly SaleRepository _saleRepo = new();
    private readonly ReceiptService _receiptService = new();

    private DateTimePicker _dtpStart = null!;
    private DateTimePicker _dtpEnd = null!;
    private Label _lblTotalTurnover = null!;
    private Label _lblCashTotal = null!;
    private Label _lblCardTotal = null!;
    private Label _lblReceiptCount = null!;

    private DataGridView _gridSales = null!;
    private DataGridView _gridSaleItems = null!;
    private DataGridView _gridTopProducts = null!;

    public ReportForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Kasa Gün Sonu Raporu ve Satış Geçmişi";
        this.Size = new Size(1100, 750);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(950, 650);
        this.BackColor = Theme.AppBackground;

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Theme.HeaderBg
        };
        var lblTitle = new Label
        {
            Text = "GÜN SONU RAPORU & SATIŞ GEÇMİŞİ",
            Font = Theme.HeaderFont,
            ForeColor = Theme.HeaderText,
            Dock = DockStyle.Left,
            Padding = new Padding(20, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        // Date Filter Toolbar
        var pnlFilter = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Theme.SurfaceBg,
            Padding = new Padding(15, 10, 15, 10)
        };

        var lblDates = new Label { Text = "Tarih:", Location = new Point(15, 16), Size = new Size(50, 25), Font = Theme.BoldFont };
        _dtpStart = new DateTimePicker { Location = new Point(70, 14), Size = new Size(130, 28), Font = Theme.RegularFont, Format = DateTimePickerFormat.Short };
        var lblTo = new Label { Text = "-", Location = new Point(208, 16), Size = new Size(20, 25), Font = Theme.BoldFont };
        _dtpEnd = new DateTimePicker { Location = new Point(230, 14), Size = new Size(130, 28), Font = Theme.RegularFont, Format = DateTimePickerFormat.Short };

        var btnFilter = new Button { Text = "SORGULA", Location = new Point(375, 12), Size = new Size(110, 32) };
        Theme.ApplyModernButton(btnFilter, Theme.PrimaryBlue, Color.White, 10F, true);
        btnFilter.Click += (s, e) => LoadReportData();

        var btnToday = new Button { Text = "BUGÜNÜ GETİR", Location = new Point(495, 12), Size = new Size(130, 32) };
        Theme.ApplyModernButton(btnToday, Color.FromArgb(100, 116, 139), Color.White, 10F, true);
        btnToday.Click += (s, e) =>
        {
            _dtpStart.Value = DateTime.Today;
            _dtpEnd.Value = DateTime.Today;
            LoadReportData();
        };

        var btnCancelSale = new Button { Text = "Seçili Fişi İptal Et / İade Al", Location = new Point(870, 12), Size = new Size(200, 32) };
        Theme.ApplyModernButton(btnCancelSale, Theme.DangerRed, Color.White, 9F, true);
        btnCancelSale.Click += OnCancelSaleClick;

        pnlFilter.Controls.AddRange(new Control[] { lblDates, _dtpStart, lblTo, _dtpEnd, btnFilter, btnToday, btnCancelSale });

        // Summary Cards Bar (Cards for Turnover, Cash, Card, Receipt Count)
        var pnlCards = new Panel
        {
            Dock = DockStyle.Top,
            Height = 100,
            BackColor = Theme.AppBackground,
            Padding = new Padding(15, 10, 15, 10)
        };

        var cardTurnover = CreateSummaryCard("TOPLAM CİRO", out _lblTotalTurnover, Theme.SuccessGreenDark, new Point(15, 10), new Size(245, 80));
        var cardCash = CreateSummaryCard("NAKİT TAHSİLAT", out _lblCashTotal, Theme.PrimaryBlueDark, new Point(275, 10), new Size(220, 80));
        var cardCard = CreateSummaryCard("KREDİ KARTI", out _lblCardTotal, Theme.PurpleAccent, new Point(510, 10), new Size(220, 80));
        var cardCount = CreateSummaryCard("TOPLAM FİŞ / ADET", out _lblReceiptCount, Color.FromArgb(51, 65, 85), new Point(745, 10), new Size(240, 80));

        pnlCards.Controls.AddRange(new Control[] { cardTurnover, cardCash, cardCard, cardCount });

        // Main Tab Control (1: Fişler & Kalemleri, 2: En Çok Satan Ürünler)
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = Theme.TitleFont,
            Padding = new Point(15, 8)
        };

        var tabSales = new TabPage(" Satış Fişleri ve Detayları ");
        tabSales.BackColor = Theme.AppBackground;

        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 580,
            Padding = new Padding(10)
        };

        // Left Grid: Sales Receipts
        _gridSales = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true };
        Theme.ApplyModernGrid(_gridSales);
        ConfigureSalesGrid();
        _gridSales.SelectionChanged += OnSalesGridSelectionChanged;

        // Right Grid: Sale Items of selected receipt
        _gridSaleItems = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true };
        Theme.ApplyModernGrid(_gridSaleItems);
        ConfigureSaleItemsGrid();

        var pnlLeftContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
        pnlLeftContainer.Controls.Add(_gridSales);

        var pnlRightContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
        var pnlRightHeader = new Panel { Dock = DockStyle.Top, Height = 35 };
        var lblItemsTitle = new Label { Text = "Seçili Fişin Ürün Detayları:", Font = Theme.BoldFont, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        pnlRightHeader.Controls.Add(lblItemsTitle);
        pnlRightContainer.Controls.Add(_gridSaleItems);
        pnlRightContainer.Controls.Add(pnlRightHeader);

        splitContainer.Panel1.Controls.Add(pnlLeftContainer);
        splitContainer.Panel2.Controls.Add(pnlRightContainer);
        tabSales.Controls.Add(splitContainer);

        // Tab 2: Top Selling Products
        var tabTop = new TabPage(" En Çok Satan Ürünler ");
        tabTop.BackColor = Theme.AppBackground;
        var pnlTopContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        _gridTopProducts = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true };
        Theme.ApplyModernGrid(_gridTopProducts);
        ConfigureTopProductsGrid();
        pnlTopContainer.Controls.Add(_gridTopProducts);
        tabTop.Controls.Add(pnlTopContainer);

        tabControl.TabPages.Add(tabSales);
        tabControl.TabPages.Add(tabTop);

        this.Controls.Add(tabControl);
        this.Controls.Add(pnlCards);
        this.Controls.Add(pnlFilter);
        this.Controls.Add(pnlHeader);

        this.Shown += (s, e) => LoadReportData();
    }

    private Panel CreateSummaryCard(string title, out Label valueLabel, Color accentColor, Point loc, Size size)
    {
        var panel = new Panel
        {
            Location = loc,
            Size = size,
            BackColor = Theme.SurfaceBg,
            BorderStyle = BorderStyle.None
        };

        var pnlStrip = new Panel
        {
            Dock = DockStyle.Left,
            Width = 6,
            BackColor = accentColor
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(16, 10),
            Size = new Size(size.Width - 25, 20)
        };

        valueLabel = new Label
        {
            Text = "0.00 ₺",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = accentColor,
            Location = new Point(16, 32),
            Size = new Size(size.Width - 25, 35)
        };

        panel.Controls.Add(valueLabel);
        panel.Controls.Add(lblTitle);
        panel.Controls.Add(pnlStrip);
        return panel;
    }

    private void ConfigureSalesGrid()
    {
        _gridSales.AutoGenerateColumns = false;
        _gridSales.AllowUserToResizeColumns = false;
        _gridSales.Columns.Clear();

        var colNo = new DataGridViewTextBoxColumn { HeaderText = "Fiş No", DataPropertyName = "ReceiptNumber", Width = 150, ReadOnly = true, Resizable = DataGridViewTriState.False };
        var colAmount = new DataGridViewTextBoxColumn { HeaderText = "Tutar (TL)", DataPropertyName = "FinalAmount", Width = 120, ReadOnly = true, Resizable = DataGridViewTriState.False, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } };
        colAmount.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        colAmount.HeaderCell.Style.Padding = new Padding(0, 0, 10, 0);
        colAmount.DefaultCellStyle.Padding = new Padding(0, 0, 10, 0);

        var colType = new DataGridViewTextBoxColumn { HeaderText = "Ödeme Türü", DataPropertyName = "PaymentType", Width = 110, ReadOnly = true, Resizable = DataGridViewTriState.False, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } };
        colType.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

        var colDate = new DataGridViewTextBoxColumn { HeaderText = "Tarih & Saat", DataPropertyName = "SaleDate", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true, Resizable = DataGridViewTriState.False, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy HH:mm:ss" } };

        _gridSales.Columns.AddRange(new DataGridViewColumn[] { colNo, colAmount, colType, colDate });
    }

    private void ConfigureSaleItemsGrid()
    {
        _gridSaleItems.AutoGenerateColumns = false;
        _gridSaleItems.AllowUserToResizeColumns = false;
        _gridSaleItems.Columns.Clear();

        var colBar = new DataGridViewTextBoxColumn { HeaderText = "Barkod", DataPropertyName = "Barcode", Width = 120, ReadOnly = true, Resizable = DataGridViewTriState.False };
        var colName = new DataGridViewTextBoxColumn { HeaderText = "Ürün Adı", DataPropertyName = "ProductName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true, Resizable = DataGridViewTriState.False };
        
        var colQty = new DataGridViewTextBoxColumn { HeaderText = "Adet", DataPropertyName = "Quantity", Width = 70, ReadOnly = true, Resizable = DataGridViewTriState.False, DefaultCellStyle = new DataGridViewCellStyle { Format = "0.##", Alignment = DataGridViewContentAlignment.MiddleCenter } };
        colQty.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

        var colPrice = new DataGridViewTextBoxColumn { HeaderText = "Fiyat (TL)", DataPropertyName = "UnitPrice", Width = 95, ReadOnly = true, Resizable = DataGridViewTriState.False, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } };
        colPrice.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        colPrice.HeaderCell.Style.Padding = new Padding(0, 0, 8, 0);
        colPrice.DefaultCellStyle.Padding = new Padding(0, 0, 8, 0);

        var colTotal = new DataGridViewTextBoxColumn { HeaderText = "Tutar (TL)", DataPropertyName = "TotalPrice", Width = 100, ReadOnly = true, Resizable = DataGridViewTriState.False, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } };
        colTotal.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        colTotal.HeaderCell.Style.Padding = new Padding(0, 0, 8, 0);
        colTotal.DefaultCellStyle.Padding = new Padding(0, 0, 8, 0);

        _gridSaleItems.Columns.AddRange(new DataGridViewColumn[] { colBar, colName, colQty, colPrice, colTotal });
    }

    private void ConfigureTopProductsGrid()
    {
        _gridTopProducts.AutoGenerateColumns = false;
        _gridTopProducts.AllowUserToResizeColumns = false;
        _gridTopProducts.Columns.Clear();

        var colBar = new DataGridViewTextBoxColumn { HeaderText = "Barkod", DataPropertyName = "Barcode", Width = 160, ReadOnly = true, Resizable = DataGridViewTriState.False };
        var colName = new DataGridViewTextBoxColumn { HeaderText = "Ürün Adı", DataPropertyName = "ProductName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true, Resizable = DataGridViewTriState.False };
        
        var colQty = new DataGridViewTextBoxColumn { HeaderText = "Toplam Satılan Adet", DataPropertyName = "TotalQuantity", Width = 180, ReadOnly = true, Resizable = DataGridViewTriState.False, DefaultCellStyle = new DataGridViewCellStyle { Format = "0.##", Alignment = DataGridViewContentAlignment.MiddleCenter } };
        colQty.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

        var colTotal = new DataGridViewTextBoxColumn { HeaderText = "Toplam Ciro (TL)", DataPropertyName = "TotalAmount", Width = 160, ReadOnly = true, Resizable = DataGridViewTriState.False, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } };
        colTotal.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        colTotal.HeaderCell.Style.Padding = new Padding(0, 0, 10, 0);
        colTotal.DefaultCellStyle.Padding = new Padding(0, 0, 10, 0);

        _gridTopProducts.Columns.AddRange(new DataGridViewColumn[] { colBar, colName, colQty, colTotal });
    }

    private void LoadReportData()
    {
        DateTime start = _dtpStart.Value.Date;
        DateTime end = _dtpEnd.Value.Date;

        // Daily Summary for selected range
        var summary = _saleRepo.GetDailySummary(start == end ? start : start);
        var sales = _saleRepo.GetSalesByDateRange(start, end);

        // Recalculate totals for multi-day if needed
        decimal total = sales.Sum(s => s.FinalAmount);
        decimal cash = sales.Where(s => s.PaymentType == "Nakit").Sum(s => s.FinalAmount);
        decimal card = sales.Where(s => s.PaymentType == "KrediKarti").Sum(s => s.FinalAmount);

        _lblTotalTurnover.Text = $"{total:N2} ₺";
        _lblCashTotal.Text = $"{cash:N2} ₺";
        _lblCardTotal.Text = $"{card:N2} ₺";
        _lblReceiptCount.Text = $"{sales.Count} Fiş";

        _gridSales.DataSource = sales;

        // Top products
        var top = _saleRepo.GetTopSellingProducts(start, end, 20);
        _gridTopProducts.DataSource = top;

        if (sales.Count == 0)
        {
            _gridSaleItems.DataSource = null;
        }
    }

    private void OnSalesGridSelectionChanged(object? sender, EventArgs e)
    {
        if (_gridSales.SelectedRows.Count > 0 && _gridSales.SelectedRows[0].DataBoundItem is Sale sale)
        {
            var items = _saleRepo.GetSaleItems(sale.Id);
            _gridSaleItems.DataSource = items;
        }
        else
        {
            _gridSaleItems.DataSource = null;
        }
    }

    private void OnCancelSaleClick(object? sender, EventArgs e)
    {
        if (_gridSales.SelectedRows.Count == 0 || _gridSales.SelectedRows[0].DataBoundItem is not Sale sale)
        {
            MessageBox.Show("Lütfen iptal etmek istediğiniz fişi seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var res = MessageBox.Show(
            $"{sale.ReceiptNumber} nolu fişi iptal etmek istediğinize emin misiniz?\n\nBu işlem satılan ürünleri tekrar stoğa iade edecektir!",
            "Fiş İptali & İade",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (res == DialogResult.Yes)
        {
            try
            {
                _saleRepo.CancelSale(sale.Id);
                SoundService.PlaySuccessBeep();
                MessageBox.Show("Fiş başarıyla iptal edildi ve ürünler stoğa iade edildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadReportData();
            }
            catch (Exception ex)
            {
                SoundService.PlayErrorBeep();
                MessageBox.Show($"İptal işlemi başarısız: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
