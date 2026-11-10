using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using RomaniaPulse.Web.Catalog;
using RomaniaPulse.Web.Domain;
using RomaniaPulse.Web.Services;

namespace RomaniaPulse.Web.Pages.Fragments;

public sealed record TableRow(string Label, IReadOnlyList<string> Cells);

public class ChartModel(IndicatorService indicators, ILogger<ChartModel> logger) : PageModel
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web);

    public IndicatorDefinition Definition { get; private set; } = null!;

    public IndicatorData? Data { get; private set; }

    public ChartPayload? Payload { get; private set; }

    public string? PeriodLabel { get; private set; }

    public bool Failed { get; private set; }

    public int Years { get; private set; }

    public string View { get; private set; } = "chart";

    public string PayloadJson => JsonSerializer.Serialize(Payload, PayloadJsonOptions);

    public async Task<IActionResult> OnGetAsync(string id, int? years, string? view, CancellationToken cancellationToken)
    {
        if (IndicatorCatalog.Find(id) is not { } definition)
        {
            return NotFound();
        }

        Definition = definition;
        View = view == "table" ? "table" : "chart";
        Years = years is { } y && IndicatorShaper.RangesFor(definition.Frequency).Any(r => r.Years == y)
            ? y
            : IndicatorShaper.DefaultRange(definition.Frequency);

        try
        {
            Data = await indicators.GetAsync(definition, cancellationToken);
            if (definition.Chart == ChartKind.Breakdown)
            {
                (Payload, PeriodLabel) = IndicatorShaper.Breakdown(definition, Data);
            }
            else
            {
                Payload = IndicatorShaper.TimeSeries(definition, Data, Years);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or FormatException or JsonException or TaskCanceledException)
        {
            logger.LogError(ex, "Could not load indicator {Indicator}", definition.Id);
            Failed = true;
        }

        return Page();
    }

    /// <summary>Rows for the table view; newest period first for time series.</summary>
    public IEnumerable<TableRow> TableRows()
    {
        if (Payload is null)
        {
            yield break;
        }

        var indexes = Enumerable.Range(0, Payload.Labels.Count);
        if (Definition.Chart != ChartKind.Breakdown)
        {
            indexes = indexes.Reverse();
        }

        foreach (var i in indexes)
        {
            yield return new TableRow(
                Payload.Labels[i],
                [.. Payload.Datasets.Select(ds => ds.Data[i] is { } value ? IndicatorShaper.Format(Definition, value) : "–")]);
        }
    }
}
