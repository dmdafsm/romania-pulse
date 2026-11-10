namespace RomaniaPulse.Web.Sources;

/// <summary>The one HttpClient configuration used for all upstream statistical APIs.</summary>
public static class UpstreamHttp
{
    public const string ClientName = "upstream";

    public static void Configure(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.Timeout = TimeSpan.FromSeconds(45);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("RomaniaPulse/0.1");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json, application/xml;q=0.9, */*;q=0.1");
    }
}
