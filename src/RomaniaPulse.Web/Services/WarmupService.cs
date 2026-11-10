using RomaniaPulse.Web.Catalog;

namespace RomaniaPulse.Web.Services;

/// <summary>Loads every indicator once at startup so the first visitor does not wait on upstream APIs.</summary>
public sealed class WarmupService(IndicatorService indicators, ILogger<WarmupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Parallel.ForEachAsync(IndicatorCatalog.All, stoppingToken, async (definition, token) =>
        {
            try
            {
                await indicators.GetAsync(definition, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Warm-up failed for {Indicator}", definition.Id);
            }
        });
    }
}
