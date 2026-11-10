using RomaniaPulse.Web.Domain;

namespace RomaniaPulse.Tests;

public class PeriodTests
{
    [Theory]
    [InlineData("2025", 2025, 1, 1)]
    [InlineData("2026-Q3", 2026, 7, 1)]
    [InlineData("2026-S1", 2026, 1, 1)]
    [InlineData("2026-S2", 2026, 7, 1)]
    [InlineData("2026-10", 2026, 10, 1)]
    [InlineData("2026-11-10", 2026, 11, 10)]
    public void TryParse_UnderstandsEveryFrequency(string text, int year, int month, int day)
    {
        Assert.True(Period.TryParse(text, out var date));
        Assert.Equal(new DateOnly(year, month, day), date);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026-Q5")]
    [InlineData("2026-S3")]
    [InlineData("2026-13")]
    [InlineData("soon")]
    public void TryParse_RejectsGarbage(string text) => Assert.False(Period.TryParse(text, out _));

    [Theory]
    [InlineData(Frequency.Annual, "2026")]
    [InlineData(Frequency.Quarterly, "Q3 2026")]
    [InlineData(Frequency.Semiannual, "H2 2026")]
    [InlineData(Frequency.Monthly, "Aug 2026")]
    [InlineData(Frequency.Daily, "5 Aug 2026")]
    public void Label_IsHumanReadable(Frequency frequency, string expected) =>
        Assert.Equal(expected, Period.Label(new DateOnly(2026, 8, 5), frequency));
}
