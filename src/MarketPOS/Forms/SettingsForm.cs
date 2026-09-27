using MarketPOS.Data;
using MarketPOS.Data.Repositories;
using MarketPOS.Services;
using MarketPOS.UI;

namespace MarketPOS.Forms;

public class SettingsForm : Form
{
    private readonly SettingsRepository _settingsRepo = new();
    private bool _isLoading = true;

    private TextBox _txtMarketName = null!;
    private TextBox _txtPhone = null!;
    private TextBox _txtAddress = null!;
    private TextBox _txtReceiptHeader = null!;
    private TextBox _txtReceiptFooter = null!;
    private CheckBox _chkCalculateChange = null!;
    private ComboBox _cmbStockAction = null!;
    private CheckBox _chkSound = null!;
    private Label _lblDbInfo = null!;

    // Telegram Cloud Backup Controls
    private CheckBox _chkTelegramEnabled = null!;
    private TextBox _txtTelegramToken = null!;
    private TextBox _txtTelegramChatId = null!;
    private ComboBox _cmbTelegramPeriod = null!;
    private Button _btnTestTelegram = null!;
    private Label _lblTelegramStatus = null!;

    public SettingsForm()
    {
        InitializeComponent();
        LoadSettings();
        _isLoading = false;
        WireAutoSaveEvents();
    }

    private void InitializeComponent()
    {
        this.Text = "Sistem Ayarları, Telegram Bulut Yedekleme ve config.json";
        this.Size = new Size(740, 720);
        this.MinimumSize = new Size(680, 550);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.BackColor = Theme.AppBackground;

        // 1. Top Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Theme.HeaderBg
        };
        var lblTitle = new Label
        {
            Text = "SİSTEM VE BULUT YEDEKLEME AYARLARI",
            Font = Theme.HeaderFont,
            ForeColor = Theme.HeaderText,
            Dock = DockStyle.Left,
            Padding = new Padding(20, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        // 2. Fixed Bottom Bar (ALWAYS visible on all resolutions and DPI scales)
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 70,
            BackColor = Theme.SurfaceBg,
            Padding = new Padding(20, 10, 20, 10)
        };

        var btnSave = new Button
        {
            Text = "AYARLARI KAYDET VE KAPAT",
            Dock = DockStyle.Right,
            Width = 260
        };
        Theme.ApplyModernButton(btnSave, Theme.SuccessGreen, Color.White, 12F, true);
        btnSave.Click += (s, e) =>
        {
            SaveCurrentSettings(silent: false);
            this.Close();
        };

        var lblAutoSaveNotice = new Label
        {
            Text = "✓ Otomatik Kayıt Aktif: Değişiklikleriniz anında config.json ve SQLite'a kaydedilir.",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Theme.SuccessGreenDark,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        pnlBottom.Controls.Add(lblAutoSaveNotice);
        pnlBottom.Controls.Add(btnSave);

        // 3. Scrollable Content Panel
        var pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20)
        };

        // Market Info Group
        var grpMarket = new GroupBox
        {
            Text = "  Market ve Fiş Bilgileri  ",
            Font = Theme.TitleFont,
            Location = new Point(20, 10),
            Size = new Size(675, 230),
            BackColor = Theme.SurfaceBg
        };

        var lblMarketName = new Label { Text = "Market Adı:", Location = new Point(20, 32), Size = new Size(130, 25), Font = Theme.BoldFont };
        _txtMarketName = new TextBox { Location = new Point(160, 29), Size = new Size(480, 28), Font = Theme.RegularFont };

        var lblPhone = new Label { Text = "Telefon:", Location = new Point(20, 70), Size = new Size(130, 25), Font = Theme.BoldFont };
        _txtPhone = new TextBox { Location = new Point(160, 67), Size = new Size(480, 28), Font = Theme.RegularFont };

        var lblAddress = new Label { Text = "Adres:", Location = new Point(20, 108), Size = new Size(130, 25), Font = Theme.BoldFont };
        _txtAddress = new TextBox { Location = new Point(160, 105), Size = new Size(480, 28), Font = Theme.RegularFont };

        var lblReceiptHeader = new Label { Text = "Fiş Başlığı:", Location = new Point(20, 146), Size = new Size(130, 25), Font = Theme.BoldFont };
        _txtReceiptHeader = new TextBox { Location = new Point(160, 143), Size = new Size(480, 28), Font = Theme.RegularFont };

        var lblReceiptFooter = new Label { Text = "Fiş Alt Notu:", Location = new Point(20, 184), Size = new Size(130, 25), Font = Theme.BoldFont };
        _txtReceiptFooter = new TextBox { Location = new Point(160, 181), Size = new Size(480, 28), Font = Theme.RegularFont };

        grpMarket.Controls.AddRange(new Control[] {
            lblMarketName, _txtMarketName, lblPhone, _txtPhone, lblAddress, _txtAddress,
            lblReceiptHeader, _txtReceiptHeader, lblReceiptFooter, _txtReceiptFooter
        });

        // Sales & Checkout Preferences Group
        var grpSales = new GroupBox
        {
            Text = "  Kasa, Satış ve Stok Aşımı Tercihleri  ",
            Font = Theme.TitleFont,
            Location = new Point(20, 250),
            Size = new Size(675, 195),
            BackColor = Theme.SurfaceBg
        };

        _chkCalculateChange = new CheckBox
        {
            Text = "Nakit Satışta Para Üstü Hesaplama Ekranını Aç (F12)",
            Location = new Point(20, 25),
            Size = new Size(620, 24),
            Font = Theme.BoldFont,
            ForeColor = Theme.PrimaryBlueDark,
            Checked = true
        };

        var lblChangeHelp = new Label
        {
            Text = "• AÇIK (Önerilen): F12'ye basıldığında para üstü ekranı gelir, müşteriden alınan para girilir.\n• KAPALI (Hızlı Kasa): F12'ye basıldığında para üstü sormadan tek tıkla satış tamamlanır.",
            Location = new Point(20, 49),
            Size = new Size(630, 36),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(100, 116, 139)
        };

        var lblStockAction = new Label
        {
            Text = "Stok Miktarı Aşıldığında / Tükendiğinde Kasa Davranışı:",
            Location = new Point(20, 92),
            Size = new Size(420, 22),
            Font = Theme.BoldFont,
            ForeColor = Theme.PrimaryBlueDark
        };

        _cmbStockAction = new ComboBox
        {
            Location = new Point(20, 116),
            Size = new Size(480, 28),
            Font = Theme.RegularFont,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbStockAction.Items.AddRange(new object[] {
            "Kasiyerden Ek Onay İsteyerek Satışa İzin Ver (Önerilen)",
            "Satışı Tamamen Engelle (Eksiye Asla Düşemez)"
        });
        _cmbStockAction.SelectedIndex = 0;

        var lblStockHelp = new Label
        {
            Text = "• Ek Onay: Barkod okutulduğunda stok aşılırsa 'Maksimum stok aşılıyor' uyarısı çıkar, kasiyer onay verirse eklenir.\n• Engelle: Stok yetersizse sisteme eklenmesine izin verilmez, eksi stok oluşması tamamen engellenir.",
            Location = new Point(20, 148),
            Size = new Size(630, 38),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(100, 116, 139)
        };

        grpSales.Controls.AddRange(new Control[] {
            _chkCalculateChange, lblChangeHelp,
            lblStockAction, _cmbStockAction, lblStockHelp
        });

        // Telegram Cloud Backup Group
        var grpTelegram = new GroupBox
        {
            Text = "  Telegram Otomatik Bulut Yedekleme  ",
            Font = Theme.TitleFont,
            Location = new Point(20, 455),
            Size = new Size(675, 235),
            BackColor = Theme.SurfaceBg
        };

        _chkTelegramEnabled = new CheckBox
        {
            Text = "Otomatik Telegram Bulut Yedeklemeyi Etkinleştir (İnternet Varsa Gönderir)",
            Location = new Point(20, 28),
            Size = new Size(620, 26),
            Font = Theme.BoldFont,
            ForeColor = Theme.PrimaryBlueDark
        };

        var lblToken = new Label { Text = "Bot Token:", Location = new Point(20, 65), Size = new Size(130, 25), Font = Theme.BoldFont };
        _txtTelegramToken = new TextBox { Location = new Point(160, 62), Size = new Size(480, 28), Font = Theme.RegularFont, PlaceholderText = "Örn: 123456789:ABCdefGhIJKlmNoPQRstuVWXyz..." };

        var lblChatId = new Label { Text = "Chat ID:", Location = new Point(20, 103), Size = new Size(130, 25), Font = Theme.BoldFont };
        _txtTelegramChatId = new TextBox { Location = new Point(160, 100), Size = new Size(480, 28), Font = Theme.RegularFont, PlaceholderText = "Örn: 987654321 veya @kanal_adi" };

        var lblPeriod = new Label { Text = "Yedekleme Sıklığı:", Location = new Point(20, 141), Size = new Size(140, 25), Font = Theme.BoldFont };
        _cmbTelegramPeriod = new ComboBox
        {
            Location = new Point(160, 138),
            Size = new Size(220, 28),
            Font = Theme.RegularFont,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbTelegramPeriod.Items.AddRange(new object[] {
            "Her 30 Dakikada Bir",
            "Her 1 Saatte Bir",
            "Her 2 Saatte Bir",
            "Her 4 Saatte Bir",
            "Günde 1 Kez (Gün Sonu)"
        });
        _cmbTelegramPeriod.SelectedIndex = 1;

        _btnTestTelegram = new Button
        {
            Text = "HEMEN BULUTA YEDEKLE & TEST ET",
            Location = new Point(395, 136),
            Size = new Size(245, 33)
        };
        Theme.ApplyModernButton(_btnTestTelegram, Theme.PurpleAccent, Color.White, 9.5F, true);
        _btnTestTelegram.Click += OnTestTelegramClick;

        _lblTelegramStatus = new Label
        {
            Text = "İpucu: İnternet bağlantısı yoksa sistem kasiyeri rahatsız etmeden bekler; internet geldiğinde yedeği gönderir.",
            Location = new Point(20, 180),
            Size = new Size(620, 42),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(100, 116, 139)
        };

        grpTelegram.Controls.AddRange(new Control[] {
            _chkTelegramEnabled, lblToken, _txtTelegramToken, lblChatId, _txtTelegramChatId,
            lblPeriod, _cmbTelegramPeriod, _btnTestTelegram, _lblTelegramStatus
        });

        // Local Database Group
        var grpDb = new GroupBox
        {
            Text = "  Yerel Veritabanı ve Ses Ayarları  ",
            Font = Theme.TitleFont,
            Location = new Point(20, 705),
            Size = new Size(675, 140),
            BackColor = Theme.SurfaceBg
        };

        _chkSound = new CheckBox
        {
            Text = "Kasa ve Barkod Bip Sesleri Açık",
            Location = new Point(20, 30),
            Size = new Size(300, 26),
            Font = Theme.RegularFont,
            Checked = true
        };

        _lblDbInfo = new Label
        {
            Text = "Veritabanı: market_pos.db (SQLite + config.json)",
            Location = new Point(20, 62),
            Size = new Size(620, 25),
            Font = Theme.RegularFont,
            ForeColor = Color.FromArgb(71, 85, 105)
        };

        var btnBackup = new Button
        {
            Text = "VERİTABANINI YEDEKLENMİŞ OLARAK MASAÜSTÜNE KAYDET",
            Location = new Point(20, 92),
            Size = new Size(460, 35)
        };
        Theme.ApplyModernButton(btnBackup, Theme.PrimaryBlue, Color.White, 9.5F, true);
        btnBackup.Click += OnBackupClick;

        grpDb.Controls.AddRange(new Control[] { _chkSound, _lblDbInfo, btnBackup });

        pnlContent.Controls.AddRange(new Control[] { grpMarket, grpSales, grpTelegram, grpDb });

        this.Controls.Add(pnlContent);
        this.Controls.Add(pnlBottom);
        this.Controls.Add(pnlHeader);

        this.FormClosing += (s, e) => SaveCurrentSettings(silent: true);
    }

    private void WireAutoSaveEvents()
    {
        _txtMarketName.TextChanged += (s, e) => AutoSaveField();
        _txtPhone.TextChanged += (s, e) => AutoSaveField();
        _txtAddress.TextChanged += (s, e) => AutoSaveField();
        _txtReceiptHeader.TextChanged += (s, e) => AutoSaveField();
        _txtReceiptFooter.TextChanged += (s, e) => AutoSaveField();
        _chkCalculateChange.CheckedChanged += (s, e) => AutoSaveField();
        _cmbStockAction.SelectedIndexChanged += (s, e) => AutoSaveField();
        _txtTelegramToken.TextChanged += (s, e) => AutoSaveField();
        _txtTelegramChatId.TextChanged += (s, e) => AutoSaveField();
        _chkTelegramEnabled.CheckedChanged += (s, e) => AutoSaveField();
        _cmbTelegramPeriod.SelectedIndexChanged += (s, e) => AutoSaveField();
        _chkSound.CheckedChanged += (s, e) =>
        {
            SoundService.SoundEnabled = _chkSound.Checked;
            AutoSaveField();
        };
    }

    private void AutoSaveField()
    {
        if (_isLoading) return;
        SaveCurrentSettings(silent: true);
    }

    private void LoadSettings()
    {
        _txtMarketName.Text = _settingsRepo.Get("MarketName", "ÖRNEK MARKET & ŞARKÜTERİ");
        _txtPhone.Text = _settingsRepo.Get("Phone", "");
        _txtAddress.Text = _settingsRepo.Get("Address", "");
        _txtReceiptHeader.Text = _settingsRepo.Get("ReceiptHeader", "HOŞGELDİNİZ");
        _txtReceiptFooter.Text = _settingsRepo.Get("ReceiptFooter", "BİZİ TERCİH ETTİĞİNİZ İÇİN TEŞEKKÜR EDERİZ");
        _chkCalculateChange.Checked = _settingsRepo.Get("CalculateChange", "1") != "0";

        string stockAction = _settingsRepo.Get("StockExceedAction", "warn_confirm");
        _cmbStockAction.SelectedIndex = stockAction == "block" ? 1 : 0;

        _chkSound.Checked = SoundService.SoundEnabled;

        // Telegram settings
        _chkTelegramEnabled.Checked = _settingsRepo.Get("TelegramEnabled", "0") == "1";
        _txtTelegramToken.Text = _settingsRepo.Get("TelegramBotToken", "");
        _txtTelegramChatId.Text = _settingsRepo.Get("TelegramChatId", "");

        int minutes = int.TryParse(_settingsRepo.Get("TelegramPeriodMinutes", "60"), out int m) ? m : 60;
        _cmbTelegramPeriod.SelectedIndex = minutes switch
        {
            30 => 0,
            60 => 1,
            120 => 2,
            240 => 3,
            1440 => 4,
            _ => 1
        };

        string lastBackup = _settingsRepo.Get("TelegramLastBackupTime", "");
        if (!string.IsNullOrEmpty(lastBackup))
        {
            _lblTelegramStatus.Text = $"Son başarılı Telegram bulut yedeği: {lastBackup}";
            _lblTelegramStatus.ForeColor = Theme.SuccessGreenDark;
        }

        string dbPath = DatabaseContext.DatabasePath;
        if (File.Exists(dbPath))
        {
            var fi = new FileInfo(dbPath);
            _lblDbInfo.Text = $"Veritabanı: {fi.Name} ({fi.Length / 1024.0:N1} KB) + config.json";
        }
    }

    private void SaveCurrentSettings(bool silent = true)
    {
        _settingsRepo.Set("MarketName", _txtMarketName.Text.Trim());
        _settingsRepo.Set("Phone", _txtPhone.Text.Trim());
        _settingsRepo.Set("Address", _txtAddress.Text.Trim());
        _settingsRepo.Set("ReceiptHeader", _txtReceiptHeader.Text.Trim());
        _settingsRepo.Set("ReceiptFooter", _txtReceiptFooter.Text.Trim());
        _settingsRepo.Set("CalculateChange", _chkCalculateChange.Checked ? "1" : "0");
        _settingsRepo.Set("StockExceedAction", _cmbStockAction.SelectedIndex == 1 ? "block" : "warn_confirm");

        SoundService.SoundEnabled = _chkSound.Checked;

        // Telegram
        _settingsRepo.Set("TelegramEnabled", _chkTelegramEnabled.Checked ? "1" : "0");
        _settingsRepo.Set("TelegramBotToken", _txtTelegramToken.Text.Trim());
        _settingsRepo.Set("TelegramChatId", _txtTelegramChatId.Text.Trim());

        int period = _cmbTelegramPeriod.SelectedIndex switch
        {
            0 => 30,
            1 => 60,
            2 => 120,
            3 => 240,
            4 => 1440,
            _ => 60
        };
        _settingsRepo.Set("TelegramPeriodMinutes", period.ToString());

        if (!silent)
        {
            SoundService.PlaySuccessBeep();
            MessageBox.Show("Tüm ayarlar config.json ve SQLite veritabanına başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void OnTestTelegramClick(object? sender, EventArgs e)
    {
        // First, automatically save settings so nothing is lost!
        SaveCurrentSettings(silent: true);

        string token = _txtTelegramToken.Text.Trim();
        string chatId = _txtTelegramChatId.Text.Trim();

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId))
        {
            MessageBox.Show("Lütfen önce Telegram Bot Token ve Chat ID alanlarını doldurunuz!", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnTestTelegram.Enabled = false;
        _btnTestTelegram.Text = "GÖNDERİLİYOR...";

        try
        {
            var service = new TelegramBackupService();
            bool result = await service.SendBackupAsync(token, chatId, isManual: true);

            if (result)
            {
                SoundService.PlaySuccessBeep();
                _lblTelegramStatus.Text = $"Son başarılı Telegram bulut yedeği: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
                _lblTelegramStatus.ForeColor = Theme.SuccessGreenDark;

                MessageBox.Show(
                    "TEBRİKLER!\n\nVeritabanı yedeğiniz Telegram botunuza başarıyla iletildi.\nBilgileriniz config.json dosyasına kalıcı olarak kaydedildi.",
                    "Yedekleme Başarılı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            SoundService.PlayErrorBeep();
            MessageBox.Show($"Telegram gönderme hatası:\n\n{ex.Message}", "Yedekleme Başarısız", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnTestTelegram.Enabled = true;
            _btnTestTelegram.Text = "HEMEN BULUTA YEDEKLE & TEST ET";
        }
    }

    private void OnBackupClick(object? sender, EventArgs e)
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "SQLite Veritabanı (*.db)|*.db",
            FileName = $"market_pos_yedek_{DateTime.Now:yyyyMMdd_HHmmss}.db",
            Title = "Veritabanı Yedeğini Kaydedeceğiniz Konumu Seçin"
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                SettingsRepository.BackupDatabase(sfd.FileName);
                SoundService.PlaySuccessBeep();
                MessageBox.Show($"Veritabanı başarıyla yedeklendi:\n{sfd.FileName}", "Yedekleme Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SoundService.PlayErrorBeep();
                MessageBox.Show($"Yedekleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
