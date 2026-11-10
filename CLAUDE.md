# Romania Pulse

Portal showing macro trends of the Romanian economy from public data. .NET Razor Pages + htmx + Beer CSS + Chart.js.

**Read and follow [docs/ENGINEERING.md](docs/ENGINEERING.md) before changing anything.** Key points:

- Stack is fixed: no SPA frameworks, no JSON API for the UI, no front-end build step, no database without agreement.
- Never fabricate or interpolate data. Every indicator carries source, dataset code and period.
- All upstream calls go through `IndicatorService`. Tests never hit the network; use fixtures.
- New indicator = entry in `Catalog/IndicatorCatalog.cs` + row in `docs/DATA_SOURCES.md`.
- No inline scripts/handlers (strict CSP). CDN assets are version-pinned with SRI.

## Commands

- Build: `dotnet build` (warnings are errors)
- Test: `dotnet test`
- Format: `dotnet format` (CI runs `--verify-no-changes`)
- Run: `dotnet run --project src/RomaniaPulse.Web` → http://localhost:5080

## Layout

- `src/RomaniaPulse.Web/Catalog` — indicator and section definitions (the single place that says what the portal shows)
- `src/RomaniaPulse.Web/Sources` — upstream clients and pure parsers (Eurostat JSON-stat, BNR XML)
- `src/RomaniaPulse.Web/Services` — caching, snapshots, view-model shaping
- `src/RomaniaPulse.Web/Pages` — full pages; `Pages/Fragments` — htmx partials
- `tests/RomaniaPulse.Tests` — xUnit, fixtures in `Fixtures/`
