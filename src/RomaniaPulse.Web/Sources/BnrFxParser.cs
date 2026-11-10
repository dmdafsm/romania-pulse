using System.Globalization;
using System.Xml;
using System.Xml.Linq;

using RomaniaPulse.Web.Domain;

namespace RomaniaPulse.Web.Sources;

/// <summary>Parses BNR reference-rate XML (<c>nbrfxrates*.xml</c>) into lei per one unit of a currency.</summary>
public static class BnrFxParser
{
    public static IReadOnlyList<Observation> Parse(Stream xml, string currency)
    {
        // BNR is a trusted source, but there is no reason to resolve DTDs or external entities.
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        using var reader = XmlReader.Create(xml, settings);
        var document = XDocument.Load(reader);

        var points = new List<Observation>();
        // The namespace changed from http:// to https:// over the years, so match on local names.
        foreach (var cube in document.Descendants().Where(e => e.Name.LocalName == "Cube"))
        {
            var rate = cube.Elements().FirstOrDefault(e =>
                e.Name.LocalName == "Rate" && (string?)e.Attribute("currency") == currency);
            if (rate is null
                || !DateOnly.TryParseExact((string?)cube.Attribute("date"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                || !double.TryParse(rate.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                continue;
            }

            var multiplier = (string?)rate.Attribute("multiplier") is { } text
                && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var m) && m > 0
                ? m
                : 1;
            points.Add(new Observation(date, value / multiplier));
        }

        return [.. points.OrderBy(p => p.Date)];
    }
}
