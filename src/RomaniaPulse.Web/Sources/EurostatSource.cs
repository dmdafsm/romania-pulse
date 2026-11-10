using System.Text;

using RomaniaPulse.Web.Catalog;
using RomaniaPulse.Web.Domain;

namespace RomaniaPulse.Web.Sources;

public sealed class EurostatSource(IHttpClientFactory clients) : IIndicatorSource
{
    public static readonly Uri BaseAddress = new("https://ec.europa.eu/eurostat/api/dissemination/statistics/1.0/data/");

    public bool CanHandle(SourceQuery query) => query is EurostatQuery;

    public async Task<SourceResult> FetchAsync(IndicatorDefinition definition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var query = (EurostatQuery)definition.Query;

        using var http = clients.CreateClient(UpstreamHttp.ClientName);
        using var response = await http.GetAsync(BuildUri(query, definition.Frequency), cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        return JsonStatParser.Parse(body, query.SeriesDimension, query.Series);
    }

    public static Uri BuildUri(EurostatQuery query, Frequency frequency)
    {
        ArgumentNullException.ThrowIfNull(query);

        var freq = frequency switch
        {
            Frequency.Annual => "A",
            Frequency.Quarterly => "Q",
            Frequency.Monthly => "M",
            _ => "D",
        };

        var url = new StringBuilder(Uri.EscapeDataString(query.Dataset));
        url.Append("?format=JSON&lang=EN&geo=RO&freq=").Append(freq);
        foreach (var (key, value) in query.Filters.OrderBy(f => f.Key, StringComparer.Ordinal))
        {
            Append(url, key, value);
        }

        if (query.SeriesDimension is not null && query.Series is not null)
        {
            foreach (var spec in query.Series)
            {
                Append(url, query.SeriesDimension, spec.Code);
            }
        }

        return new Uri(BaseAddress, url.ToString());
    }

    private static void Append(StringBuilder url, string key, string value) =>
        url.Append('&').Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(value));
}
