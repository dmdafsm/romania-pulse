# Contributing

Thanks for helping. Please read [docs/ENGINEERING.md](docs/ENGINEERING.md) first: it is short and CI enforces most of it.

```bash
dotnet restore
dotnet build
dotnet test
dotnet format            # before committing
dotnet run --project src/RomaniaPulse.Web
```

- Open an issue before large changes or new dependencies.
- New indicator? Add it to `IndicatorCatalog`, document it in [docs/DATA_SOURCES.md](docs/DATA_SOURCES.md), and make sure `dotnet test` passes.
- Found a wrong number? Open an issue with the indicator, the period, and a link to the source figure.
