using System.Globalization;
using System.Text.Json;

using RomaniaPulse.Web.Catalog;
using RomaniaPulse.Web.Domain;

namespace RomaniaPulse.Web.Sources;

/// <summary>
/// Parses a JSON-stat 2.0 dataset (the Eurostat API format) into time series.
/// The dataset must vary only along time and, optionally, one series dimension.
/// </summary>
public static class JsonStatParser
{
    private const string TimeDimension = "time";

    public static SourceResult Parse(Stream json, string? seriesDimension = null, IReadOnlyList<SeriesSpec>? specs = null)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var ids = root.GetProperty("id").EnumerateArray().Select(e => e.GetString()!).ToArray();
        var sizes = root.GetProperty("size").EnumerateArray().Select(e => e.GetInt32()).ToArray();
        var dimensions = root.GetProperty("dimension");

        var timeIndex = Array.IndexOf(ids, TimeDimension);
        if (timeIndex < 0)
        {
            throw new FormatException("Dataset has no time dimension.");
        }

        var seriesIndex = seriesDimension is null ? -1 : Array.IndexOf(ids, seriesDimension);
        if (seriesDimension is not null && seriesIndex < 0)
        {
            throw new FormatException($"Dataset has no '{seriesDimension}' dimension.");
        }

        for (var i = 0; i < ids.Length; i++)
        {
            if (i != timeIndex && i != seriesIndex && sizes[i] > 1)
            {
                throw new FormatException(
                    $"Dimension '{ids[i]}' has {sizes[i]} values; pin it with a filter or make it the series dimension.");
            }
        }

        // Row-major layout: the last dimension varies fastest.
        var strides = new int[ids.Length];
        var stride = 1;
        for (var i = ids.Length - 1; i >= 0; i--)
        {
            strides[i] = stride;
            stride *= sizes[i];
        }

        var dates = ReadCodes(dimensions.GetProperty(TimeDimension), sizes[timeIndex])
            .Select(code => Period.TryParse(code, out var date)
                ? date
                : throw new FormatException($"Unrecognised period '{code}'."))
            .ToArray();

        var seriesCodes = seriesIndex < 0 ? [""] : ReadCodes(dimensions.GetProperty(seriesDimension!), sizes[seriesIndex]);
        var points = seriesCodes.Select(_ => new List<Observation>()).ToArray();

        foreach (var (flatIndex, value) in ReadValues(root.GetProperty("value")))
        {
            var timePos = flatIndex / strides[timeIndex] % sizes[timeIndex];
            var seriesPos = seriesIndex < 0 ? 0 : flatIndex / strides[seriesIndex] % sizes[seriesIndex];
            points[seriesPos].Add(new Observation(dates[timePos], value));
        }

        var labels = seriesIndex < 0 ? null : ReadLabels(dimensions.GetProperty(seriesDimension!));
        var series = new List<Series>();
        foreach (var (code, position) in Order(seriesCodes, specs))
        {
            if (points[position].Count == 0)
            {
                continue;
            }

            var label = specs?.FirstOrDefault(s => s.Code == code)?.Label
                ?? labels?.GetValueOrDefault(code)
                ?? code;
            series.Add(new Series(code, label, [.. points[position].OrderBy(p => p.Date)]));
        }

        return new SourceResult(series, ParseUpdated(root));
    }

    internal static DateTimeOffset? ParseUpdated(JsonElement root)
    {
        if (!root.TryGetProperty("updated", out var updated) || updated.GetString() is not { } text)
        {
            return null;
        }

        // Eurostat writes offsets as "+0100"; .NET wants "+01:00".
        if (text.Length > 5 && text[^5] is '+' or '-' && text[^3] != ':')
        {
            text = text.Insert(text.Length - 2, ":");
        }

        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
    }

    private static IEnumerable<(string Code, int Position)> Order(string[] codes, IReadOnlyList<SeriesSpec>? specs)
    {
        if (specs is null)
        {
            return codes.Select((code, position) => (code, position));
        }

        return specs
            .Select(spec => (spec.Code, Position: Array.IndexOf(codes, spec.Code)))
            .Where(x => x.Position >= 0);
    }

    private static string[] ReadCodes(JsonElement dimension, int size)
    {
        var index = dimension.GetProperty("category").GetProperty("index");
        var codes = new string[size];
        if (index.ValueKind == JsonValueKind.Array)
        {
            var i = 0;
            foreach (var item in index.EnumerateArray())
            {
                codes[i++] = item.GetString()!;
            }
        }
        else
        {
            foreach (var property in index.EnumerateObject())
            {
                codes[property.Value.GetInt32()] = property.Name;
            }
        }

        return codes;
    }

    private static Dictionary<string, string> ReadLabels(JsonElement dimension)
    {
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        if (dimension.GetProperty("category").TryGetProperty("label", out var label))
        {
            foreach (var property in label.EnumerateObject())
            {
                labels[property.Name] = property.Value.GetString() ?? property.Name;
            }
        }

        return labels;
    }

    private static IEnumerable<(int FlatIndex, double Value)> ReadValues(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            var i = 0;
            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number)
                {
                    yield return (i, item.GetDouble());
                }

                i++;
            }
        }
        else
        {
            foreach (var property in value.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Number)
                {
                    yield return (int.Parse(property.Name, CultureInfo.InvariantCulture), property.Value.GetDouble());
                }
            }
        }
    }
}
