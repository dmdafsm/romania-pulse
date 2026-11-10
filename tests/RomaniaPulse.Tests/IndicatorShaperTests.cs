using RomaniaPulse.Web.Catalog;
using RomaniaPulse.Web.Domain;
using RomaniaPulse.Web.Services;

namespace RomaniaPulse.Tests;

public class IndicatorShaperTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 0, 0, 0, TimeSpan.Zero);

    private static IndicatorDefinition Definition(ChartKind chart = ChartKind.Line, Frequency frequency = Frequency.Quarterly) => new()
    {
        Id = "test",
        Title = "Test",
        Description = "Test",
        Unit = "Test",
        Suffix = "%",
        Frequency = frequency,
        Section = "growth",
        Chart = chart,
        Query = new BnrFxQuery("EUR", 1),
        SourceName = "Test",
        SourceUrl = new Uri("https://example.org/"),
    };

    private static Series Quarterly(string key, int startYear, params double[] values) => new(
        key,
        key,
        [.. values.Select((v, i) => new Observation(new DateOnly(startYear, 1, 1).AddMonths(3 * i), v))]);

    private static IndicatorData Data(params Series[] series) => new(series, null, Now);

    [Fact]
    public void TimeSeries_KeepsOnlyTheRequestedYears()
    {
        var data = Data(Quarterly("a", 2020, Enumerable.Range(1, 24).Select(i => (double)i).ToArray()));

        var payload = IndicatorShaper.TimeSeries(Definition(), data, years: 1);

        Assert.Equal(["Q1 2025", "Q2 2025", "Q3 2025", "Q4 2025"], payload.Labels);
        Assert.Equal([21, 22, 23, 24], payload.Datasets[0].Data);
    }

    [Fact]
    public void TimeSeries_AllYears_KeepsEverything()
    {
        var data = Data(Quarterly("a", 2020, 1, 2, 3, 4, 5, 6, 7, 8));

        var payload = IndicatorShaper.TimeSeries(Definition(), data, IndicatorShaper.AllYears);

        Assert.Equal(8, payload.Labels.Count);
    }

    [Fact]
    public void TimeSeries_MissingPeriodsStayMissing()
    {
        var data = Data(Quarterly("a", 2025, 1, 2, 3), Quarterly("b", 2025, 10, 20));

        var payload = IndicatorShaper.TimeSeries(Definition(), data, IndicatorShaper.AllYears);

        Assert.Equal([10, 20, null], payload.Datasets[1].Data);
    }

    [Fact]
    public void TimeSeries_AppliesScaleAndChartKind()
    {
        var definition = Definition(ChartKind.Bar) with { Scale = 0.001 };

        var payload = IndicatorShaper.TimeSeries(definition, Data(Quarterly("a", 2025, 96509.8)), IndicatorShaper.AllYears);

        Assert.Equal("bar", payload.Kind);
        Assert.Equal(96.51, payload.Datasets[0].Data[0]);
    }

    [Fact]
    public void Breakdown_SharesOfTotalOverWindow_SortedLargestFirst()
    {
        var definition = Definition(ChartKind.Breakdown) with { Breakdown = new BreakdownOptions(WindowPeriods: 4, ShareOfKey: "TOTAL") };
        var data = Data(
            Quarterly("TOTAL", 2025, 999, 100, 100, 100, 100),
            Quarterly("small", 2025, 999, 10, 10, 10, 10),
            Quarterly("big", 2025, 999, 40, 60, 80, 60));

        var (payload, period) = IndicatorShaper.Breakdown(definition, data);

        Assert.Equal("hbar", payload.Kind);
        Assert.Equal(["big", "small"], payload.Labels);
        Assert.Equal([60, 10], payload.Datasets[0].Data);
        Assert.Equal("Q2 2025 – Q1 2026", period);
    }

    [Fact]
    public void Breakdown_UsesLatestPeriodMostCategoriesHave()
    {
        // Only one of three categories has Q3 yet: the comparison must fall back to Q2 for all.
        var data = Data(Quarterly("a", 2026, 1, 2, 3), Quarterly("b", 2026, 5, 6), Quarterly("c", 2026, 8, 9));

        var (payload, period) = IndicatorShaper.Breakdown(Definition(ChartKind.Breakdown), data);

        Assert.Equal("Q2 2026", period);
        Assert.Equal(["c", "b", "a"], payload.Labels);
        Assert.Equal([9, 6, 2], payload.Datasets[0].Data);
    }

    [Fact]
    public void Breakdown_HighlightKey_NamesTheHighlightedCategory()
    {
        var definition = Definition(ChartKind.Breakdown) with { HighlightKey = "b" };
        var data = Data(Quarterly("a", 2026, 1), new Series("b", "Romania", [new Observation(new DateOnly(2026, 1, 1), 2)]));

        var (payload, _) = IndicatorShaper.Breakdown(definition, data);

        Assert.Equal("Romania", payload.Highlight);
    }

    [Fact]
    public void Breakdown_DropsCategoriesWithIncompleteWindow()
    {
        var definition = Definition(ChartKind.Breakdown) with { Breakdown = new BreakdownOptions(WindowPeriods: 2) };
        var gappy = new Series("gappy", "gappy", [new Observation(new DateOnly(2026, 4, 1), 50)]);
        var data = Data(Quarterly("a", 2026, 1, 2), Quarterly("b", 2026, 3, 4), gappy);

        var (payload, _) = IndicatorShaper.Breakdown(definition, data);

        Assert.Equal(["b", "a"], payload.Labels);
    }

    [Fact]
    public void Kpi_RateIndicator_ReportsChangeInPercentagePoints()
    {
        var kpi = IndicatorShaper.Kpi(Definition(), Data(Quarterly("a", 2026, 6.4, 6.3)));

        Assert.Equal("6.3%", kpi.Value);
        Assert.Equal("Q2 2026", kpi.PeriodLabel);
        Assert.Equal("−0.1 pp vs Q1 2026", kpi.Change);
    }

    [Fact]
    public void Kpi_LevelIndicator_ReportsPercentChangeAgainstCompareLag()
    {
        var definition = Definition() with { Delta = DeltaKind.Percent, CompareLag = 4, Suffix = "", Decimals = 0 };

        var kpi = IndicatorShaper.Kpi(definition, Data(Quarterly("a", 2025, 100, 0, 0, 0, 110)));

        Assert.Equal("110", kpi.Value);
        Assert.Equal("+10.0% vs Q1 2025", kpi.Change);
    }

    [Fact]
    public void Kpi_SinglePoint_HasNoChange()
    {
        var kpi = IndicatorShaper.Kpi(Definition(), Data(Quarterly("a", 2026, 1)));

        Assert.Null(kpi.Change);
        Assert.Equal("", kpi.SparklinePath);
    }

    [Fact]
    public void SparklinePath_SpansTheBoxWithHighValuesOnTop()
    {
        Assert.Equal("M2 30L60 2L118 30", IndicatorShaper.SparklinePath([0, 10, 0]));
    }

    [Fact]
    public void SparklinePath_FlatSeries_DrawsMidline()
    {
        Assert.Equal("M2 16L118 16", IndicatorShaper.SparklinePath([5, 5]));
    }

    [Fact]
    public void Format_UsesTypographicMinusAndThousandsSeparator()
    {
        var definition = Definition() with { Suffix = " € bn", Decimals = 1 };

        Assert.Equal("−1,234.5 € bn", IndicatorShaper.Format(definition, -1234.5));
    }
}
