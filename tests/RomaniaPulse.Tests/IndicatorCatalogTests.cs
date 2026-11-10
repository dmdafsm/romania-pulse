using RomaniaPulse.Web.Catalog;
using RomaniaPulse.Web.Services;

namespace RomaniaPulse.Tests;

/// <summary>Guards the catalog: a bad definition should fail here, not in production.</summary>
public class IndicatorCatalogTests
{
    public static TheoryData<string> Ids => [.. IndicatorCatalog.All.Select(d => d.Id)];

    [Fact]
    public void Ids_AreUniqueAndUrlSafe()
    {
        Assert.Equal(IndicatorCatalog.All.Count, IndicatorCatalog.All.Select(d => d.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(IndicatorCatalog.All, d => Assert.Matches("^[a-z0-9-]+$", d.Id));
    }

    [Fact]
    public void EverySection_HasIndicators() =>
        Assert.All(IndicatorCatalog.Sections, s => Assert.NotEmpty(IndicatorCatalog.InSection(s.Slug)));

    [Theory]
    [MemberData(nameof(Ids))]
    public void Definition_IsComplete(string id)
    {
        var d = IndicatorCatalog.Find(id)!;

        Assert.NotNull(IndicatorCatalog.FindSection(d.Section));
        Assert.False(string.IsNullOrWhiteSpace(d.Title));
        Assert.False(string.IsNullOrWhiteSpace(d.Unit));
        Assert.True(d.Description.Length >= 60, "Every indicator needs a plain-language explanation.");
        Assert.True(d.CompareLag >= 1);
        Assert.True(d.Scale > 0);
        Assert.Equal(Uri.UriSchemeHttps, d.SourceUrl.Scheme);
        Assert.Contains(IndicatorShaper.RangesFor(d.Frequency), r => r.Years == IndicatorShaper.DefaultRange(d.Frequency));
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void Definition_ChartMatchesItsQuery(string id)
    {
        var d = IndicatorCatalog.Find(id)!;
        var eurostat = d.Query as EurostatQuery;

        if (d.Chart == ChartKind.Breakdown)
        {
            Assert.False(d.IsHeadline, "A breakdown has no single headline value.");
            Assert.NotNull(eurostat?.SeriesDimension);
            Assert.NotEmpty(eurostat.Series!);
            if (d.Breakdown?.ShareOfKey is { } shareKey)
            {
                Assert.Contains(eurostat.Series!, s => s.Code == shareKey);
            }
        }

        if (eurostat is not null)
        {
            Assert.Equal(eurostat.SeriesDimension is null, eurostat.Series is null);
            // geo and freq are added by the client; pinning them here would duplicate the parameter.
            Assert.DoesNotContain("geo", eurostat.Filters.Keys);
            Assert.DoesNotContain("freq", eurostat.Filters.Keys);
            // No more series than the validated colour slots for a multi-line chart.
            Assert.True(d.Chart == ChartKind.Breakdown || (eurostat.Series?.Count ?? 1) <= 4);
        }
    }

    [Fact]
    public void EveryEurostatDataset_IsDocumented()
    {
        var docs = File.ReadAllText(Path.Combine(Fixture.RepositoryRoot(), "docs", "DATA_SOURCES.md"));

        Assert.All(
            IndicatorCatalog.All.Select(d => d.Query).OfType<EurostatQuery>().Select(q => q.Dataset).Distinct(),
            dataset => Assert.Contains($"`{dataset}`", docs, StringComparison.Ordinal));
    }
}
