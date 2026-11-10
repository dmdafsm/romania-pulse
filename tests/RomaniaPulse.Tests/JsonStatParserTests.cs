using RomaniaPulse.Web.Catalog;
using RomaniaPulse.Web.Sources;

namespace RomaniaPulse.Tests;

public class JsonStatParserTests
{
    [Fact]
    public void Parse_SingleSeries_ReadsValuesAndSkipsUnpublishedPeriods()
    {
        using var json = Fixture.Open("eurostat-unemployment.json");

        var result = JsonStatParser.Parse(json);

        var series = Assert.Single(result.Series);
        // The fixture lists six months but October has no value yet: it must not appear as a point.
        Assert.Equal(5, series.Points.Count);
        Assert.Equal(new DateOnly(2026, 5, 1), series.Points[0].Date);
        Assert.Equal(6.7, series.Points[0].Value);
        Assert.Equal(new DateOnly(2026, 9, 1), series.Points[^1].Date);
        Assert.Equal(6.3, series.Points[^1].Value);
    }

    [Fact]
    public void Parse_ReadsSourceUpdateTime()
    {
        using var json = Fixture.Open("eurostat-unemployment.json");

        var result = JsonStatParser.Parse(json);

        Assert.Equal(new DateTimeOffset(2026, 11, 5, 23, 0, 0, TimeSpan.FromHours(1)), result.SourceUpdated);
    }

    [Fact]
    public void Parse_SeriesDimension_SplitsSeriesInSpecOrderWithSpecLabels()
    {
        using var json = Fixture.Open("eurostat-trade.json");
        SeriesSpec[] specs = [new("EXP", "Exports"), new("IMP", "Imports")];

        var result = JsonStatParser.Parse(json, "stk_flow", specs);

        Assert.Equal(["Exports", "Imports"], result.Series.Select(s => s.Label));
        Assert.Equal([8264.6, 8687.9, 9247.7, 7549.9], result.Series[0].Points.Select(p => p.Value));
        Assert.Equal([10850.7, 11643.2, 12092.3, 10685.5], result.Series[1].Points.Select(p => p.Value));
        Assert.Equal(new DateOnly(2026, 8, 1), result.Series[0].Points[^1].Date);
    }

    [Fact]
    public void Parse_SeriesDimensionWithoutSpecs_UsesSourceLabels()
    {
        using var json = Fixture.Open("eurostat-trade.json");

        var result = JsonStatParser.Parse(json, "stk_flow");

        Assert.Equal(["Imports", "Exports"], result.Series.Select(s => s.Label));
    }

    [Fact]
    public void Parse_UnpinnedDimension_FailsLoudlyInsteadOfMixingSeries()
    {
        using var json = Fixture.Open("eurostat-trade.json");

        var error = Assert.Throws<FormatException>(() => JsonStatParser.Parse(json));

        Assert.Contains("stk_flow", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_ArrayEncodedValues_SkipsNulls()
    {
        using var json = Fixture.FromText("""
            {
              "id": ["geo", "time"], "size": [1, 3],
              "dimension": {
                "geo": { "category": { "index": { "RO": 0 } } },
                "time": { "category": { "index": ["2023", "2024", "2025"] } }
              },
              "value": [1.5, null, 2.5]
            }
            """);

        var series = Assert.Single(JsonStatParser.Parse(json).Series);

        Assert.Equal([new DateOnly(2023, 1, 1), new DateOnly(2025, 1, 1)], series.Points.Select(p => p.Date));
    }
}
