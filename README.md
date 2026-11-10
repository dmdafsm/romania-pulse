# Romania Pulse

A small web portal that shows the pulse of the Romanian economy — growth, jobs, prices, trade, public finances and business activity — from official public data, explained in plain language.

- **Data:** Eurostat (which republishes INS and BNR statistics in a harmonised form) and the BNR daily reference exchange rate. See [docs/DATA_SOURCES.md](docs/DATA_SOURCES.md) for every dataset and for sources evaluated but not yet used (INS Tempo, data.gov.ro, World Bank).
- **Stack:** ASP.NET Core Razor Pages (.NET 10), htmx, Beer CSS (Material Design 3), Chart.js. No database, no front-end build step.

## Run it

```bash
dotnet run --project src/RomaniaPulse.Web
```

Then open http://localhost:5080. The first start fetches every indicator once; results are cached in memory and under `src/RomaniaPulse.Web/App_Data/` for 12 hours, and the last good copy is served (and labelled) if a source is down.

### With Docker

```bash
docker run --rm -p 8080:8080 ghcr.io/dmdafsm/romania-pulse:latest
```

Then open http://localhost:8080. The image is built and published by [.github/workflows/docker.yml](.github/workflows/docker.yml) on every push to `main`. Mount a volume at `/app/App_Data` to keep the data cache across restarts.

## How it works

```
Catalog/IndicatorCatalog.cs   what the portal shows (23 indicators in 6 sections)
        │
Services/IndicatorService     memory cache → disk snapshot → upstream, with stale fallback
        │
Sources/EurostatSource        JSON-stat 2.0  ─┐
Sources/BnrFxSource           BNR XML        ─┴─ pure parsers, tested against fixtures
        │
Services/IndicatorShaper      ranges, shares, deltas, sparklines (pure)
        │
Pages/ + Pages/Fragments/     Razor pages; htmx swaps server-rendered fragments
```

Adding an indicator from an existing source is one entry in `IndicatorCatalog` plus a row in `docs/DATA_SOURCES.md`.

## Contributing

Read [docs/ENGINEERING.md](docs/ENGINEERING.md) and [CONTRIBUTING.md](CONTRIBUTING.md). In short: warnings are errors, `dotnet format` and `dotnet test` must pass, tests never touch the network, and no number is ever invented.

## Licence

Code: [MIT](LICENSE). Data belongs to its publishers and is reused under their terms with attribution (Eurostat: © European Union; BNR: National Bank of Romania).
