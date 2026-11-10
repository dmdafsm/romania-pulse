using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using RomaniaPulse.Web.Catalog;
using RomaniaPulse.Web.Services;

namespace RomaniaPulse.Web.Pages.Fragments;

public class KpiModel(IndicatorService indicators, ILogger<KpiModel> logger) : PageModel
{
    public IndicatorDefinition Definition { get; private set; } = null!;

    public KpiView? Kpi { get; private set; }

    public bool IsStale { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        if (IndicatorCatalog.Find(id) is not { } definition)
        {
            return NotFound();
        }

        Definition = definition;
        try
        {
            var data = await indicators.GetAsync(definition, cancellationToken);
            Kpi = IndicatorShaper.Kpi(definition, data);
            IsStale = data.IsStale;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or FormatException or JsonException or TaskCanceledException)
        {
            logger.LogError(ex, "Could not load indicator {Indicator}", definition.Id);
        }

        return Page();
    }
}
