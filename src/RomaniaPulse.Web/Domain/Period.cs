using System.Globalization;

namespace RomaniaPulse.Web.Domain;

/// <summary>Parses and prints statistical reference periods ("2026", "2026-Q3", "2026-10", "2026-10-31").</summary>
public static class Period
{
    public static bool TryParse(string text, out DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(text);
        date = default;
        var inv = CultureInfo.InvariantCulture;

        if (text.Length == 4 && int.TryParse(text, NumberStyles.None, inv, out var year))
        {
            date = new DateOnly(year, 1, 1);
            return true;
        }

        if (text.Length == 7 && text[4] == '-' && text[5] == 'Q'
            && int.TryParse(text.AsSpan(0, 4), NumberStyles.None, inv, out year)
            && text[6] is >= '1' and <= '4')
        {
            date = new DateOnly(year, ((text[6] - '1') * 3) + 1, 1);
            return true;
        }

        if (text.Length == 7)
        {
            return DateOnly.TryParseExact(text, "yyyy-MM", inv, DateTimeStyles.None, out date);
        }

        return DateOnly.TryParseExact(text, "yyyy-MM-dd", inv, DateTimeStyles.None, out date);
    }

    public static string Label(DateOnly date, Frequency frequency) => frequency switch
    {
        Frequency.Annual => date.Year.ToString(CultureInfo.InvariantCulture),
        Frequency.Quarterly => string.Create(CultureInfo.InvariantCulture, $"Q{((date.Month - 1) / 3) + 1} {date.Year}"),
        Frequency.Monthly => date.ToString("MMM yyyy", CultureInfo.InvariantCulture),
        _ => date.ToString("d MMM yyyy", CultureInfo.InvariantCulture),
    };
}
