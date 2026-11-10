using RomaniaPulse.Web.Catalog;
using RomaniaPulse.Web.Domain;

namespace RomaniaPulse.Web.Sources;

public sealed record SourceResult(IReadOnlyList<Series> Series, DateTimeOffset? SourceUpdated);

/// <summary>An upstream data provider. Implementations do HTTP only; parsing lives in pure parsers.</summary>
public interface IIndicatorSource
{
    bool CanHandle(SourceQuery query);

    Task<SourceResult> FetchAsync(IndicatorDefinition definition, CancellationToken cancellationToken);
}
