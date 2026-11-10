using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using RomaniaPulse.Web.Catalog;

namespace RomaniaPulse.Web.Pages;

public class SectionModel : PageModel
{
    public Section Section { get; private set; } = null!;

    public IReadOnlyList<IndicatorDefinition> Indicators { get; private set; } = [];

    public IActionResult OnGet(string slug)
    {
        if (IndicatorCatalog.FindSection(slug) is not { } section)
        {
            return NotFound();
        }

        Section = section;
        Indicators = [.. IndicatorCatalog.InSection(section.Slug)];
        return Page();
    }
}
