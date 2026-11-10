using System.Collections.Concurrent;

using Microsoft.Extensions.Caching.Memory;

using RomaniaPulse.Web.Catalog;
using RomaniaPulse.Web.Domain;
using RomaniaPulse.Web.Sources;

namespace RomaniaPulse.Web.Services;

/// <summary>
/// The only door to upstream data. Serves from memory, then from a fresh disk snapshot, then from the
/// source; if the source fails it falls back to the last snapshot and marks it stale.
/// </summary>
public sealed class IndicatorService(
    IEnumerable<IIndicatorSource> sources,
    IMemoryCache cache,
    ISnapshotStore snapshots,
    TimeProvider time,
    ILogger<IndicatorService> logger) : IDisposable
{
    public static readonly TimeSpan FreshFor = TimeSpan.FromHours(12);
    public static readonly TimeSpan RetryAfterFailure = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _upstream = new(4);

    public async Task<IndicatorData> GetAsync(IndicatorDefinition definition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var key = "indicator:" + definition.Id;
        if (cache.TryGetValue(key, out IndicatorData? cached) && cached is not null)
        {
            return cached;
        }

        var gate = _locks.GetOrAdd(definition.Id, _ => new SemaphoreSlim(1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue(key, out cached) && cached is not null)
            {
                return cached;
            }

            var now = time.GetUtcNow();
            var snapshot = await snapshots.ReadAsync(definition.Id, cancellationToken);
            if (snapshot is not null && now - snapshot.FetchedAt < FreshFor)
            {
                cache.Set(key, snapshot, FreshFor - (now - snapshot.FetchedAt));
                return snapshot;
            }

            try
            {
                var fresh = await FetchAsync(definition, now, cancellationToken);
                await snapshots.WriteAsync(definition.Id, fresh, cancellationToken);
                cache.Set(key, fresh, FreshFor);
                return fresh;
            }
            catch (Exception ex) when (snapshot is not null && ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Upstream failed for {Indicator}; serving snapshot from {FetchedAt}", definition.Id, snapshot.FetchedAt);
                var stale = snapshot with { IsStale = true };
                cache.Set(key, stale, RetryAfterFailure);
                return stale;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose()
    {
        _upstream.Dispose();
        foreach (var gate in _locks.Values)
        {
            gate.Dispose();
        }
    }

    private async Task<IndicatorData> FetchAsync(IndicatorDefinition definition, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var source = sources.FirstOrDefault(s => s.CanHandle(definition.Query))
            ?? throw new InvalidOperationException($"No source can handle indicator '{definition.Id}'.");

        await _upstream.WaitAsync(cancellationToken);
        try
        {
            var result = await source.FetchAsync(definition, cancellationToken);
            if (result.Series.Count == 0)
            {
                throw new InvalidDataException($"Source returned no data for '{definition.Id}'.");
            }

            return new IndicatorData(result.Series, result.SourceUpdated, now);
        }
        finally
        {
            _upstream.Release();
        }
    }
}
