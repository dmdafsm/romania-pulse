using System.Globalization;
using System.Text;

using RomaniaPulse.Web.Catalog;
using RomaniaPulse.Web.Domain;

namespace RomaniaPulse.Web.Services;

public sealed record ChartDataset(string Label, IReadOnlyList<double?> Data);

/// <summary>What the browser needs to draw a chart. Serialized into a data attribute.</summary>
public sealed record ChartPayload(
    string Kind,
    IReadOnlyList<string> Labels,
    IReadOnlyList<ChartDataset> Datasets,
    string Suffix,
    int Decimals);

public sealed record RangeOption(string Label, int Years);

public sealed record KpiView(
    string Value,
    string PeriodLabel,
    string? Change,
    string SparklinePath);

/// <summary>Pure functions that turn raw series into what pages display. No I/O; fully unit tested.</summary>
public static class IndicatorShaper
{
    public const int AllYears = 0;
    public const int SparklineWidth = 120;
    public const int SparklineHeight = 32;

    public static IReadOnlyList<RangeOption> RangesFor(Frequency frequency) => frequency switch
    {
        Frequency.Daily => [new("1 year", 1), new("3 years", 3), new("All", AllYears)],
        Frequency.Annual => [new("10 years", 10), new("20 years", 20), new("All", AllYears)],
        _ => [new("5 years", 5), new("10 years", 10), new("All", AllYears)],
    };

    public static int DefaultRange(Frequency frequency) => frequency switch
    {
        Frequency.Daily => 1,
        Frequency.Annual => 20,
        _ => 10,
    };

    public static string Format(IndicatorDefinition definition, double value)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return Number(value, definition.Decimals) + definition.Suffix;
    }

    /// <summary>Time-series chart: one dataset per series, aligned on the union of their dates.</summary>
    public static ChartPayload TimeSeries(IndicatorDefinition definition, IndicatorData data, int years)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(data);

        var last = data.Series.SelectMany(s => s.Points).Max(p => p.Date);
        var from = years == AllYears ? DateOnly.MinValue : last.AddYears(-years);
        var dates = data.Series.SelectMany(s => s.Points).Select(p => p.Date)
            .Where(d => d > from).Distinct().Order().ToArray();

        var datasets = data.Series.Select(series =>
        {
            var byDate = series.Points.ToDictionary(p => p.Date, p => p.Value);
            var values = dates.Select(d => byDate.TryGetValue(d, out var v) ? Round(definition, v) : (double?)null).ToArray();
            return new ChartDataset(series.Label, values);
        }).ToArray();

        return new ChartPayload(
            definition.Chart == ChartKind.Bar ? "bar" : "line",
            [.. dates.Select(d => Period.Label(d, definition.Frequency))],
            datasets,
            definition.Suffix,
            definition.Decimals);
    }

    /// <summary>Category comparison for the latest period (or window of periods) with broad coverage.</summary>
    public static (ChartPayload Payload, string PeriodLabel) Breakdown(IndicatorDefinition definition, IndicatorData data)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(data);
        var options = definition.Breakdown ?? new BreakdownOptions();

        // The newest period is often published for the total only; use the newest one most categories have.
        var needed = (data.Series.Count + 1) / 2;
        var allDates = data.Series.SelectMany(s => s.Points).Select(p => p.Date).Distinct().Order().ToArray();
        var endIndex = Array.FindLastIndex(allDates, d => data.Series.Count(s => s.Points.Any(p => p.Date == d)) >= needed);
        if (endIndex < 0)
        {
            throw new InvalidDataException($"No period with enough categories for '{definition.Id}'.");
        }

        var window = allDates[Math.Max(0, endIndex - options.WindowPeriods + 1)..(endIndex + 1)];

        // A category missing any period of the window is left out rather than shown as an undercount.
        var totals = data.Series
            .Select(s => (Series: s, Values: s.Points.Where(p => window.Contains(p.Date)).ToArray()))
            .Where(x => x.Values.Length == window.Length)
            .ToDictionary(x => x.Series.Key, x => (x.Series.Label, Sum: x.Values.Sum(p => p.Value)), StringComparer.Ordinal);

        IEnumerable<(string Label, double Value)> items;
        if (options.ShareOfKey is { } shareKey)
        {
            if (!totals.TryGetValue(shareKey, out var whole) || whole.Sum == 0)
            {
                throw new InvalidDataException($"Share base '{shareKey}' is missing for '{definition.Id}'.");
            }

            items = totals.Where(t => t.Key != shareKey).Select(t => (t.Value.Label, t.Value.Sum / whole.Sum * 100));
        }
        else
        {
            items = totals.Select(t => (t.Value.Label, t.Value.Sum * definition.Scale));
        }

        var ordered = items.OrderByDescending(i => i.Value).ToArray();
        var payload = new ChartPayload(
            "hbar",
            [.. ordered.Select(i => i.Label)],
            [new ChartDataset(definition.Title, [.. ordered.Select(i => (double?)Math.Round(i.Value, definition.Decimals))])],
            definition.Suffix,
            definition.Decimals);

        var first = Period.Label(window[0], definition.Frequency);
        var final = Period.Label(window[^1], definition.Frequency);
        return (payload, window.Length == 1 ? final : $"{first} – {final}");
    }

    /// <summary>Headline tile: latest value, change against an earlier period, and a sparkline.</summary>
    public static KpiView Kpi(IndicatorDefinition definition, IndicatorData data)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(data);

        var points = data.Series[0].Points;
        var latest = points[^1];
        string? change = null;
        if (points.Count > definition.CompareLag)
        {
            var earlier = points[^(definition.CompareLag + 1)];
            var against = Period.Label(earlier.Date, definition.Frequency);
            if (definition.Delta == DeltaKind.Points)
            {
                change = $"{Signed((latest.Value - earlier.Value) * definition.Scale, definition.Decimals)} pp vs {against}";
            }
            else if (earlier.Value != 0)
            {
                change = $"{Signed(((latest.Value / earlier.Value) - 1) * 100, 1)}% vs {against}";
            }
        }

        var tail = definition.Frequency switch
        {
            Frequency.Daily => 250,
            Frequency.Monthly => 60,
            Frequency.Quarterly => 40,
            _ => 20,
        };

        return new KpiView(
            Format(definition, latest.Value * definition.Scale),
            Period.Label(latest.Date, definition.Frequency),
            change,
            SparklinePath([.. points.Skip(Math.Max(0, points.Count - tail)).Select(p => p.Value)]));
    }

    /// <summary>SVG path for a sparkline in a <see cref="SparklineWidth"/>×<see cref="SparklineHeight"/> viewBox.</summary>
    public static string SparklinePath(IReadOnlyList<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count < 2)
        {
            return "";
        }

        const double Pad = 2;
        var min = values.Min();
        var max = values.Max();
        var span = max - min;
        var path = new StringBuilder();
        for (var i = 0; i < values.Count; i++)
        {
            var x = Pad + (i * (SparklineWidth - (2 * Pad)) / (values.Count - 1));
            var y = span == 0
                ? SparklineHeight / 2.0
                : SparklineHeight - Pad - ((values[i] - min) / span * (SparklineHeight - (2 * Pad)));
            path.Append(i == 0 ? 'M' : 'L')
                .Append(x.ToString("0.#", CultureInfo.InvariantCulture))
                .Append(' ')
                .Append(y.ToString("0.#", CultureInfo.InvariantCulture));
        }

        return path.ToString();
    }

    private static double Round(IndicatorDefinition definition, double value) =>
        Math.Round(value * definition.Scale, Math.Max(definition.Decimals, 2));

    private static string Number(double value, int decimals) =>
        value.ToString("N" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)
            .Replace('-', '−');

    private static string Signed(double value, int decimals)
    {
        var rounded = Math.Round(value, decimals);
        return (rounded > 0 ? "+" : "") + Number(rounded, decimals);
    }
}
