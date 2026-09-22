using System.ComponentModel;
using MarketPOS.Data;
using MarketPOS.Data.Repositories;
using MarketPOS.Models;
using MarketPOS.Services;
using MarketPOS.UI;

namespace MarketPOS.Forms;

public class MainForm : Form
{
    private readonly ProductRepository _productRepo = new();
    private readonly SaleRepository _saleRepo = new();
    private readonly SettingsRepository _settingsRepo = new();
    private readonly ReceiptService _receiptService = new();

    private readonly BindingList<CartItem> _cartItems = new();

    // UI Controls
    private Label _lblMarketHeader = null!;
    private Label _lblClock = null!;
    private System.Windows.Forms.Timer _clockTimer = null!;

    private DataGridView _gridCart = null!;
    private TextBox _txtBarcode = null!;
    private Label _lblTotalDisplay = null!;
    private Label _lblCartSummary = null!;

    private FlowLayoutPanel _pnlFastButtons = null!;
    private Button _btnPayCash = null!;
    private Button _btnPayCard = null!;
    private Button _btnClearCart = null!;
    private Button _btnQtyPlus = null!;
    private Button _btnQtyMinus = null!;
    private Button _btnRemoveItem = null!;

    private bool _isFullScreen = false;
    private FormBorderStyle _previousBorderStyle = FormBorderStyle.Sizable;
    private FormWindowState _previousWindowState = FormWindowState.Normal;
    private Rectangle _previousBounds;
    private Button _btnToggleFullScreen = null!;

    public MainForm()
    {
        InitializeComponent();
        DatabaseContext.InitializeDatabase();
        LoadHeaderAndSettings();
        TelegramBackupService.StartBackgroundWorker();

        ProductRepository.OnProductsChanged += () =>
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                this.BeginInvoke((MethodInvoker)LoadQuickButtons);
            }
        };
    }

    private void InitializeComponent()
    {
        this.Text = "Market POS - Hızlı Satış ve Kasa Sistemi";
        this.Size = new Size(1200, 750);
        this.MinimumSize = new Size(1000, 680);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.WindowState = FormWindowState.Normal;
        this.MaximizeBox = true;
        this.MinimizeBox = true;
        this.BackColor = Theme.AppBackground;
        this.KeyPreview = true;

        // 1. Top Navigation Bar
        var pnlTopNav = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Theme.HeaderBg,
            Padding = new Padding(15, 0, 15, 0)
        };

        _lblMarketHeader = new Label
        {
            Text = "MARKET HIZLI SATIŞ KASASI",
            Font = Theme.HeaderFont,
            ForeColor = Theme.HeaderText,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            UseMnemonic = false
        };

        var pnlRightHeader = new Panel
        {
            Dock = DockStyle.Right,
            Width = 760,
            Height = 65,
            BackColor = Color.Transparent
        };

        var btnNavStock = new Button { Text = "Ürün & Stok (F2)", Location = new Point(5, 12), Size = new Size(130, 40) };
        Theme.ApplyModernButton(btnNavStock, Theme.PrimaryBlue, Color.White, 9.5F, true);
        btnNavStock.Click += (s, e) => OpenProductForm();

        var btnNavReports = new Button { Text = "Gün Sonu (F3)", Location = new Point(140, 12), Size = new Size(125, 40) };
        Theme.ApplyModernButton(btnNavReports, Theme.PurpleAccent, Color.White, 9.5F, true);
        btnNavReports.Click += (s, e) => OpenReportForm();

        var btnNavSettings = new Button { Text = "Ayarlar (F4)", Location = new Point(270, 12), Size = new Size(120, 40) };
        Theme.ApplyModernButton(btnNavSettings, Color.FromArgb(71, 85, 105), Color.White, 9.5F, true);
        btnNavSettings.Click += (s, e) => OpenSettingsForm();

        _btnToggleFullScreen = new Button { Text = "Tam Ekran (F9)", Location = new Point(395, 12), Size = new Size(140, 40) };
        Theme.ApplyModernButton(_btnToggleFullScreen, Color.FromArgb(13, 148, 136), Color.White, 9.5F, true);
        _btnToggleFullScreen.Click += (s, e) => ToggleFullScreen();

        _lblClock = new Label
        {
            Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
            Font = Theme.BoldFont,
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(545, 12),
            Size = new Size(205, 40),
            TextAlign = ContentAlignment.MiddleRight
        };

        pnlRightHeader.Controls.AddRange(new Control[] { btnNavStock, btnNavReports, btnNavSettings, _btnToggleFullScreen, _lblClock });

        pnlTopNav.Controls.Add(_lblMarketHeader);
        pnlTopNav.Controls.Add(pnlRightHeader);

        // 2. Main Body Split Container
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        // Left Container: Cart Grid + Barcode Input + Cart Item Controls
        var pnlLeft = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 12, 0)
        };

        // Top of Left: Barcode Scan Field
        var pnlBarcodeBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = Theme.SurfaceBg,
            Padding = new Padding(12)
        };

        var lblBarcodeIcon = new Label
        {
            Text = "BARKOD:",
            Font = Theme.TitleFont,
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(12, 22),
            Size = new Size(95, 30)
        };

        _txtBarcode = new TextBox
        {
            Location = new Point(115, 16),
            Size = new Size(480, 40),
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            PlaceholderText = "Barkodu okutun veya yazıp Enter'a basın... (Örn: 5*barkod)"
        };
        _txtBarcode.KeyDown += OnBarcodeKeyDown;

        var btnAddBarcode = new Button
        {
            Text = "EKLE (ENTER)",
            Location = new Point(605, 15),
            Size = new Size(130, 42)
        };
        Theme.ApplyModernButton(btnAddBarcode, Theme.SuccessGreen, Color.White, 10F, true);
        btnAddBarcode.Click += (s, e) => ProcessBarcodeEntry();

        pnlBarcodeBar.Controls.AddRange(new Control[] { lblBarcodeIcon, _txtBarcode, btnAddBarcode });

        // Middle of Left: DataGridView
        _gridCart = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = _cartItems
        };
        Theme.ApplyModernGrid(_gridCart);
        ConfigureCartGrid();
        _gridCart.CellDoubleClick += OnGridCartCellDoubleClick;

        // Bottom of Left: Cart Row Modification Buttons (+1, -1, Delete, Clear)
        var pnlCartActions = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 55,
            BackColor = Theme.SurfaceBg,
            Padding = new Padding(10, 8, 10, 8)
        };

        _btnQtyPlus = new Button { Text = "+1 Adet", Size = new Size(100, 38), Location = new Point(10, 8) };
        Theme.ApplyModernButton(_btnQtyPlus, Theme.PrimaryBlue, Color.White, 10F, true);
        _btnQtyPlus.Click += (s, e) => ModifySelectedQuantity(1);

        _btnQtyMinus = new Button { Text = "-1 Adet", Size = new Size(100, 38), Location = new Point(118, 8) };
        Theme.ApplyModernButton(_btnQtyMinus, Theme.PrimaryBlue, Color.White, 10F, true);
        _btnQtyMinus.Click += (s, e) => ModifySelectedQuantity(-1);

        _btnRemoveItem = new Button { Text = "Seçiliyi Sil (DEL)", Size = new Size(140, 38), Location = new Point(226, 8) };
        Theme.ApplyModernButton(_btnRemoveItem, Theme.DangerRed, Color.White, 10F, true);
        _btnRemoveItem.Click += (s, e) => RemoveSelectedItem();

        _btnClearCart = new Button { Text = "Sepeti Sıfırla (ESC)", Size = new Size(150, 38), Location = new Point(374, 8) };
        Theme.ApplyModernButton(_btnClearCart, Color.FromArgb(100, 116, 139), Color.White, 10F, true);
        _btnClearCart.Click += (s, e) => ClearCartWithConfirm();

        pnlCartActions.Controls.AddRange(new Control[] { _btnQtyPlus, _btnQtyMinus, _btnRemoveItem, _btnClearCart });

        pnlLeft.Controls.Add(_gridCart);
        pnlLeft.Controls.Add(pnlCartActions);
        pnlLeft.Controls.Add(pnlBarcodeBar);

        // Right Container: Total Display + Fast Buttons + Payment Buttons
        var pnlRight = new Panel
        {
            Dock = DockStyle.Right,
            Width = 420,
            Padding = new Padding(0)
        };

        // Top of Right: Grand Display (Big fluorescent total)
        var pnlDisplayCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 150,
            BackColor = Theme.DisplayScreenBg,
            Padding = new Padding(15)
        };

        var lblDisplayTitle = new Label
        {
            Text = "ÖDENECEK TOPLAM TUTAR",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            Dock = DockStyle.Top,
            Height = 25,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _lblTotalDisplay = new Label
        {
            Text = "0.00 ₺",
            Font = Theme.GiantDisplayFont,
            ForeColor = Theme.DisplayTotalText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _lblCartSummary = new Label
        {
            Text = "Sepet: 0 Kalem | Toplam: 0 Adet",
            Font = new Font("Segoe UI", 10F, FontStyle.Regular),
            ForeColor = Color.FromArgb(148, 163, 184),
            Dock = DockStyle.Bottom,
            Height = 22,
            TextAlign = ContentAlignment.MiddleCenter
        };

        pnlDisplayCard.Controls.Add(_lblTotalDisplay);
        pnlDisplayCard.Controls.Add(_lblCartSummary);
        pnlDisplayCard.Controls.Add(lblDisplayTitle);

        // Middle of Right: Quick Products (Touch/Shortcut Buttons)
        var grpFastButtons = new GroupBox
        {
            Text = "  HIZLI ÜRÜNLER (BÜYÜK DOKUNMATİK)  ",
            Font = Theme.TitleFont,
            ForeColor = Color.FromArgb(30, 41, 59),
            Dock = DockStyle.Fill,
            BackColor = Theme.SurfaceBg,
            Padding = new Padding(10)
        };

        _pnlFastButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        grpFastButtons.Controls.Add(_pnlFastButtons);

        // Bottom of Right: Huge Payment Buttons (F12 NAKİT, F11 KREDİ KARTI)
        var pnlPayment = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 160,
            BackColor = Theme.SurfaceBg,
            Padding = new Padding(12)
        };

        _btnPayCash = new Button
        {
            Text = "NAKİT ÖDEME (F12)\n[Para Üstü Hesapla]",
            Location = new Point(12, 10),
            Size = new Size(396, 68)
        };
        Theme.ApplyModernButton(_btnPayCash, Theme.SuccessGreen, Color.White, 15F, true);
        _btnPayCash.Click += (s, e) => CompleteCashSale();

        _btnPayCard = new Button
        {
            Text = "KREDİ KARTI (F11)",
            Location = new Point(12, 84),
            Size = new Size(396, 62)
        };
        Theme.ApplyModernButton(_btnPayCard, Theme.PrimaryBlue, Color.White, 14F, true);
        _btnPayCard.Click += (s, e) => CompleteCardSale();

        pnlPayment.Controls.AddRange(new Control[] { _btnPayCash, _btnPayCard });

        pnlRight.Controls.Add(grpFastButtons);
        pnlRight.Controls.Add(pnlPayment);
        pnlRight.Controls.Add(pnlDisplayCard);

        pnlBody.Controls.Add(pnlLeft);
        pnlBody.Controls.Add(pnlRight);

        this.Controls.Add(pnlBody);
        this.Controls.Add(pnlTopNav);

        // Clock Timer
        _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _clockTimer.Tick += (s, e) =>
        {
            _lblClock.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        };
        _clockTimer.Start();

        // Keyboard Shortcuts
        this.KeyDown += OnMainFormKeyDown;

        this.Shown += (s, e) =>
        {
            LoadHeaderAndSettings();
            LoadQuickButtons();
            _txtBarcode.Focus();
        };
    }

    private void ConfigureCartGrid()
    {
        _gridCart.AutoGenerateColumns = false;
        _gridCart.AllowUserToResizeColumns = false;
        _gridCart.Columns.Clear();

        var colNo = new DataGridViewTextBoxColumn
        {
            HeaderText = "No",
            DataPropertyName = "LineNumber",
            Width = 50,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False
        };
        colNo.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colNo.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridCart.Columns.Add(colNo);

        var colBar = new DataGridViewTextBoxColumn
        {
            HeaderText = "Barkod",
            DataPropertyName = "Barcode",
            Width = 140,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False
        };
        colBar.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
        colBar.HeaderCell.Style.Padding = new Padding(8, 0, 0, 0);
        colBar.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        colBar.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
        _gridCart.Columns.Add(colBar);

        var colName = new DataGridViewTextBoxColumn
        {
            HeaderText = "Ürün Adı",
            DataPropertyName = "ProductName",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False
        };
        colName.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
        colName.HeaderCell.Style.Padding = new Padding(8, 0, 0, 0);
        colName.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        colName.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
        _gridCart.Columns.Add(colName);

        var colQty = new DataGridViewTextBoxColumn
        {
            HeaderText = "Adet/Miktar",
            DataPropertyName = "Quantity",
            Width = 110,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "0.##", Alignment = DataGridViewContentAlignment.MiddleCenter, Font = Theme.BoldFont },
            ToolTipText = "Miktarı düzenlemek için çift tıklayın"
        };
        colQty.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridCart.Columns.Add(colQty);

        var colUnit = new DataGridViewTextBoxColumn
        {
            HeaderText = "Birim",
            DataPropertyName = "Unit",
            Width = 70,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
        };
        colUnit.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridCart.Columns.Add(colUnit);

        var colPrice = new DataGridViewTextBoxColumn
        {
            HeaderText = "Birim Fiyat (TL)",
            DataPropertyName = "UnitPrice",
            Width = 120,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight }
        };
        colPrice.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        colPrice.HeaderCell.Style.Padding = new Padding(0, 0, 10, 0);
        colPrice.DefaultCellStyle.Padding = new Padding(0, 0, 10, 0);
        _gridCart.Columns.Add(colPrice);

        var colTotal = new DataGridViewTextBoxColumn
        {
            HeaderText = "Tutar (TL)",
            DataPropertyName = "TotalPrice",
            Width = 130,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight, Font = Theme.BoldFont }
        };
        colTotal.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        colTotal.HeaderCell.Style.Padding = new Padding(0, 0, 10, 0);
        colTotal.DefaultCellStyle.Padding = new Padding(0, 0, 10, 0);
        _gridCart.Columns.Add(colTotal);
    }

    private void LoadHeaderAndSettings()
    {
        string marketName = _settingsRepo.Get("MarketName", "MARKET HIZLI SATIŞ KASASI");
        _lblMarketHeader.Text = marketName;

        bool calculateChange = _settingsRepo.Get("CalculateChange", "1") != "0";
        _btnPayCash.Text = calculateChange
            ? "NAKİT ÖDEME (F12)\n[Para Üstü Hesapla]"
            : "NAKİT ÖDEME (F12)";
    }

    private void LoadQuickButtons()
    {
        _pnlFastButtons.Controls.Clear();
        var buttons = _productRepo.GetQuickButtons();

        foreach (var qb in buttons)
        {
            var btn = new Button
            {
                Text = $"{qb.Title}\n{qb.Price:N2} ₺",
                Size = new Size(120, 75),
                Margin = new Padding(6),
                Tag = qb
            };

            Color bg;
            try { bg = ColorTranslator.FromHtml(qb.ColorHex); } catch { bg = Theme.PrimaryBlueDark; }
            Theme.ApplyModernButton(btn, bg, Color.White, 11F, true);

            btn.Click += (s, e) =>
            {
                AddProductToCartByBarcode(qb.Barcode, 1);
                _txtBarcode.Focus();
            };

            _pnlFastButtons.Controls.Add(btn);
        }
    }

    private void OnBarcodeKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            ProcessBarcodeEntry();
        }
    }

    private void ProcessBarcodeEntry()
    {
        string input = _txtBarcode.Text.Trim();
        if (string.IsNullOrWhiteSpace(input)) return;

        decimal quantity = 1m;
        string barcode = input;

        // Check multiplier e.g. "5*869..." or "5x869..."
        if (input.Contains('*') || input.Contains('x') || input.Contains('X'))
        {
            char sep = input.Contains('*') ? '*' : (input.Contains('x') ? 'x' : 'X');
            string[] parts = input.Split(sep, 2);
            if (parts.Length == 2 && decimal.TryParse(parts[0].Trim().Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal q))
            {
                quantity = q;
                barcode = parts[1].Trim();
            }
        }

        AddProductToCartByBarcode(barcode, quantity);
        _txtBarcode.Clear();
        _txtBarcode.Focus();
    }

    private void AddProductToCartByBarcode(string barcode, decimal quantity)
    {
        var product = _productRepo.GetByBarcode(barcode);
        if (product == null)
        {
            SoundService.PlayErrorBeep();
            var res = MessageBox.Show(
                $"'{barcode}' barkodlu ürün sistemde bulunamadı!\n\nBu ürünü ve stok sayısını hemen sisteme kaydetmek ister misiniz?",
                "Kayıtsız Ürün",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                OpenProductForm(barcode);
            }
            return;
        }

        // Check if item is already in cart, if so, increase quantity
        var existingInCart = _cartItems.FirstOrDefault(c => c.Barcode == product.Barcode);
        if (existingInCart != null)
        {
            existingInCart.Quantity += quantity;
            _cartItems.ResetBindings();
        }
        else
        {
            var cartItem = new CartItem
            {
                LineNumber = _cartItems.Count + 1,
                ProductId = product.Id,
                Barcode = product.Barcode,
                ProductName = product.Name,
                UnitPrice = product.SellPrice,
                Quantity = quantity,
                Unit = product.Unit
            };
            _cartItems.Add(cartItem);
        }

        SoundService.PlaySuccessBeep();
        UpdateCartTotals();

        // Select the last row in grid
        if (_gridCart.Rows.Count > 0)
        {
            _gridCart.ClearSelection();
            _gridCart.Rows[_gridCart.Rows.Count - 1].Selected = true;
            _gridCart.FirstDisplayedScrollingRowIndex = _gridCart.Rows.Count - 1;
        }
    }

    private void ModifySelectedQuantity(decimal delta)
    {
        if (_gridCart.SelectedRows.Count == 0 || _gridCart.SelectedRows[0].DataBoundItem is not CartItem item)
            return;

        item.Quantity += delta;
        if (item.Quantity <= 0)
        {
            _cartItems.Remove(item);
            ReindexCartLines();
        }
        else
        {
            _cartItems.ResetBindings();
        }

        UpdateCartTotals();
        _txtBarcode.Focus();
    }

    private void OnGridCartCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _gridCart.Rows.Count) return;
        if (_gridCart.Rows[e.RowIndex].DataBoundItem is not CartItem item) return;

        OpenEditCartQuantityDialog(item);
    }

    private void OpenEditCartQuantityDialog(CartItem item)
    {
        using var dlg = new Form
        {
            Text = $"Miktar Düzenle - {item.ProductName}",
            Size = new Size(460, 320),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Theme.AppBackground,
            KeyPreview = true
        };

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Theme.HeaderBg,
            Padding = new Padding(15, 8, 15, 8)
        };

        var lblHeaderProd = new Label
        {
            Text = item.ProductName,
            Font = Theme.TitleFont,
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 26,
            UseMnemonic = false
        };

        var lblHeaderInfo = new Label
        {
            Text = $"Birim Fiyat: {item.UnitPrice:N2} ₺  |  Mevcut Sepet: {item.Quantity:0.##} {item.Unit}",
            Font = Theme.RegularFont,
            ForeColor = Color.FromArgb(148, 163, 184),
            Dock = DockStyle.Bottom,
            Height = 22,
            UseMnemonic = false
        };

        pnlHeader.Controls.Add(lblHeaderProd);
        pnlHeader.Controls.Add(lblHeaderInfo);

        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var lblPrompt = new Label
        {
            Text = $"Yeni Miktar / Adet ({item.Unit}):",
            Font = Theme.BoldFont,
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(20, 14),
            Size = new Size(250, 24)
        };

        var txtQty = new TextBox
        {
            Text = item.Quantity.ToString("0.##"),
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            Location = new Point(20, 42),
            Size = new Size(400, 42),
            TextAlign = HorizontalAlignment.Center
        };

        // Quick Preset Chips (1, 2, 3, 5, 10, 20)
        var pnlPresets = new FlowLayoutPanel
        {
            Location = new Point(20, 92),
            Size = new Size(400, 42),
            FlowDirection = FlowDirection.LeftToRight
        };

        int[] presets = { 1, 2, 3, 5, 10, 20 };
        foreach (int pr in presets)
        {
            var btnPr = new Button
            {
                Text = pr.ToString(),
                Size = new Size(58, 34),
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplyModernButton(btnPr, Color.FromArgb(226, 232, 240), Color.FromArgb(30, 41, 59), 11F, true);
            btnPr.Click += (s, e) =>
            {
                txtQty.Text = pr.ToString();
                txtQty.Focus();
                txtQty.SelectAll();
            };
            pnlPresets.Controls.Add(btnPr);
        }

        var btnConfirm = new Button
        {
            Text = "GÜNCELLE (ENTER)",
            Location = new Point(20, 148),
            Size = new Size(230, 46),
            DialogResult = DialogResult.OK
        };
        Theme.ApplyModernButton(btnConfirm, Theme.SuccessGreen, Color.White, 11F, true);

        var btnCancel = new Button
        {
            Text = "İPTAL (ESC)",
            Location = new Point(260, 148),
            Size = new Size(160, 46),
            DialogResult = DialogResult.Cancel
        };
        Theme.ApplyModernButton(btnCancel, Color.FromArgb(100, 116, 139), Color.White, 11F, false);

        dlg.AcceptButton = btnConfirm;
        dlg.CancelButton = btnCancel;

        pnlBody.Controls.AddRange(new Control[] { lblPrompt, txtQty, pnlPresets, btnConfirm, btnCancel });
        dlg.Controls.Add(pnlBody);
        dlg.Controls.Add(pnlHeader);

        dlg.Shown += (s, e) =>
        {
            txtQty.Focus();
            txtQty.SelectAll();
        };

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            string clean = txtQty.Text.Replace(",", ".").Trim();
            if (decimal.TryParse(clean, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal newQty))
            {
                if (newQty <= 0)
                {
                    _cartItems.Remove(item);
                    ReindexCartLines();
                }
                else
                {
                    item.Quantity = newQty;
                    _cartItems.ResetBindings();
                }
                SoundService.PlaySuccessBeep();
                UpdateCartTotals();
            }
            else
            {
                MessageBox.Show("Lütfen geçerli bir sayı girin.", "Geçersiz Miktar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        _txtBarcode.Focus();
    }

    private void RemoveSelectedItem()
    {
        if (_gridCart.SelectedRows.Count == 0 || _gridCart.SelectedRows[0].DataBoundItem is not CartItem item)
            return;

        _cartItems.Remove(item);
        ReindexCartLines();
        UpdateCartTotals();
        _txtBarcode.Focus();
    }

    private void ReindexCartLines()
    {
        for (int i = 0; i < _cartItems.Count; i++)
        {
            _cartItems[i].LineNumber = i + 1;
        }
        _cartItems.ResetBindings();
    }

    private void ClearCartWithConfirm()
    {
        if (_cartItems.Count == 0) return;

        var res = MessageBox.Show("Sepetteki tüm ürünleri temizlemek istediğinize emin misiniz?", "Sepeti Sıfırla", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (res == DialogResult.Yes)
        {
            _cartItems.Clear();
            UpdateCartTotals();
        }
        _txtBarcode.Focus();
    }

    private decimal GetCartTotal()
    {
        return _cartItems.Sum(c => c.TotalPrice);
    }

    private void UpdateCartTotals()
    {
        decimal total = GetCartTotal();
        decimal totalQty = _cartItems.Sum(c => c.Quantity);

        _lblTotalDisplay.Text = $"{total:N2} ₺";
        _lblCartSummary.Text = $"Sepet: {_cartItems.Count} Kalem  |  Toplam: {totalQty:0.##} Adet";

        bool hasItems = _cartItems.Count > 0;
        _btnPayCash.Enabled = hasItems;
        _btnPayCard.Enabled = hasItems;
    }

    private void CompleteCashSale()
    {
        if (_cartItems.Count == 0) return;

        decimal total = GetCartTotal();
        bool calculateChange = _settingsRepo.Get("CalculateChange", "1") != "0";

        if (calculateChange)
        {
            using var dlg = new PaymentDialog(total);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                FinalizeSale("Nakit", dlg.CashReceived, dlg.ChangeAmount);
            }
        }
        else
        {
            // Ask confirmation just like credit card sale
            var res = MessageBox.Show(
                $"{total:N2} ₺ NAKİT satışını onaylıyor musunuz?",
                "Nakit Satışı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                FinalizeSale("Nakit", total, 0m);
            }
        }
        _txtBarcode.Focus();
    }

    private void CompleteCardSale()
    {
        if (_cartItems.Count == 0) return;

        decimal total = GetCartTotal();
        var res = MessageBox.Show($"{total:N2} ₺ KREDİ KARTI satışını onaylıyor musunuz?", "Kredi Kartı Satışı", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (res == DialogResult.Yes)
        {
            FinalizeSale("KrediKarti", total, 0m);
        }
        _txtBarcode.Focus();
    }

    private void FinalizeSale(string paymentType, decimal cashReceived, decimal changeAmount)
    {
        try
        {
            var sale = new Sale
            {
                TotalAmount = GetCartTotal(),
                DiscountAmount = 0m,
                FinalAmount = GetCartTotal(),
                PaymentType = paymentType,
                CashReceived = cashReceived,
                ChangeAmount = changeAmount,
                SaleDate = DateTime.Now
            };

            foreach (var cartItem in _cartItems)
            {
                sale.Items.Add(new SaleItem
                {
                    ProductId = cartItem.ProductId,
                    Barcode = cartItem.Barcode,
                    ProductName = cartItem.ProductName,
                    UnitPrice = cartItem.UnitPrice,
                    Quantity = cartItem.Quantity,
                    TotalPrice = cartItem.TotalPrice
                });
            }

            _saleRepo.CompleteSale(sale);
            SoundService.PlayCashDrawerSound();

            string receiptText = _receiptService.BuildReceiptText(sale);

            // Clear Cart & Show Last Sale Summary
            decimal completedAmount = sale.FinalAmount;
            string completedReceipt = sale.ReceiptNumber;
            _cartItems.Clear();
            UpdateCartTotals();
            _lblCartSummary.Text = $"Son Satış: {paymentType} ({completedAmount:N2} ₺ - Fiş #{completedReceipt})  |  Sepet: 0 Kalem";

            // Notify cashier if change needs to be given
            if (paymentType == "Nakit" && changeAmount > 0)
            {
                MessageBox.Show(
                    $"SATIŞ TAMAMLANDI!\n\nVERİLECEK PARA ÜSTÜ: {changeAmount:N2} ₺\n\nFiş No: {sale.ReceiptNumber}",
                    "Satış Başarılı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            SoundService.PlayErrorBeep();
            MessageBox.Show($"Satış kaydedilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenProductForm(string? initialBarcode = null)
    {
        using var frm = new ProductForm(initialBarcode);
        frm.ShowDialog(this);
        LoadQuickButtons();
        _txtBarcode.Focus();
    }

    private void OpenReportForm()
    {
        using var frm = new ReportForm();
        frm.ShowDialog(this);
        _txtBarcode.Focus();
    }

    private void OpenSettingsForm()
    {
        using var frm = new SettingsForm();
        frm.ShowDialog(this);
        LoadHeaderAndSettings();
        LoadQuickButtons();
        _txtBarcode.Focus();
    }

    private void ToggleFullScreen()
    {
        if (!_isFullScreen)
        {
            _previousBorderStyle = this.FormBorderStyle;
            _previousWindowState = this.WindowState;
            _previousBounds = this.Bounds;

            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Bounds = Screen.FromControl(this).Bounds;
            _isFullScreen = true;

            _btnToggleFullScreen.Text = "Pencere Modu (F9)";
            Theme.ApplyModernButton(_btnToggleFullScreen, Theme.AmberWarning, Color.White, 9.5F, true);
        }
        else
        {
            this.FormBorderStyle = _previousBorderStyle != FormBorderStyle.None ? _previousBorderStyle : FormBorderStyle.Sizable;
            if (_previousWindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                this.Bounds = _previousBounds;
            }
            _isFullScreen = false;

            _btnToggleFullScreen.Text = "Tam Ekran (F9)";
            Theme.ApplyModernButton(_btnToggleFullScreen, Color.FromArgb(13, 148, 136), Color.White, 9.5F, true);
        }
    }

    private void OnMainFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Alt && e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            ToggleFullScreen();
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.F1:
                e.Handled = true;
                _txtBarcode.Focus();
                _txtBarcode.SelectAll();
                break;
            case Keys.F2:
                e.Handled = true;
                OpenProductForm();
                break;
            case Keys.F3:
                e.Handled = true;
                OpenReportForm();
                break;
            case Keys.F4:
                e.Handled = true;
                OpenSettingsForm();
                break;
            case Keys.F9:
                e.Handled = true;
                ToggleFullScreen();
                break;
            case Keys.F11:
                e.Handled = true;
                CompleteCardSale();
                break;
            case Keys.F12:
                e.Handled = true;
                CompleteCashSale();
                break;
            case Keys.Delete:
                e.Handled = true;
                RemoveSelectedItem();
                break;
            case Keys.Escape:
                e.Handled = true;
                ClearCartWithConfirm();
                break;
        }
    }
}
