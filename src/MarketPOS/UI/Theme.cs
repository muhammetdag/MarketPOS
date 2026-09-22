using System.Drawing.Drawing2D;

namespace MarketPOS.UI;

public static class Theme
{
    // Color Palette
    public static readonly Color HeaderBg = Color.FromArgb(15, 23, 42);        // Slate 900
    public static readonly Color HeaderText = Color.White;
    public static readonly Color AppBackground = Color.FromArgb(241, 245, 249); // Slate 100
    public static readonly Color SurfaceBg = Color.White;
    public static readonly Color BorderColor = Color.FromArgb(203, 213, 225);   // Slate 300

    public static readonly Color SuccessGreen = Color.FromArgb(16, 185, 129);  // Emerald 500
    public static readonly Color SuccessGreenDark = Color.FromArgb(5, 150, 105);
    public static readonly Color DangerRed = Color.FromArgb(239, 68, 68);       // Red 500
    public static readonly Color DangerRedDark = Color.FromArgb(220, 38, 38);
    public static readonly Color PrimaryBlue = Color.FromArgb(14, 165, 233);    // Sky 500
    public static readonly Color PrimaryBlueDark = Color.FromArgb(2, 132, 199);
    public static readonly Color AmberWarning = Color.FromArgb(245, 158, 11);   // Amber 500
    public static readonly Color PurpleAccent = Color.FromArgb(139, 92, 246);

    public static readonly Color DisplayScreenBg = Color.FromArgb(15, 23, 42);  // Night display
    public static readonly Color DisplayTotalText = Color.FromArgb(74, 222, 128); // Bright green LED
    public static readonly Color DisplayChangeText = Color.FromArgb(250, 204, 21); // Bright yellow

    // Fonts
    public static readonly Font HeaderFont = new("Segoe UI", 14F, FontStyle.Bold);
    public static readonly Font TitleFont = new("Segoe UI", 12F, FontStyle.Bold);
    public static readonly Font RegularFont = new("Segoe UI", 11F, FontStyle.Regular);
    public static readonly Font BoldFont = new("Segoe UI", 11F, FontStyle.Bold);
    public static readonly Font LargeFont = new("Segoe UI", 16F, FontStyle.Bold);
    public static readonly Font GiantDisplayFont = new("Segoe UI", 34F, FontStyle.Bold);
    public static readonly Font SubDisplayFont = new("Segoe UI", 20F, FontStyle.Bold);

    public static void ApplyModernButton(Button btn, Color bg, Color fg, float fontSize = 11F, bool isBold = true)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = bg;
        btn.ForeColor = fg;
        btn.Font = new Font("Segoe UI", fontSize, isBold ? FontStyle.Bold : FontStyle.Regular);
        btn.Cursor = Cursors.Hand;
        btn.UseVisualStyleBackColor = false;
        btn.UseMnemonic = false;
    }

    public static void ApplyModernGrid(DataGridView grid)
    {
        grid.BackgroundColor = SurfaceBg;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Color.FromArgb(226, 232, 240);
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AllowUserToResizeColumns = false;
        grid.EnableHeadersVisualStyles = false;

        // Header style
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59); // Slate 800
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.ColumnHeadersHeight = 44;

        // Row style
        grid.DefaultCellStyle.BackColor = Color.White;
        grid.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254); // Light blue selection
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
        grid.RowTemplate.Height = 42;
    }
}
