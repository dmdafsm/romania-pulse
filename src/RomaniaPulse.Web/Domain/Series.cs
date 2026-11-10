namespace RomaniaPulse.Web.Domain;

public enum Frequency
{
    Annual,
    Quarterly,
    Monthly,
    Daily,
}

/// <summary>One data point. <see cref="Date"/> is the first day of the reference period.</summary>
public sealed record Observation(DateOnly Date, double Value);

public sealed record Series(string Key, string Label, IReadOnlyList<Observation> Points);

/// <summary>Everything the portal knows about one indicator at a point in time.</summary>
public sealed record IndicatorData(
    IReadOnlyList<Series> Series,
    DateTimeOffset? SourceUpdated,
    DateTimeOffset FetchedAt)
{
    /// <summary>True when the upstream source failed and this is the last good snapshot.</summary>
    public bool IsStale { get; init; }
}
