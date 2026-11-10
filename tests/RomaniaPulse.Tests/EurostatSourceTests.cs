using RomaniaPulse.Web.Catalog;
using RomaniaPulse.Web.Domain;
using RomaniaPulse.Web.Sources;

namespace RomaniaPulse.Tests;

public class EurostatSourceTests
{
    [Fact]
    public void BuildUri_PinsRomaniaAndFrequencyAndRepeatsSeriesCodes()
    {
        var query = new EurostatQuery(
            "ei_eteu27_2020_m",
            new Dictionary<string, string> { ["unit"] = "MIO-EUR-NSA", ["partner"] = "WORLD" },
            "stk_flow",
            [new("EXP", "Exports"), new("IMP", "Imports")]);

        var uri = EurostatSource.BuildUri(query, Frequency.Monthly);

        Assert.Equal(
            "https://ec.europa.eu/eurostat/api/dissemination/statistics/1.0/data/ei_eteu27_2020_m"
            + "?format=JSON&lang=EN&geo=RO&freq=M&partner=WORLD&unit=MIO-EUR-NSA&stk_flow=EXP&stk_flow=IMP",
            uri.AbsoluteUri);
    }
}
