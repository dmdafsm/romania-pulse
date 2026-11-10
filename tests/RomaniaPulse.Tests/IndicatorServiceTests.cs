using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

using RomaniaPulse.Web.Catalog;
using RomaniaPulse.Web.Domain;
using RomaniaPulse.Web.Services;
using RomaniaPulse.Web.Sources;

namespace RomaniaPulse.Tests;

public class IndicatorServiceTests
{
    private static readonly IndicatorDefinition Definition = IndicatorCatalog.Find("unemployment")!;

    [Fact]
    public async Task GetAsync_SecondCall_IsServedFromCache()
    {
        var source = new FakeSource();
        using var harness = new Harness(source);

        var first = await harness.Service.GetAsync(Definition, CancellationToken.None);
        var second = await harness.Service.GetAsync(Definition, CancellationToken.None);

        Assert.Equal(1, source.Calls);
        Assert.Same(first, second);
        Assert.False(first.IsStale);
    }

    [Fact]
    public async Task GetAsync_FreshSnapshot_AvoidsUpstreamCall()
    {
        var source = new FakeSource();
        using var harness = new Harness(source);
        harness.Snapshots.Saved[Definition.Id] = FakeSource.Data(harness.Clock.GetUtcNow().AddHours(-1));

        await harness.Service.GetAsync(Definition, CancellationToken.None);

        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task GetAsync_UpstreamFails_ServesOldSnapshotMarkedStale()
    {
        var source = new FakeSource { Fail = true };
        using var harness = new Harness(source);
        harness.Snapshots.Saved[Definition.Id] = FakeSource.Data(harness.Clock.GetUtcNow().AddDays(-3));

        var data = await harness.Service.GetAsync(Definition, CancellationToken.None);

        Assert.True(data.IsStale);
        Assert.Equal(6.3, data.Series[0].Points[0].Value);
    }

    [Fact]
    public async Task GetAsync_UpstreamFailsWithoutSnapshot_Throws()
    {
        using var harness = new Harness(new FakeSource { Fail = true });

        await Assert.ThrowsAsync<HttpRequestException>(() => harness.Service.GetAsync(Definition, CancellationToken.None));
    }

    [Fact]
    public async Task GetAsync_SuccessfulFetch_IsSavedAsSnapshot()
    {
        using var harness = new Harness(new FakeSource());

        await harness.Service.GetAsync(Definition, CancellationToken.None);

        Assert.True(harness.Snapshots.Saved.ContainsKey(Definition.Id));
    }

    private sealed class Harness : IDisposable
    {
        private readonly MemoryCache _cache = new(new MemoryCacheOptions());

        public Harness(IIndicatorSource source)
        {
            Service = new IndicatorService([source], _cache, Snapshots, Clock, NullLogger<IndicatorService>.Instance);
        }

        public FixedClock Clock { get; } = new();

        public MemorySnapshots Snapshots { get; } = new();

        public IndicatorService Service { get; }

        public void Dispose()
        {
            Service.Dispose();
            _cache.Dispose();
        }
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class MemorySnapshots : ISnapshotStore
    {
        public Dictionary<string, IndicatorData> Saved { get; } = [];

        public Task<IndicatorData?> ReadAsync(string id, CancellationToken cancellationToken) =>
            Task.FromResult(Saved.GetValueOrDefault(id));

        public Task WriteAsync(string id, IndicatorData data, CancellationToken cancellationToken)
        {
            Saved[id] = data;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSource : IIndicatorSource
    {
        public int Calls { get; private set; }

        public bool Fail { get; init; }

        public static IndicatorData Data(DateTimeOffset fetchedAt) =>
            new([new Series("", "", [new Observation(new DateOnly(2026, 9, 1), 6.3)])], null, fetchedAt);

        public bool CanHandle(SourceQuery query) => true;

        public Task<SourceResult> FetchAsync(IndicatorDefinition definition, CancellationToken cancellationToken)
        {
            Calls++;
            return Fail
                ? Task.FromException<SourceResult>(new HttpRequestException("upstream down"))
                : Task.FromResult(new SourceResult(Data(default).Series, null));
        }
    }
}
