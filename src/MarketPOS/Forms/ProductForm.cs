using MarketPOS.Data.Repositories;
using MarketPOS.Models;
using MarketPOS.Services;
using MarketPOS.UI;

namespace MarketPOS.Forms;

public class ProductForm : Form
{
    private readonly ProductRepository _productRepo = new();
    private Product? _editingProduct;

    // Controls
    private TextBox _txtBarcode = null!;
    private TextBox _txtName = null!;
    private TextBox _txtSellPrice = null!;
    private TextBox _txtBuyPrice = null!;
    private NumericUpDown _numStock = null!;
    private ComboBox _cmbUnit = null!;
    private ComboBox _cmbKdv = null!;
    private CheckBox _chkFastProduct = null!;
    private Button _btnSave = null!;
    private Button _btnClear = null!;
    private Button _btnGenBarcode = null!;

    private TextBox _txtSearch = null!;
    private DataGridView _gridProducts = null!;
    private Label _lblStatusInfo = null!;

    public ProductForm(string? initialBarcode = null)
    {
        InitializeComponent();
        if (!string.IsNullOrWhiteSpace(initialBarcode))
        {
            _txtBarcode.Text = initialBarcode;
            CheckBarcodeExisting(initialBarcode);
        }
    }

    private void InitializeComponent()
    {
        this.Text = "Ürün ve Stok Yönetimi";
        this.Size = new Size(1060, 720);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(950, 650);
        this.BackColor = Theme.AppBackground;
        this.KeyPreview = true;

        // 1. Header Bar
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Theme.HeaderBg
        };
        var lblTitle = new Label
        {
            Text = "ÜRÜN VE STOK YÖNETİMİ",
            Font = Theme.HeaderFont,
            ForeColor = Theme.HeaderText,
            Dock = DockStyle.Left,
            Padding = new Padding(20, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };
        var lblHelp = new Label
        {
            Text = "İpucu: Barkodu okutun, bilgileri girip 'KAYDET'e basın.",
            Font = Theme.RegularFont,
            ForeColor = Color.FromArgb(148, 163, 184),
            Dock = DockStyle.Right,
            Padding = new Padding(0, 0, 20, 0),
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblHelp);
        pnlHeader.Controls.Add(lblTitle);

        // 2. Main Layout (Top: Input Card, Bottom: Data Table)
        var pnlMain = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15)
        };

        // Input Card Panel
        var pnlInputCard = new GroupBox
        {
            Text = "  HIZLI ÜRÜN VE STOK GİRİŞİ  ",
            Font = Theme.TitleFont,
            ForeColor = Color.FromArgb(30, 41, 59),
            Dock = DockStyle.Top,
            Height = 220,
            BackColor = Theme.SurfaceBg,
            Padding = new Padding(15)
        };

        // Row 1: Barcode + Gen Barcode + Product Name
        var lblBarcode = new Label { Text = "Barkod:", Location = new Point(15, 35), Size = new Size(90, 25), Font = Theme.BoldFont };
        _txtBarcode = new TextBox { Location = new Point(110, 32), Size = new Size(220, 30), Font = Theme.RegularFont };
        _txtBarcode.KeyDown += OnBarcodeKeyDown;
        _txtBarcode.Leave += (s, e) => CheckBarcodeExisting(_txtBarcode.Text);

        _btnGenBarcode = new Button { Text = "Otomatik Üret", Location = new Point(335, 31), Size = new Size(110, 32) };
        Theme.ApplyModernButton(_btnGenBarcode, Color.FromArgb(100, 116, 139), Color.White, 9F, true);
        _btnGenBarcode.Click += OnGenerateBarcodeClick;

        var lblName = new Label { Text = "Ürün Adı:", Location = new Point(460, 35), Size = new Size(80, 25), Font = Theme.BoldFont };
        _txtName = new TextBox { Location = new Point(545, 32), Size = new Size(350, 30), Font = Theme.RegularFont };

        // Row 2: Sell Price + Buy Price + Stock Quantity + Unit
        var lblSellPrice = new Label { Text = "Satış Fiyatı (₺):", Location = new Point(15, 80), Size = new Size(110, 25), Font = Theme.BoldFont };
        _txtSellPrice = new TextBox { Location = new Point(130, 77), Size = new Size(110, 30), Font = Theme.RegularFont, Text = "0.00" };

        var lblBuyPrice = new Label { Text = "Alış Fiyatı (₺):", Location = new Point(255, 80), Size = new Size(110, 25), Font = Theme.BoldFont };
        _txtBuyPrice = new TextBox { Location = new Point(370, 77), Size = new Size(110, 30), Font = Theme.RegularFont, Text = "0.00" };

        var lblStock = new Label { Text = "Stok Miktarı:", Location = new Point(495, 80), Size = new Size(95, 25), Font = Theme.BoldFont };
        _numStock = new NumericUpDown
        {
            Location = new Point(595, 77),
            Size = new Size(100, 30),
            Font = Theme.RegularFont,
            DecimalPlaces = 2,
            Maximum = 999999,
            Minimum = 0,
            Value = 1
        };

        var lblUnit = new Label { Text = "Birim:", Location = new Point(710, 80), Size = new Size(50, 25), Font = Theme.BoldFont };
        _cmbUnit = new ComboBox
        {
            Location = new Point(765, 77),
            Size = new Size(90, 30),
            Font = Theme.RegularFont,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbUnit.Items.AddRange(new object[] { "Adet", "Kg", "Paket", "Litre", "Koli" });
        _cmbUnit.SelectedIndex = 0;

        // Row 3: Kdv + Fast Button Check + Action Buttons
        var lblKdv = new Label { Text = "KDV (%):", Location = new Point(15, 125), Size = new Size(90, 25), Font = Theme.BoldFont };
        _cmbKdv = new ComboBox
        {
            Location = new Point(110, 122),
            Size = new Size(90, 30),
            Font = Theme.RegularFont,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbKdv.Items.AddRange(new object[] { "1", "10", "20" });
        _cmbKdv.SelectedIndex = 0;

        _chkFastProduct = new CheckBox
        {
            Text = "Hızlı Ürünler Listesine Ekle",
            Location = new Point(220, 124),
            Size = new Size(240, 26),
            Font = Theme.RegularFont
        };

        _lblStatusInfo = new Label
        {
            Text = "Yeni ürün kaydı yapmaya hazır.",
            Location = new Point(15, 170),
            Size = new Size(450, 25),
            Font = Theme.BoldFont,
            ForeColor = Theme.PrimaryBlueDark
        };

        _btnSave = new Button { Text = "KAYDET (F2)", Location = new Point(545, 140), Size = new Size(170, 50) };
        Theme.ApplyModernButton(_btnSave, Theme.SuccessGreen, Color.White, 12F, true);
        _btnSave.Click += OnSaveClick;

        _btnClear = new Button { Text = "TEMİZLE / YENİ", Location = new Point(725, 140), Size = new Size(160, 50) };
        Theme.ApplyModernButton(_btnClear, Color.FromArgb(100, 116, 139), Color.White, 11F, true);
        _btnClear.Click += (s, e) => ClearInputs();

        pnlInputCard.Controls.AddRange(new Control[] {
            lblBarcode, _txtBarcode, _btnGenBarcode, lblName, _txtName,
            lblSellPrice, _txtSellPrice, lblBuyPrice, _txtBuyPrice, lblStock, _numStock, lblUnit, _cmbUnit,
            lblKdv, _cmbKdv, _chkFastProduct, _lblStatusInfo, _btnSave, _btnClear
        });

        // Search & Table Panel
        var pnlTableContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 15, 0, 0)
        };

        // Search Bar Area
        var pnlSearch = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.Transparent
        };

        var lblSearch = new Label { Text = "Ürün Ara:", Location = new Point(5, 12), Size = new Size(80, 25), Font = Theme.BoldFont };
        _txtSearch = new TextBox { Location = new Point(90, 8), Size = new Size(320, 30), Font = Theme.RegularFont, PlaceholderText = "Barkod veya ürün adı..." };
        _txtSearch.TextChanged += (s, e) => RefreshProductList(_txtSearch.Text);

        var btnDelete = new Button { Text = "Seçili Ürünü Sil", Location = new Point(430, 8), Size = new Size(140, 34) };
        Theme.ApplyModernButton(btnDelete, Theme.DangerRed, Color.White, 10F, true);
        btnDelete.Click += OnDeleteClick;

        var btnAddStock = new Button { Text = "Stok Ekle", Location = new Point(580, 8), Size = new Size(120, 34) };
        Theme.ApplyModernButton(btnAddStock, Theme.PrimaryBlue, Color.White, 10F, true);
        btnAddStock.Click += (s, e) => OpenAddStockDialog();

        var btnLowStockFilter = new Button { Text = "Kritik Stoklar (<=5)", Location = new Point(710, 8), Size = new Size(150, 34) };
        Theme.ApplyModernButton(btnLowStockFilter, Theme.AmberWarning, Color.White, 10F, true);
        btnLowStockFilter.Click += (s, e) => ShowLowStockOnly();

        pnlSearch.Controls.AddRange(new Control[] { lblSearch, _txtSearch, btnDelete, btnAddStock, btnLowStockFilter });

        // DataGridView
        _gridProducts = new DataGridView
        {
            Dock = DockStyle.Fill
        };
        Theme.ApplyModernGrid(_gridProducts);
        ConfigureGridColumns();
        _gridProducts.CellDoubleClick += OnGridCellDoubleClick;

        pnlTableContainer.Controls.Add(_gridProducts);
        pnlTableContainer.Controls.Add(pnlSearch);

        pnlMain.Controls.Add(pnlTableContainer);
        pnlMain.Controls.Add(pnlInputCard);

        this.Controls.Add(pnlMain);
        this.Controls.Add(pnlHeader);

        this.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.F2)
            {
                OnSaveClick(s, e);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                ClearInputs();
            }
        };

        this.Shown += (s, e) =>
        {
            RefreshProductList();
            _txtBarcode.Focus();
        };
    }

    private void ConfigureGridColumns()
    {
        _gridProducts.AutoGenerateColumns = false;
        _gridProducts.AllowUserToResizeColumns = false;
        _gridProducts.Columns.Clear();

        // 1. ID
        var colId = new DataGridViewTextBoxColumn
        {
            HeaderText = "ID",
            DataPropertyName = "Id",
            Width = 55,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False
        };
        colId.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colId.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridProducts.Columns.Add(colId);

        // 2. Barkod
        var colBarcode = new DataGridViewTextBoxColumn
        {
            HeaderText = "Barkod",
            DataPropertyName = "Barcode",
            Width = 140,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False
        };
        colBarcode.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
        colBarcode.HeaderCell.Style.Padding = new Padding(8, 0, 0, 0);
        colBarcode.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        colBarcode.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
        _gridProducts.Columns.Add(colBarcode);

        // 3. Ürün Adı
        var colName = new DataGridViewTextBoxColumn
        {
            HeaderText = "Ürün Adı",
            DataPropertyName = "Name",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False
        };
        colName.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
        colName.HeaderCell.Style.Padding = new Padding(8, 0, 0, 0);
        colName.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        colName.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
        _gridProducts.Columns.Add(colName);

        // 4. Satış Fiyatı (TL)
        var colSellPrice = new DataGridViewTextBoxColumn
        {
            HeaderText = "Satış Fiyatı (TL)",
            DataPropertyName = "SellPrice",
            Width = 125,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False
        };
        colSellPrice.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        colSellPrice.HeaderCell.Style.Padding = new Padding(0, 0, 10, 0);
        colSellPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        colSellPrice.DefaultCellStyle.Padding = new Padding(0, 0, 10, 0);
        colSellPrice.DefaultCellStyle.Format = "N2";
        _gridProducts.Columns.Add(colSellPrice);

        // 5. Alış Fiyatı (TL)
        var colBuyPrice = new DataGridViewTextBoxColumn
        {
            HeaderText = "Alış Fiyatı (TL)",
            DataPropertyName = "BuyPrice",
            Width = 120,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False
        };
        colBuyPrice.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        colBuyPrice.HeaderCell.Style.Padding = new Padding(0, 0, 10, 0);
        colBuyPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        colBuyPrice.DefaultCellStyle.Padding = new Padding(0, 0, 10, 0);
        colBuyPrice.DefaultCellStyle.Format = "N2";
        _gridProducts.Columns.Add(colBuyPrice);

        // 6. Stok
        var colStock = new DataGridViewTextBoxColumn
        {
            HeaderText = "Stok",
            DataPropertyName = "StockQuantity",
            Width = 85,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False
        };
        colStock.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colStock.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colStock.DefaultCellStyle.Format = "0.##";
        _gridProducts.Columns.Add(colStock);

        // 7. Birim
        var colUnit = new DataGridViewTextBoxColumn
        {
            HeaderText = "Birim",
            DataPropertyName = "Unit",
            Width = 75,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False
        };
        colUnit.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colUnit.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridProducts.Columns.Add(colUnit);

        // 8. KDV
        var colKdv = new DataGridViewTextBoxColumn
        {
            HeaderText = "KDV",
            DataPropertyName = "KdvRate",
            Width = 75,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False
        };
        colKdv.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colKdv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridProducts.Columns.Add(colKdv);

        // 9. Hızlı Ürün
        var colFast = new DataGridViewCheckBoxColumn
        {
            HeaderText = "Hızlı Ürün",
            DataPropertyName = "IsFastProduct",
            Width = 90,
            ReadOnly = false,
            Resizable = DataGridViewTriState.False
        };
        colFast.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colFast.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridProducts.Columns.Add(colFast);

        _gridProducts.CellFormatting += (s, e) =>
        {
            if (e.RowIndex >= 0 && _gridProducts.Columns[e.ColumnIndex].DataPropertyName == "KdvRate" && e.Value != null)
            {
                e.Value = $"%{e.Value}";
                e.FormattingApplied = true;
            }
        };

        _gridProducts.CellContentClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && _gridProducts.Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn)
            {
                _gridProducts.CommitEdit(DataGridViewDataErrorContexts.Commit);
                if (_gridProducts.Rows[e.RowIndex].DataBoundItem is Product p)
                {
                    _productRepo.Update(p);
                    SoundService.PlaySuccessBeep();
                }
            }
        };
    }

    private void RefreshProductList(string? query = null)
    {
        var list = _productRepo.Search(query ?? "");
        _gridProducts.DataSource = list;

        // Color low stock rows
        foreach (DataGridViewRow row in _gridProducts.Rows)
        {
            if (row.DataBoundItem is Product p && p.StockQuantity <= 5)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242); // Light red
                row.DefaultCellStyle.ForeColor = Color.FromArgb(185, 28, 28);
            }
        }
    }

    private void ShowLowStockOnly()
    {
        var list = _productRepo.GetLowStockProducts(5);
        _gridProducts.DataSource = list;
        _lblStatusInfo.Text = $"Kritik seviyedeki {list.Count} ürün listelendi.";
    }

    private void CheckBarcodeExisting(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return;

        var existing = _productRepo.GetByBarcode(barcode.Trim());
        if (existing != null)
        {
            // Existing product found! Populate data for update / stock increment
            _editingProduct = existing;
            _txtName.Text = existing.Name;
            _txtSellPrice.Text = existing.SellPrice.ToString("0.00");
            _txtBuyPrice.Text = existing.BuyPrice.ToString("0.00");
            _numStock.Value = existing.StockQuantity;
            _cmbUnit.SelectedItem = existing.Unit;
            _cmbKdv.SelectedItem = existing.KdvRate.ToString();
            _chkFastProduct.Checked = existing.IsFastProduct;

            _lblStatusInfo.Text = $"Kayıtlı ürün bulundu! Bilgileri güncelleyebilir veya stok artırabilirsiniz.";
            _lblStatusInfo.ForeColor = Theme.AmberWarning;
            _btnSave.Text = "GÜNCELLE (F2)";
            SoundService.PlaySuccessBeep();
        }
        else
        {
            // New product!
            _editingProduct = null;
            _lblStatusInfo.Text = "YENİ ÜRÜN: İsim, satış fiyatı ve ilk stok adedini giriniz.";
            _lblStatusInfo.ForeColor = Theme.SuccessGreenDark;
            _btnSave.Text = "KAYDET (F2)";
        }
    }

    private void OnBarcodeKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            CheckBarcodeExisting(_txtBarcode.Text);
            _txtName.Focus();
            _txtName.SelectAll();
        }
    }

    private void OnGenerateBarcodeClick(object? sender, EventArgs e)
    {
        // Generate random 13-digit EAN-like barcode for fresh/unlabeled items
        var random = new Random();
        string generated = "200" + random.Next(100000000, 999999999).ToString();
        _txtBarcode.Text = generated;
        _editingProduct = null;
        _lblStatusInfo.Text = "Otomatik barkod üretildi. Şimdi ürün adı ve fiyatını girin.";
        _txtName.Focus();
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        string barcode = _txtBarcode.Text.Trim();
        string name = _txtName.Text.Trim();

        if (string.IsNullOrWhiteSpace(barcode))
        {
            MessageBox.Show("Lütfen ürün barkodunu okutun veya girin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtBarcode.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Lütfen ürün adını yazın!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtName.Focus();
            return;
        }

        decimal sellPrice = ParseDecimal(_txtSellPrice.Text);
        decimal buyPrice = ParseDecimal(_txtBuyPrice.Text);
        decimal stock = _numStock.Value;
        string unit = _cmbUnit.SelectedItem?.ToString() ?? "Adet";
        int kdv = int.TryParse(_cmbKdv.SelectedItem?.ToString(), out int k) ? k : 1;
        bool isFast = _chkFastProduct.Checked;

        if (sellPrice <= 0)
        {
            MessageBox.Show("Satış fiyatı 0 veya daha küçük olamaz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtSellPrice.Focus();
            return;
        }

        try
        {
            if (_editingProduct != null)
            {
                // Update
                _editingProduct.Barcode = barcode;
                _editingProduct.Name = name;
                _editingProduct.SellPrice = sellPrice;
                _editingProduct.BuyPrice = buyPrice;
                _editingProduct.StockQuantity = stock;
                _editingProduct.Unit = unit;
                _editingProduct.KdvRate = kdv;
                _editingProduct.IsFastProduct = isFast;

                _productRepo.Update(_editingProduct);
                _lblStatusInfo.Text = $"'{name}' ürünü başarıyla güncellendi.";
            }
            else
            {
                // New Product
                var newProduct = new Product
                {
                    Barcode = barcode,
                    Name = name,
                    SellPrice = sellPrice,
                    BuyPrice = buyPrice,
                    StockQuantity = stock,
                    Unit = unit,
                    KdvRate = kdv,
                    IsFastProduct = isFast
                };

                _productRepo.Add(newProduct);
                _lblStatusInfo.Text = $"'{name}' sisteme ilk stoğuyla ({stock} {unit}) kaydedildi!";
            }

            SoundService.PlaySuccessBeep();
            RefreshProductList();
            ClearInputs();
        }
        catch (Exception ex)
        {
            SoundService.PlayErrorBeep();
            MessageBox.Show($"Kaydetme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearInputs()
    {
        _editingProduct = null;
        _txtBarcode.Clear();
        _txtName.Clear();
        _txtSellPrice.Text = "0.00";
        _txtBuyPrice.Text = "0.00";
        _numStock.Value = 1;
        _cmbUnit.SelectedIndex = 0;
        _cmbKdv.SelectedIndex = 0;
        _chkFastProduct.Checked = false;
        _btnSave.Text = "KAYDET (F2)";
        _lblStatusInfo.Text = "Yeni ürün okutmaya hazır...";
        _lblStatusInfo.ForeColor = Theme.PrimaryBlueDark;
        _txtBarcode.Focus();
    }

    private void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && _gridProducts.Rows[e.RowIndex].DataBoundItem is Product p)
        {
            _editingProduct = p;
            _txtBarcode.Text = p.Barcode;
            _txtName.Text = p.Name;
            _txtSellPrice.Text = p.SellPrice.ToString("0.00");
            _txtBuyPrice.Text = p.BuyPrice.ToString("0.00");
            _numStock.Value = p.StockQuantity;
            _cmbUnit.SelectedItem = p.Unit;
            _cmbKdv.SelectedItem = p.KdvRate.ToString();
            _chkFastProduct.Checked = p.IsFastProduct;

            _btnSave.Text = "GÜNCELLE (F2)";
            _lblStatusInfo.Text = $"'{p.Name}' düzenlenmek üzere seçildi.";
            _lblStatusInfo.ForeColor = Theme.AmberWarning;
            _txtName.Focus();
        }
    }

    private void OnDeleteClick(object? sender, EventArgs e)
    {
        if (_gridProducts.SelectedRows.Count == 0 || _gridProducts.SelectedRows[0].DataBoundItem is not Product p)
        {
            MessageBox.Show("Lütfen silmek istediğiniz ürünü tablodan seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var res = MessageBox.Show($"'{p.Name}' ürününü silmek istediğinize emin misiniz?", "Ürün Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (res == DialogResult.Yes)
        {
            _productRepo.Delete(p.Id);
            RefreshProductList();
            ClearInputs();
            MessageBox.Show("Ürün başarıyla silindi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void OpenAddStockDialog()
    {
        if (_gridProducts.SelectedRows.Count == 0 || _gridProducts.SelectedRows[0].DataBoundItem is not Product p)
        {
            MessageBox.Show("Lütfen stok eklemek istediğiniz ürünü tablodan seçin.", "Ürün Seçilmedi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new Form
        {
            Text = $"Stok Ekle - {p.Name}",
            Size = new Size(460, 330),
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
            Text = p.Name,
            Font = Theme.TitleFont,
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 26
        };

        var lblHeaderBarcode = new Label
        {
            Text = $"Barkod: {p.Barcode}  |  Mevcut Stok: {p.StockQuantity:0.##} {p.Unit}",
            Font = Theme.RegularFont,
            ForeColor = Color.FromArgb(148, 163, 184),
            Dock = DockStyle.Bottom,
            Height = 22
        };

        pnlHeader.Controls.Add(lblHeaderProd);
        pnlHeader.Controls.Add(lblHeaderBarcode);

        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var lblPrompt = new Label
        {
            Text = $"Eklenecek Miktar ({p.Unit}):",
            Font = Theme.BoldFont,
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(20, 16),
            Size = new Size(200, 25)
        };

        var txtQty = new TextBox
        {
            Text = "10",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            Location = new Point(20, 44),
            Size = new Size(400, 40),
            TextAlign = HorizontalAlignment.Center
        };

        var pnlPresets = new FlowLayoutPanel
        {
            Location = new Point(20, 94),
            Size = new Size(400, 42),
            FlowDirection = FlowDirection.LeftToRight
        };

        int[] presets = { 5, 10, 25, 50, 100 };
        foreach (int pr in presets)
        {
            var btnPr = new Button
            {
                Text = $"+{pr}",
                Size = new Size(72, 34),
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplyModernButton(btnPr, Color.FromArgb(226, 232, 240), Color.FromArgb(30, 41, 59), 10F, true);
            btnPr.Click += (s, e) =>
            {
                if (decimal.TryParse(txtQty.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal cur))
                {
                    txtQty.Text = (cur + pr).ToString("0.##");
                }
                else
                {
                    txtQty.Text = pr.ToString();
                }
                txtQty.Focus();
                txtQty.SelectAll();
            };
            pnlPresets.Controls.Add(btnPr);
        }

        var btnConfirm = new Button
        {
            Text = "STOĞA EKLE (ENTER)",
            Location = new Point(20, 150),
            Size = new Size(230, 48),
            DialogResult = DialogResult.OK
        };
        Theme.ApplyModernButton(btnConfirm, Theme.SuccessGreen, Color.White, 11F, true);

        var btnCancel = new Button
        {
            Text = "İPTAL (ESC)",
            Location = new Point(260, 150),
            Size = new Size(160, 48),
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
            if (decimal.TryParse(clean, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal qtyToAdd) && qtyToAdd > 0)
            {
                _productRepo.AddStock(p.Id, qtyToAdd);
                RefreshProductList();
                SoundService.PlaySuccessBeep();
                _lblStatusInfo.Text = $"'{p.Name}' stoğuna +{qtyToAdd:0.##} {p.Unit} eklendi. (Yeni Stok: {p.StockQuantity + qtyToAdd:0.##})";
                _lblStatusInfo.ForeColor = Theme.SuccessGreenDark;
            }
            else
            {
                MessageBox.Show("Lütfen geçerli ve 0'dan büyük bir stok miktarı girin.", "Geçersiz Miktar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private static decimal ParseDecimal(string text)
    {
        string clean = text.Replace("₺", "").Replace(",", ".").Trim();
        if (decimal.TryParse(clean, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal val))
        {
            return val;
        }
        return 0m;
    }
}
