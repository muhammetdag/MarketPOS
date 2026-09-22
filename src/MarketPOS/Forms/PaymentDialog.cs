using MarketPOS.UI;

namespace MarketPOS.Forms;

public class PaymentDialog : Form
{
    private readonly decimal _totalAmount;
    private decimal _cashReceived;
    private decimal _changeAmount;

    private Label _lblTotalValue = null!;
    private TextBox _txtCashReceived = null!;
    private Label _lblChangeValue = null!;
    private Button _btnConfirm = null!;
    private Button _btnCancel = null!;

    public decimal CashReceived => _cashReceived;
    public decimal ChangeAmount => _changeAmount;

    public PaymentDialog(decimal totalAmount)
    {
        _totalAmount = totalAmount;
        _cashReceived = totalAmount; // Default to exact amount
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Nakit Ödeme & Para Üstü (F12)";
        this.Size = new Size(520, 480);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Theme.AppBackground;
        this.KeyPreview = true;

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Theme.HeaderBg
        };
        var lblTitle = new Label
        {
            Text = "NAKİT ÖDEME ALINIYOR",
            Font = Theme.LargeFont,
            ForeColor = Color.White,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlHeader.Controls.Add(lblTitle);

        // Content Panel
        var pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24)
        };

        // Total To Pay Row
        var lblTotalTitle = new Label
        {
            Text = "ÖDENECEK TUTAR:",
            Font = Theme.TitleFont,
            ForeColor = Color.FromArgb(71, 85, 105),
            Location = new Point(24, 85),
            Size = new Size(180, 30)
        };

        _lblTotalValue = new Label
        {
            Text = $"{_totalAmount:N2} ₺",
            Font = Theme.LargeFont,
            ForeColor = Theme.DangerRedDark,
            Location = new Point(210, 80),
            Size = new Size(270, 35),
            TextAlign = ContentAlignment.MiddleRight
        };

        // Cash Received Input Row
        var lblCashTitle = new Label
        {
            Text = "ALINAN NAKİT PARA:",
            Font = Theme.TitleFont,
            ForeColor = Color.FromArgb(71, 85, 105),
            Location = new Point(24, 135),
            Size = new Size(180, 35)
        };

        _txtCashReceived = new TextBox
        {
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            Location = new Point(210, 130),
            Size = new Size(270, 40),
            TextAlign = HorizontalAlignment.Right,
            Text = _totalAmount.ToString("0.00")
        };
        _txtCashReceived.TextChanged += OnCashReceivedChanged;

        // Quick Cash Buttons Panel (Tam, 50, 100, 200, 500)
        var pnlQuickMoney = new FlowLayoutPanel
        {
            Location = new Point(24, 185),
            Size = new Size(460, 46),
            FlowDirection = FlowDirection.LeftToRight
        };

        AddQuickMoneyButton(pnlQuickMoney, "TAM", _totalAmount);
        AddQuickMoneyButton(pnlQuickMoney, "50 ₺", 50m);
        AddQuickMoneyButton(pnlQuickMoney, "100 ₺", 100m);
        AddQuickMoneyButton(pnlQuickMoney, "200 ₺", 200m);
        AddQuickMoneyButton(pnlQuickMoney, "500 ₺", 500m);

        // Display Box for Change Amount (Giant LCD Style)
        var pnlChangeBox = new Panel
        {
            Location = new Point(24, 245),
            Size = new Size(456, 95),
            BackColor = Theme.DisplayScreenBg
        };

        var lblChangeTitle = new Label
        {
            Text = "VERİLECEK PARA ÜSTÜ",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _lblChangeValue = new Label
        {
            Text = "0.00 ₺",
            Font = Theme.GiantDisplayFont,
            ForeColor = Theme.DisplayChangeText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlChangeBox.Controls.Add(_lblChangeValue);
        pnlChangeBox.Controls.Add(lblChangeTitle);

        // Action Buttons
        _btnConfirm = new Button
        {
            Text = "ONAYLA (ENTER)",
            Location = new Point(24, 360),
            Size = new Size(240, 55),
            DialogResult = DialogResult.OK
        };
        Theme.ApplyModernButton(_btnConfirm, Theme.SuccessGreen, Color.White, 14F, true);
        _btnConfirm.Click += OnConfirmClick;

        _btnCancel = new Button
        {
            Text = "İPTAL (ESC)",
            Location = new Point(280, 360),
            Size = new Size(200, 55),
            DialogResult = DialogResult.Cancel
        };
        Theme.ApplyModernButton(_btnCancel, Color.FromArgb(100, 116, 139), Color.White, 12F, false);

        this.Controls.Add(_btnCancel);
        this.Controls.Add(_btnConfirm);
        this.Controls.Add(pnlChangeBox);
        this.Controls.Add(pnlQuickMoney);
        this.Controls.Add(_txtCashReceived);
        this.Controls.Add(lblCashTitle);
        this.Controls.Add(_lblTotalValue);
        this.Controls.Add(lblTotalTitle);
        this.Controls.Add(pnlHeader);

        this.AcceptButton = _btnConfirm;
        this.CancelButton = _btnCancel;

        this.Shown += (s, e) =>
        {
            _txtCashReceived.Focus();
            _txtCashReceived.SelectAll();
            CalculateChange();
        };

        this.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        };
    }

    private void AddQuickMoneyButton(FlowLayoutPanel panel, string text, decimal amount)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(86, 40),
            Margin = new Padding(0, 0, 6, 0)
        };
        Theme.ApplyModernButton(btn, Color.FromArgb(226, 232, 240), Color.FromArgb(30, 41, 59), 11F, true);
        btn.Click += (s, e) =>
        {
            _txtCashReceived.Text = amount.ToString("0.00");
            _txtCashReceived.Focus();
            _txtCashReceived.SelectAll();
        };
        panel.Controls.Add(btn);
    }

    private void OnCashReceivedChanged(object? sender, EventArgs e)
    {
        CalculateChange();
    }

    private void CalculateChange()
    {
        string raw = _txtCashReceived.Text.Replace("₺", "").Replace(",", ".").Trim();
        if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal cash))
        {
            _cashReceived = cash;
            _changeAmount = cash - _totalAmount;

            if (_changeAmount >= 0)
            {
                _lblChangeValue.ForeColor = Theme.DisplayChangeText;
                _lblChangeValue.Text = $"{_changeAmount:N2} ₺";
                _btnConfirm.Enabled = true;
            }
            else
            {
                _lblChangeValue.ForeColor = Theme.DangerRed;
                _lblChangeValue.Text = $"EKSİK: {Math.Abs(_changeAmount):N2} ₺";
                _btnConfirm.Enabled = false;
            }
        }
        else
        {
            _lblChangeValue.Text = "GEÇERSİZ TUTAR";
            _btnConfirm.Enabled = false;
        }
    }

    private void OnConfirmClick(object? sender, EventArgs e)
    {
        if (_changeAmount < 0)
        {
            MessageBox.Show("Alınan para ödenecek tutardan az olamaz!", "Eksik Tutar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None;
            return;
        }
        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}
