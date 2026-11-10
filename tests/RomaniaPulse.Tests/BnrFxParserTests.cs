using RomaniaPulse.Web.Sources;

namespace RomaniaPulse.Tests;

public class BnrFxParserTests
{
    [Fact]
    public void Parse_ReturnsRatesOldestFirst()
    {
        using var xml = Fixture.Open("bnr-10days.xml");

        var points = BnrFxParser.Parse(xml, "EUR");

        Assert.Equal(10, points.Count);
        Assert.Equal(new DateOnly(2026, 10, 28), points[0].Date);
        Assert.Equal(5.3495, points[0].Value);
        Assert.Equal(new DateOnly(2026, 11, 10), points[^1].Date);
        Assert.Equal(5.3443, points[^1].Value);
    }

    [Fact]
    public void Parse_AppliesMultiplier()
    {
        using var xml = Fixture.Open("bnr-10days.xml");

        var points = BnrFxParser.Parse(xml, "HUF");

        // Quoted as 1.4736 lei per 100 forints.
        Assert.Equal(0.014736, points[^1].Value, precision: 9);
    }

    [Fact]
    public void Parse_UnknownCurrency_ReturnsNothing()
    {
        using var xml = Fixture.Open("bnr-10days.xml");

        Assert.Empty(BnrFxParser.Parse(xml, "XXX"));
    }
}
