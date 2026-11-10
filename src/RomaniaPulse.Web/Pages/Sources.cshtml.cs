using Microsoft.AspNetCore.Mvc.RazorPages;

using RomaniaPulse.Web.Catalog;

namespace RomaniaPulse.Web.Pages;

public class SourcesModel : PageModel
{
    public static string SourceLabel(IndicatorDefinition indicator)
    {
        ArgumentNullException.ThrowIfNull(indicator);
        return indicator.Query is EurostatQuery eurostat
            ? $"{indicator.SourceName} · {eurostat.Dataset}"
            : indicator.SourceName;
    }

    public void OnGet()
    {
    }
}
