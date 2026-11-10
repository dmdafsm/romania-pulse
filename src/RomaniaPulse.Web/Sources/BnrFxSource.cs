using System.Globalization;
using System.Net;

using RomaniaPulse.Web.Catalog;
using RomaniaPulse.Web.Domain;

namespace RomaniaPulse.Web.Sources;

public sealed class BnrFxSource(IHttpClientFactory clients, TimeProvider time) : IIndicatorSource
{
    public static readonly Uri BaseAddress = new("https://curs.bnr.ro/");

    public bool CanHandle(SourceQuery query) => query is BnrFxQuery;

    public async Task<SourceResult> FetchAsync(IndicatorDefinition definition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var query = (BnrFxQuery)definition.Query;
        var thisYear = time.GetUtcNow().Year;
        var points = new SortedDictionary<DateOnly, double>();

        for (var year = thisYear - query.Years + 1; year <= thisYear; year++)
        {
            var path = string.Create(CultureInfo.InvariantCulture, $"files/xml/years/nbrfxrates{year}.xml");
            await AddAsync(points, path, query.Currency, optional: year == thisYear, cancellationToken);
        }

        // The yearly file can lag; the 10-day file always has the latest fixings.
        await AddAsync(points, "nbrfxrates10days.xml", query.Currency, optional: true, cancellationToken);

        if (points.Count == 0)
        {
            throw new InvalidDataException($"BNR returned no {query.Currency} rates.");
        }

        var series = new Series(query.Currency, $"{query.Currency}/RON", [.. points.Select(p => new Observation(p.Key, p.Value))]);
        return new SourceResult([series], SourceUpdated: null);
    }

    private async Task AddAsync(
        SortedDictionary<DateOnly, double> points, string path, string currency, bool optional, CancellationToken cancellationToken)
    {
        using var http = clients.CreateClient(UpstreamHttp.ClientName);
        using var response = await http.GetAsync(new Uri(BaseAddress, path), cancellationToken);
        if (optional && response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        foreach (var point in BnrFxParser.Parse(body, currency))
        {
            points[point.Date] = point.Value;
        }
    }
}
