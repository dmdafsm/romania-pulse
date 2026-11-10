using RomaniaPulse.Web.Domain;

namespace RomaniaPulse.Web.Catalog;

public enum ChartKind
{
    /// <summary>Time series drawn as lines (levels, rates, indices).</summary>
    Line,

    /// <summary>Time series drawn as bars from a zero baseline (growth rates, balances).</summary>
    Bar,

    /// <summary>Latest period compared across categories (sectors, product groups).</summary>
    Breakdown,
}

public enum DeltaKind
{
    /// <summary>Difference in percentage points; for indicators that are already a rate.</summary>
    Points,

    /// <summary>Percent change; for levels and indices.</summary>
    Percent,
}

public sealed record SeriesSpec(string Code, string Label);

public abstract record SourceQuery;

/// <summary>
/// A Eurostat dissemination API query. <c>geo=RO</c> and <c>freq</c> are added automatically.
/// Every dimension except time and <see cref="SeriesDimension"/> must be pinned to one value.
/// </summary>
public sealed record EurostatQuery(
    string Dataset,
    IReadOnlyDictionary<string, string> Filters,
    string? SeriesDimension = null,
    IReadOnlyList<SeriesSpec>? Series = null) : SourceQuery;

/// <summary>Official BNR daily reference rate for one currency against the leu.</summary>
public sealed record BnrFxQuery(string Currency, int Years) : SourceQuery;

/// <summary>How a <see cref="ChartKind.Breakdown"/> chart condenses time.</summary>
/// <param name="WindowPeriods">Number of latest periods summed (4 = a full year of quarterly flows).</param>
/// <param name="ShareOfKey">If set, values are shown as a percentage of this series (which is then hidden).</param>
public sealed record BreakdownOptions(int WindowPeriods = 1, string? ShareOfKey = null);

public sealed record IndicatorDefinition
{
    public required string Id { get; init; }
    public required string Title { get; init; }

    /// <summary>Plain-language "what this is and why it matters".</summary>
    public required string Description { get; init; }

    /// <summary>Long unit description shown under the title, e.g. "% change on a year earlier".</summary>
    public required string Unit { get; init; }

    /// <summary>Short suffix appended to values, e.g. "%" or " € bn".</summary>
    public string Suffix { get; init; } = "";

    public int Decimals { get; init; } = 1;

    /// <summary>Multiplier applied to source values (0.001 turns € million into € billion).</summary>
    public double Scale { get; init; } = 1;

    public required Frequency Frequency { get; init; }
    public required string Section { get; init; }
    public required ChartKind Chart { get; init; }
    public required SourceQuery Query { get; init; }
    public BreakdownOptions? Breakdown { get; init; }

    public DeltaKind Delta { get; init; } = DeltaKind.Points;

    /// <summary>How many periods back the headline change compares to (4 = same quarter last year).</summary>
    public int CompareLag { get; init; } = 1;

    /// <summary>Shown as a tile on the overview page.</summary>
    public bool IsHeadline { get; init; }

    public required string SourceName { get; init; }
    public required Uri SourceUrl { get; init; }
}

public sealed record Section(string Slug, string Title, string Intro);
