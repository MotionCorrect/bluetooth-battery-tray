using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ZephyrusKeyboardBattery;

internal static class TrayIconFactory
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Icon Create(BatteryReadResult result, AppSettings? settings = null)
    {
        settings ??= SettingsStore.Load();
        var color = StateColor(result, settings);
        var label = result.Percent is int percent
            ? percent.ToString()
            : result.Source.Contains("USB", StringComparison.OrdinalIgnoreCase) ? "USB" : "?";

        using var bitmap = new Bitmap(64, 64);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(Color.Transparent);

        // Maximize readability in the tiny Windows tray slot: use the whole icon
        // as a color-coded badge and make the percentage digits as large as possible.
        using var shadowBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
        using var fillBrush = new SolidBrush(color);
        using var borderPen = new Pen(Color.FromArgb(245, 245, 245), 3);
        var shadowRect = new Rectangle(3, 4, 58, 58);
        var badgeRect = new Rectangle(1, 1, 60, 60);
        g.FillRoundedRectangle(shadowBrush, shadowRect, 15);
        g.FillRoundedRectangle(fillBrush, badgeRect, 15);
        g.DrawRoundedRectangle(borderPen, badgeRect, 15);

        var fontSize = label.Length switch
        {
            1 => 42,
            2 => 36,
            _ => 28
        };
        using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        var textColor = color.GetBrightness() > 0.62f ? Color.FromArgb(15, 15, 15) : Color.White;
        using var textBrush = new SolidBrush(textColor);
        using var outlineBrush = new SolidBrush(textColor == Color.White ? Color.FromArgb(180, 0, 0, 0) : Color.FromArgb(130, 255, 255, 255));
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        var textRect = new RectangleF(0, label.Length >= 3 ? 3 : 1, 62, 58);
        g.DrawString(label, font, outlineBrush, new RectangleF(textRect.X + 2, textRect.Y + 2, textRect.Width, textRect.Height), format);
        g.DrawString(label, font, textBrush, textRect, format);

        if (result.Source.Contains("USB", StringComparison.OrdinalIgnoreCase))
        {
            using var boltFont = new Font("Segoe UI Symbol", 18, FontStyle.Bold, GraphicsUnit.Pixel);
            using var boltBrush = new SolidBrush(Color.White);
            using var boltOutlineBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
            g.DrawString("⚡", boltFont, boltOutlineBrush, 43, 41);
            g.DrawString("⚡", boltFont, boltBrush, 42, 40);
        }

        var handle = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(handle).Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static Color StateColor(BatteryReadResult result, AppSettings settings)
    {
        if (result.Percent is not int percent)
        {
            return Color.DimGray;
        }

        if (percent <= settings.LowThresholdPercent)
        {
            return Color.FromArgb(214, 48, 49);
        }

        if (percent <= settings.WarningThresholdPercent)
        {
            return Color.FromArgb(241, 196, 15);
        }

        return Color.FromArgb(0, 166, 90);
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using var path = RoundedPath(bounds, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
    {
        using var path = RoundedPath(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
