using MarketPOS.Models;
using MarketPOS.UI;

namespace MarketPOS.Forms;

public enum StockPromptResult
{
    Cancel,
    ProceedAnyway,
    AddRemainingOnly
}

public class StockWarningDialog : Form
{
    private readonly Product _product;
    private readonly decimal _currentCartQty;
    private readonly decimal _targetTotalQty;
    private readonly bool _isStrictBlock;

    public StockPromptResult Result { get; private set; } = StockPromptResult.Cancel;

    public StockWarningDialog(Product product, decimal currentCartQty, decimal targetTotalQty, bool isStrictBlock = false)
    {
        _product = product;
        _currentCartQty = currentCartQty;
        _targetTotalQty = targetTotalQty;
        _isStrictBlock = isStrictBlock;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        decimal availableStock = _product.StockQuantity;
        decimal remainingAvailable = Math.Max(0, availableStock - _currentCartQty);
        decimal exceededAmount = _targetTotalQty - Math.Max(0, availableStock);

        this.Text = _isStrictBlock ? "Stok Yetersiz - Satış Engellendi" : "Yetersiz Stok Uyarısı - Ek Onay";
        this.Size = new Size(560, 480);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Theme.AppBackground;
        this.KeyPreview = true;

        // 1. Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = _isStrictBlock ? Theme.DangerRedDark : Color.FromArgb(194, 65, 12) // Burnt amber-orange
        };

        var lblHeaderTitle = new Label
        {
            Text = _isStrictBlock ? "⛔  STOK YETERSİZ / TÜKENDİ" : "⚠️  YETERSİZ STOK VE EK ONAY",
            Font = Theme.LargeFont,
            ForeColor = Color.White,
            Location = new Point(20, 12),
            Size = new Size(510, 28)
        };

        var lblHeaderSub = new Label
        {
            Text = _isStrictBlock 
                ? "Sistem ayarlarında stoksuz satış engellendiği için işlem yapılamaz." 
                : "Kayıtlı stok sınırı aşılıyor! Kasiyer ek onayı gereklidir.",
            Font = Theme.RegularFont,
            ForeColor = Color.FromArgb(254, 243, 199),
            Location = new Point(22, 42),
            Size = new Size(510, 22)
        };

        pnlHeader.Controls.AddRange(new Control[] { lblHeaderTitle, lblHeaderSub });

        // 2. Content Panel
        var pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        // Product Info Card
        var pnlCard = new Panel
        {
            Location = new Point(20, 16),
            Size = new Size(504, 80),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblProdName = new Label
        {
            Text = _product.Name,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(14, 12),
            Size = new Size(475, 30),
            UseMnemonic = false
        };

        var lblBarcodeInfo = new Label
        {
            Text = $"Barkod: {_product.Barcode}   |   Birim Fiyatı: {_product.SellPrice:N2} ₺   |   Birim: {_product.Unit}",
            Font = Theme.RegularFont,
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(15, 45),
            Size = new Size(475, 24)
        };

        pnlCard.Controls.AddRange(new Control[] { lblProdName, lblBarcodeInfo });

        // 3 Badges Panel (Current Stock, Requested Total, Exceeded Amount)
        var pnlBadges = new Panel
        {
            Location = new Point(20, 108),
            Size = new Size(504, 84),
            BackColor = Color.FromArgb(248, 250, 252),
            BorderStyle = BorderStyle.FixedSingle
        };

        // Badge 1: Mevcut Stok
        var pnlB1 = CreateStatBadge(
            "Mevcut Kayıtlı Stok",
            availableStock > 0 ? $"{availableStock:0.##} {_product.Unit}" : $"0 {_product.Unit} (Tükendi)",
            availableStock > 0 ? Theme.PrimaryBlueDark : Theme.DangerRedDark,
            new Point(8, 8),
            new Size(158, 66)
        );

        // Badge 2: İstenen Toplam
        var pnlB2 = CreateStatBadge(
            "Sepetteki Toplam",
            $"{_targetTotalQty:0.##} {_product.Unit}",
            Color.FromArgb(30, 41, 59),
            new Point(172, 8),
            new Size(158, 66)
        );

        // Badge 3: Aşılan Miktar
        var pnlB3 = CreateStatBadge(
            "Aşılan Miktar",
            $"+{exceededAmount:0.##} {_product.Unit}",
            Theme.DangerRedDark,
            new Point(336, 8),
            new Size(158, 66)
        );

        pnlBadges.Controls.AddRange(new Control[] { pnlB1, pnlB2, pnlB3 });

        // Warning Explanation Text
        var lblExplanation = new Label
        {
            Location = new Point(20, 204),
            Size = new Size(504, 75),
            Font = new Font("Segoe UI", 10.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(51, 65, 85)
        };

        if (_isStrictBlock)
        {
            lblExplanation.Text = availableStock > 0
                ? $"Bu ürünün kayıtlı maksimum stoğu {availableStock:0.##} {_product.Unit}'dir.\nAyarlarınızda 'Stoksuz Satışı Engelle' açık olduğu için mevcut stoğun üzerindeki miktar eklenemez. İsterseniz sadece kalan stoğu sepete ekleyebilirsiniz."
                : $"Bu ürünün kayıtlı stoğu tükenmiştir (0 {_product.Unit}).\nAyarlarınızda 'Stoksuz Satışı Engelle' açık olduğu için bu ürün sepete eklenemez.";
        }
        else
        {
            lblExplanation.Text = availableStock > 0
                ? $"Bu ürünün kayıtlı maksimum stoğu {availableStock:0.##} {_product.Unit}'dir.\nSatış yapılırsa ürün stoğu eksiye düşecektir ({availableStock - _targetTotalQty:0.##} {_product.Unit}).\n\nYine de eklemek için ONAY veriyor musunuz?"
                : $"Bu ürünün kayıtlı stoğu tükenmiştir (Mevcut Stok: {availableStock:0.##} {_product.Unit}).\nSatış yapılırsa ürün stoğu eksiye düşecektir.\n\nYine de eklemek için ONAY veriyor musunuz?";
        }

        // Action Buttons Panel
        var pnlButtons = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 75,
            BackColor = Theme.SurfaceBg,
            Padding = new Padding(20, 14, 20, 14)
        };

        Button? btnProceed = null;
        Button? btnAddRemaining = null;
        Button btnCancel;

        if (!_isStrictBlock)
        {
            btnProceed = new Button
            {
                Text = "EVET, ONAYLA VE EKLE (ENTER)",
                Size = new Size(220, 46),
                Dock = DockStyle.Left
            };
            Theme.ApplyModernButton(btnProceed, Color.FromArgb(217, 119, 6), Color.White, 10F, true);
            btnProceed.Click += (s, e) =>
            {
                Result = StockPromptResult.ProceedAnyway;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            pnlButtons.Controls.Add(btnProceed);
        }

        if (remainingAvailable > 0)
        {
            btnAddRemaining = new Button
            {
                Text = $"KALAN STOĞU EKLE ({remainingAvailable:0.##})",
                Size = new Size(180, 46),
                Dock = DockStyle.Left,
                Margin = new Padding(10, 0, 0, 0)
            };
            Theme.ApplyModernButton(btnAddRemaining, Theme.PrimaryBlueDark, Color.White, 9.5F, true);
            btnAddRemaining.Click += (s, e) =>
            {
                Result = StockPromptResult.AddRemainingOnly;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            pnlButtons.Controls.Add(btnAddRemaining);
        }

        btnCancel = new Button
        {
            Text = _isStrictBlock && remainingAvailable <= 0 ? "KAPAT (ESC)" : "İPTAL (ESC)",
            Size = new Size(100, 46),
            Dock = DockStyle.Right
        };
        Theme.ApplyModernButton(btnCancel, Color.FromArgb(100, 116, 139), Color.White, 10F, false);
        btnCancel.Click += (s, e) =>
        {
            Result = StockPromptResult.Cancel;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        };
        pnlButtons.Controls.Add(btnCancel);

        // Key Shortcuts
        if (btnProceed != null)
        {
            this.AcceptButton = btnProceed;
        }
        else if (btnAddRemaining != null)
        {
            this.AcceptButton = btnAddRemaining;
        }
        else
        {
            this.AcceptButton = btnCancel;
        }
        this.CancelButton = btnCancel;

        pnlContent.Controls.AddRange(new Control[] { pnlCard, pnlBadges, lblExplanation });
        this.Controls.Add(pnlContent);
        this.Controls.Add(pnlButtons);
        this.Controls.Add(pnlHeader);
    }

    private Panel CreateStatBadge(string title, string value, Color valueColor, Point loc, Size size)
    {
        var pnl = new Panel
        {
            Location = loc,
            Size = size,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblT = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(4, 8),
            Size = new Size(size.Width - 8, 18),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblV = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = valueColor,
            Location = new Point(4, 28),
            Size = new Size(size.Width - 8, 28),
            TextAlign = ContentAlignment.MiddleCenter
        };

        pnl.Controls.Add(lblT);
        pnl.Controls.Add(lblV);
        return pnl;
    }
}
