using CinemaApp.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using QRCoder;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CinemaApp.Services;

/// <summary>
/// Generates a cinema-branded PDF ticket with QR code and saves / opens it.
/// </summary>
public static class TicketPdfService
{
    // ── Colours ──────────────────────────────────────────────────────
    private static readonly XColor ColBackground = XColor.FromArgb(255, 16,  16,  24);
    private static readonly XColor ColCard       = XColor.FromArgb(255, 24,  24,  36);
    private static readonly XColor ColAccent     = XColor.FromArgb(255, 229, 9,   20);
    private static readonly XColor ColGold       = XColor.FromArgb(255, 245, 200, 66);
    private static readonly XColor ColText       = XColor.FromArgb(255, 240, 240, 255);
    private static readonly XColor ColMuted      = XColor.FromArgb(255, 140, 140, 165);
    private static readonly XColor ColWhite      = XColor.FromArgb(255, 255, 255, 255);
    private static readonly XColor ColDivider    = XColor.FromArgb(255, 50,  50,  70);

    // ── Public entry point ───────────────────────────────────────────

    /// <summary>
    /// Generates a PDF for <paramref name="ticket"/>, saves it to
    /// %USERPROFILE%\Documents\CinemaTickets\ and opens it.
    /// </summary>
    public static string Generate(Ticket ticket)
    {
        var doc  = new PdfDocument();
        doc.Info.Title   = $"Билет — {ticket.MovieTitle}";
        doc.Info.Creator = "Cinema App";

        var page = doc.AddPage();
        page.Width  = XUnit.FromMillimeter(100); // A6-ish narrow ticket
        page.Height = XUnit.FromMillimeter(200);

        using var gfx = XGraphics.FromPdfPage(page);
        DrawTicket(gfx, page, ticket);

        // Save
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "CinemaTickets");
        Directory.CreateDirectory(folder);

        var safe  = SanitizeFileName(ticket.MovieTitle);
        var stamp = ticket.PurchasedAt.ToString("yyyyMMdd_HHmmss");
        var path  = Path.Combine(folder, $"Ticket_{safe}_{ticket.SeatDisplay}_{stamp}.pdf");

        doc.Save(path);
        return path;
    }

    // ── Drawing ──────────────────────────────────────────────────────

    private static void DrawTicket(XGraphics g, PdfPage page, Ticket ticket)
    {
        double W = page.Width.Point;
        double H = page.Height.Point;

        // Background
        g.DrawRectangle(new XSolidBrush(ColBackground), 0, 0, W, H);

        // Top accent stripe
        var accentBrush = new XSolidBrush(ColAccent);
        g.DrawRectangle(accentBrush, 0, 0, W, 8);

        // ── HEADER ──
        double y = 18;

        // Cinema logo text
        var fontLogo = new XFont("Arial", 14, XFontStyle.Bold);
        g.DrawString("▶  CINEMA", fontLogo, new XSolidBrush(ColAccent),
                     new XRect(0, y, W, 20), XStringFormats.TopCenter);
        y += 22;

        var fontSub = new XFont("Arial", 7, XFontStyle.Regular);
        g.DrawString("ОНЛАЙН-БРОНИРОВАНИЕ БИЛЕТОВ", fontSub, new XSolidBrush(ColMuted),
                     new XRect(0, y, W, 12), XStringFormats.TopCenter);
        y += 18;

        // Divider
        DrawDivider(g, y, W, ColDivider);
        y += 10;

        // ── MOVIE TITLE ──
        var fontTitle = new XFont("Arial", 12, XFontStyle.Bold);
        // Wrap long title across 2 lines max
        WrapText(g, ticket.MovieTitle, fontTitle, new XSolidBrush(ColText),
                 new XRect(10, y, W - 20, 36), out double titleH);
        y += titleH + 6;

        // ── DETAILS CARD ──
        double cardY = y;
        double cardH = 84;
        DrawRoundRect(g, 8, cardY, W - 16, cardH, 6, ColCard);
        y = cardY + 10;

        DrawLabelValue(g, "ФИЛЬМ",  ticket.MovieTitle,   10, ref y, W);
        DrawLabelValue(g, "ДАТА",   ticket.SessionTime.ToString("dd MMMM yyyy"), 10, ref y, W);
        DrawLabelValue(g, "ВРЕМЯ",  ticket.SessionTime.ToString("HH:mm"),        10, ref y, W);
        DrawLabelValue(g, "ЗАЛ",    ticket.HallName,     10, ref y, W);
        DrawLabelValue(g, "МЕСТО",  ticket.SeatDisplay,  10, ref y, W);
        DrawLabelValue(g, "ТИП",    FormatSeatType(ticket.SeatType), 10, ref y, W);

        y = cardY + cardH + 10;

        // ── PRICE ──
        var fontPriceLbl = new XFont("Arial", 7, XFontStyle.Regular);
        var fontPrice    = new XFont("Arial", 20, XFontStyle.Bold);
        g.DrawString("ИТОГО", fontPriceLbl, new XSolidBrush(ColMuted),
                     new XRect(0, y, W, 14), XStringFormats.TopCenter);
        y += 12;
        g.DrawString($"{ticket.Price:N0} ₽", fontPrice, new XSolidBrush(ColGold),
                     new XRect(0, y, W, 28), XStringFormats.TopCenter);
        y += 32;

        // ── DASHED TEAR LINE ──
        DrawDashedDivider(g, y, W);
        y += 14;

        // ── QR CODE ──
        var qrSize   = 90.0;
        double qrX   = (W - qrSize) / 2;
        DrawQrCode(g, ticket.QrCode, qrX, y, qrSize);
        y += qrSize + 6;

        var fontQrHint = new XFont("Arial", 7, XFontStyle.Regular);
        g.DrawString("Предъявите QR-код на входе", fontQrHint, new XSolidBrush(ColMuted),
                     new XRect(0, y, W, 12), XStringFormats.TopCenter);
        y += 14;

        // ── TICKET ID ──
        var fontId = new XFont("Arial Narrow", 7, XFontStyle.Regular);
        g.DrawString($"# {ticket.QrCode}  ·  Куплен: {ticket.PurchasedAt:dd.MM.yyyy HH:mm}",
                     fontId, new XSolidBrush(ColMuted),
                     new XRect(0, y, W, 12), XStringFormats.TopCenter);

        // Bottom accent stripe
        g.DrawRectangle(accentBrush, 0, H - 6, W, 6);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static void DrawLabelValue(XGraphics g, string label, string value,
                                        double x, ref double y, double W)
    {
        var fontLbl = new XFont("Arial", 6.5, XFontStyle.Regular);
        var fontVal = new XFont("Arial", 9,   XFontStyle.Bold);
        double colW = (W - x * 2 - 6) / 2;

        g.DrawString(label, fontLbl, new XSolidBrush(ColMuted),
                     new XRect(x, y, colW, 12), XStringFormats.TopLeft);

        // Truncate value if too long
        var displayVal = value.Length > 26 ? value[..23] + "…" : value;
        g.DrawString(displayVal, fontVal, new XSolidBrush(ColText),
                     new XRect(x + colW + 6, y, colW, 12), XStringFormats.TopLeft);
        y += 13;
    }

    private static void DrawDivider(XGraphics g, double y, double W, XColor color)
    {
        g.DrawLine(new XPen(color, 0.5), 10, y, W - 10, y);
    }

    private static void DrawDashedDivider(XGraphics g, double y, double W)
    {
        var pen = new XPen(ColDivider, 0.7) { DashStyle = XDashStyle.Dash };
        g.DrawLine(pen, 10, y, W - 10, y);
    }

    private static void DrawRoundRect(XGraphics g, double x, double y,
                                       double w, double h, double r, XColor fill)
    {
        g.DrawRoundedRectangle(new XSolidBrush(fill), x, y, w, h, r, r);
    }

    private static void WrapText(XGraphics g, string text, XFont font, XBrush brush,
                                  XRect rect, out double usedHeight)
    {
        // Simple 2-line wrap using PdfSharpCore's built-in formatter
        var tf = new XTextFormatter(g);
        tf.DrawString(text, font, brush, rect, XStringFormats.TopLeft);
        usedHeight = font.GetHeight() * 2;
    }

    private static void DrawQrCode(XGraphics g, string data, double x, double y, double size)
    {
        try
        {
            using var qrGen  = new QRCodeGenerator();
            var qrData = qrGen.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrData);
            var pngBytes = qrCode.GetGraphic(10);

            using var ms  = new MemoryStream(pngBytes);
            var bmp  = BitmapFrame.Create(ms, BitmapCreateOptions.PreservePixelFormat,
                                          BitmapCacheOption.OnLoad);

            // Encode back to stream that XImage can read
            using var outMs  = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(bmp);
            encoder.Save(outMs);
            outMs.Position = 0;

            var ximg = XImage.FromStream(() => new MemoryStream(outMs.ToArray()));

            // White background behind QR for contrast
            g.DrawRectangle(XBrushes.White, x - 4, y - 4, size + 8, size + 8);
            g.DrawImage(ximg, x, y, size, size);
        }
        catch
        {
            // Fallback: draw placeholder box
            g.DrawRectangle(new XSolidBrush(ColCard), x, y, size, size);
            var f = new XFont("Arial", 8, XFontStyle.Regular);
            g.DrawString(data, f, new XSolidBrush(ColMuted),
                         new XRect(x, y + size / 2 - 8, size, 16), XStringFormats.TopCenter);
        }
    }

    private static string FormatSeatType(SeatType t) => t switch
    {
        SeatType.Vip      => "VIP",
        SeatType.Comfort  => "Комфорт",
        SeatType.Disabled => "Для МГН",
        _                 => "Стандарт"
    };

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Length > 40 ? name[..40] : name;
    }

    /// <summary>Open PDF with the default OS viewer.</summary>
    public static void Open(string path)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось открыть PDF:\n{ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
