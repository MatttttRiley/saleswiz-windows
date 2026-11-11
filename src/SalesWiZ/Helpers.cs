using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace SalesWiZ;

/// <summary>Small formatting helpers shared by the pages (dates, due text, colors).</summary>
public static class Helpers
{
    private static readonly TimeZoneInfo Denver =
        TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time");

    /// <summary>"2026-11-10" -> DateTime, or null.</summary>
    public static DateTime? ParseDay(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            return date;
        }
        return null;
    }

    /// <summary>"Due today", "Due tomorrow", "Overdue 3 days", "Due Nov 12" — or null.</summary>
    public static string? DueText(string? followUpDue)
    {
        var date = ParseDay(followUpDue);
        if (date is null) return null;
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Denver).Date;
        var diff = (date.Value.Date - today).Days;
        return diff switch
        {
            0 => "Due today",
            1 => "Due tomorrow",
            < 0 => $"Overdue {-diff} day{(-diff == 1 ? "" : "s")}",
            _ => $"Due {date.Value:MMM d}",
        };
    }

    /// <summary>"Nov 10, 2026" for a yyyy-MM-dd string, or the string itself.</summary>
    public static string PrettyDate(string? value)
    {
        var date = ParseDay(value);
        return date is null ? value ?? "" : date.Value.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
    }

    /// <summary>"2026-11-11 09:13:41" -> "Nov 11, 2026 at 9:13 AM", or the string itself.</summary>
    public static string PrettyDateTime(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            return date.ToString("MMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture);
        }
        return value;
    }

    /// <summary>"Replied Nov 10" for a date_replied yyyy-MM-dd string.</summary>
    public static string RepliedText(string? dateReplied)
    {
        var date = ParseDay(dateReplied);
        return date is null ? "" : $"Replied {date.Value:MMM d}";
    }

    public static SolidColorBrush BrushFromHex(string hex)
    {
        // "#RRGGBB" -> Windows.UI.Color
        hex = hex.TrimStart('#');
        var r = byte.Parse(hex[0..2], NumberStyles.HexNumber);
        var g = byte.Parse(hex[2..4], NumberStyles.HexNumber);
        var b = byte.Parse(hex[4..6], NumberStyles.HexNumber);
        return new SolidColorBrush(Color.FromArgb(0xFF, r, g, b));
    }

    public static SolidColorBrush StatusBrush(string status) =>
        BrushFromHex(LeadStatusInfo.ColorHex(status));

    public static SolidColorBrush BadgeBrush(string hex) => BrushFromHex(hex);
}
