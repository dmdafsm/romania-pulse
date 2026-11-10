# Engineering guidelines

These rules apply to every change, whether written by a person or an AI agent.
CI enforces the ones marked **[CI]**.

## Stack (fixed)

- **Server:** ASP.NET Core Razor Pages on the .NET version pinned in `global.json`.
- **Interactivity:** htmx. The server returns HTML fragments; there is no JSON API for the UI and no SPA framework.
- **Design:** Beer CSS (Material Design 3). Custom CSS lives in `wwwroot/css/site.css` and uses Beer's `--primary`, `--surface`, … tokens rather than raw colours.
- **Charts:** Chart.js, configured only in `wwwroot/js/site.js`.
- **Tests:** xUnit.

Adding a framework, a database, a build step for front-end assets, or a new language needs an issue and agreement first.

## Principles

1. **Data integrity over polish.** Never invent, interpolate, or silently patch a number. A missing value is shown as missing. If a source fails, show the last good snapshot and label it as stale.
2. **Every number is traceable.** Each indicator shows its source, dataset code, reference period and last update. Adding an indicator means adding its entry in `IndicatorCatalog` *and* its row in `docs/DATA_SOURCES.md`.
3. **Be a polite client.** All upstream calls go through `IndicatorService` (cached, de-duplicated, bounded concurrency). No per-request upstream calls, no scraping of pages that offer an API, and identify the app in `User-Agent`.
4. **Plain language.** The audience is not economists. Every indicator has a one-line "what this is / why it matters" description.
5. **Boring code.** Prefer the framework's defaults. No abstraction until there are two real users of it. A new data source implements `IIndicatorSource` and nothing else changes.
6. **Progressive enhancement.** Pages must be readable without JavaScript where reasonable; every chart has a table view.

## Code rules

- Warnings are errors; nullable reference types are on; analyzers at `latest-recommended`. Do not suppress a warning without a comment explaining why. **[CI]**
- `dotnet format --verify-no-changes` must pass. **[CI]**
- Package versions live only in `Directory.Packages.props`; lock files are committed and restore runs in locked mode. **[CI]**
- Parsing is pure and separated from HTTP: parsers take a `Stream`/`string` and return domain records, so they are testable with fixtures.
- Use `CultureInfo.InvariantCulture` for all parsing and formatting of numbers and dates.
- Async all the way; pass `CancellationToken`; never block on tasks.
- No secrets in the repo. The app needs none today; if that changes, use environment variables / user-secrets.

## Testing

- Tests never touch the network. Upstream payloads are captured (trimmed) into `tests/RomaniaPulse.Tests/Fixtures/`. **[CI]**
- Every parser and every transform (ranges, breakdown shares, deltas) has tests.
- `IndicatorCatalogTests` validates the whole catalog (unique ids, known sections, complete metadata) so a bad definition fails the build, not production.

## Front-end rules

- Third-party assets are loaded from a CDN, pinned to an exact version with a Subresource Integrity hash. Bumping a version means recomputing the hash.
- A strict Content-Security-Policy is set in `Program.cs`. No inline scripts and no inline event handlers; use htmx attributes or `site.js`.
- Charts: one y-axis per chart, zero baseline for bars, a legend when there are two or more series, colours from the `--series-N` tokens in fixed order, no status colours (red/green) for series.
- Accessibility: semantic HTML, keyboard reachable controls, colour is never the only carrier of meaning, light and dark themes both checked.

## Git & review

- `main` is always green and deployable. Work on short-lived branches and merge through pull requests.
- Small, focused commits with imperative subject lines (`Add BNR FX source`).
- A PR description says what changed, why, and how it was verified (tests and, for UI, a screenshot).
- AI-assisted changes follow the same rules and are reviewed by a human before merge.

## Data licensing

The code is MIT. The data is not ours: each source has its own reuse terms (see `docs/DATA_SOURCES.md`) and the UI must always attribute the source next to the figure.
