using RomaniaPulse.Web.Domain;

namespace RomaniaPulse.Web.Catalog;

/// <summary>The single place that says what the portal shows. Mirrored in docs/DATA_SOURCES.md.</summary>
public static class IndicatorCatalog
{
    public static IReadOnlyList<Section> Sections { get; } =
    [
        new("growth", "Growth", "How fast the economy is growing, how big it is, and which sectors produce the value."),
        new("jobs", "Jobs & wages", "Who is working, who is looking for work, and how pay is changing."),
        new("prices", "Prices & money", "How fast prices rise, what borrowing costs, and what the leu is worth."),
        new("trade", "Trade", "What Romania sells to and buys from the rest of the world."),
        new("public-finances", "Public finances", "How much the state borrows and how much it owes."),
        new("activity", "Business activity", "Monthly signals from factories, shops and building sites, ahead of the GDP figures."),
    ];

    private static readonly SeriesSpec[] Sectors =
    [
        new("TOTAL", "Total"),
        new("B-E", "Industry"),
        new("G-I", "Trade, transport, hospitality"),
        new("O-Q", "Public admin, education, health"),
        new("M_N", "Professional & support services"),
        new("L", "Real estate"),
        new("F", "Construction"),
        new("J", "IT & communications"),
        new("A", "Agriculture"),
        new("K", "Finance & insurance"),
        new("R-U", "Arts & other services"),
    ];

    private static readonly SeriesSpec[] ProductGroups =
    [
        new("SITC7", "Machinery & vehicles"),
        new("SITC6_8", "Other manufactured goods"),
        new("SITC5", "Chemicals"),
        new("SITC0_1", "Food, drinks & tobacco"),
        new("SITC3", "Fuels & energy"),
        new("SITC2_4", "Raw materials"),
        new("SITC9", "Other"),
    ];

    private static readonly SeriesSpec[] PriceCategories =
    [
        new("CP01", "Food & soft drinks"),
        new("CP02", "Alcohol & tobacco"),
        new("CP03", "Clothing & footwear"),
        new("CP04", "Housing & utilities"),
        new("CP05", "Furnishings"),
        new("CP06", "Health"),
        new("CP07", "Transport"),
        new("CP08", "Information & communication"),
        new("CP09", "Recreation & culture"),
        new("CP10", "Education"),
        new("CP11", "Restaurants & hotels"),
        new("CP12", "Insurance & finance"),
        new("CP13", "Personal care & other"),
    ];

    public static IReadOnlyList<IndicatorDefinition> All { get; } =
    [
        // ---- Growth -------------------------------------------------------
        Eurostat("gdp-growth", "growth", "Economic growth", ChartKind.Bar, Frequency.Quarterly,
            "Real GDP, % change on the same quarter a year earlier",
            "GDP is the value of everything the country produces. \"Real\" means inflation is stripped out, so this shows whether the economy is actually producing more. Two negative quarters in a row is the usual shorthand for a recession.",
            "namq_10_gdp", F(("unit", "CLV_PCH_SM"), ("s_adj", "SCA"), ("na_item", "B1GQ"))) with
        { Suffix = "%", IsHeadline = true },

        Eurostat("gdp-nominal", "growth", "Size of the economy", ChartKind.Bar, Frequency.Quarterly,
            "GDP at current prices, € billion per quarter",
            "The total value produced in each quarter, in euros at the prices of the time. It is not adjusted for inflation or seasons, so compare a quarter with the same quarter a year before.",
            "namq_10_gdp", F(("unit", "CP_MEUR"), ("s_adj", "NSA"), ("na_item", "B1GQ"))) with
        { Suffix = " € bn", Scale = 0.001, Delta = DeltaKind.Percent, CompareLag = 4 },

        Eurostat("gdp-per-capita-eu", "growth", "Living standards vs the EU", ChartKind.Line, Frequency.Annual,
            "GDP per person in purchasing power standards, EU average = 100",
            "Output per person, adjusted for the fact that prices are lower in Romania. 100 would mean the same material living standard as the EU average; the gap to 100 is how far there is left to catch up.",
            "tec00114", F()) with
        { Decimals = 0 },

        Eurostat("gva-by-sector", "growth", "What the economy is made of", ChartKind.Breakdown, Frequency.Quarterly,
            "Share of gross value added by sector, last four quarters, %",
            "Each sector's slice of the value the economy creates. It shows what Romania actually lives on: a shrinking slice is not necessarily a shrinking sector, only one growing more slowly than the rest.",
            "namq_10_a10", F(("unit", "CP_MEUR"), ("s_adj", "NSA"), ("na_item", "B1G"), ("sinceTimePeriod", "2015-Q1")),
            "nace_r2", Sectors) with
        { Suffix = "%", Breakdown = new BreakdownOptions(WindowPeriods: 4, ShareOfKey: "TOTAL") },

        // ---- Jobs ---------------------------------------------------------
        Eurostat("unemployment", "jobs", "Unemployment", ChartKind.Line, Frequency.Monthly,
            "Unemployed people as % of the labour force, seasonally adjusted",
            "The share of people who want a job and are actively looking but do not have one. It rises when the economy weakens, usually with a delay of a few months.",
            "une_rt_m", F(("s_adj", "SA"), ("age", "TOTAL"), ("sex", "T"), ("unit", "PC_ACT"))) with
        { Suffix = "%", IsHeadline = true },

        Eurostat("employment-rate", "jobs", "Employment rate", ChartKind.Line, Frequency.Quarterly,
            "People aged 20–64 in work, % of that age group, seasonally adjusted",
            "How many working-age people actually have a job. Romania's rate is among the lowest in the EU, largely because fewer women and rural residents are in formal work.",
            "lfsi_emp_q", F(("s_adj", "SA"), ("age", "Y20-64"), ("sex", "T"), ("unit", "PC_POP"), ("indic_em", "EMP_LFS"))) with
        { Suffix = "%" },

        Eurostat("wage-growth", "jobs", "Wage growth", ChartKind.Bar, Frequency.Quarterly,
            "Hourly wage costs, % change on the same quarter a year earlier",
            "How fast pay per hour is rising across the business economy, before inflation. Compare it with inflation: if wages rise faster than prices, people can buy more.",
            "lc_lci_r2_q", F(("s_adj", "SCA"), ("unit", "PCH_SM"), ("nace_r2", "B-S"), ("lcstruct", "D11"))) with
        { Suffix = "%", IsHeadline = true },

        Eurostat("employment-by-sector", "jobs", "Where people work", ChartKind.Breakdown, Frequency.Quarterly,
            "Share of employed people by sector, last four quarters, %",
            "The split of jobs across sectors. Set against the value-added chart it reveals productivity: agriculture employs many people but produces a small slice of the value; IT is the reverse.",
            "namq_10_a10_e", F(("unit", "THS_PER"), ("s_adj", "NSA"), ("na_item", "EMP_DC"), ("sinceTimePeriod", "2015-Q1")),
            "nace_r2", Sectors) with
        { Suffix = "%", Breakdown = new BreakdownOptions(WindowPeriods: 4, ShareOfKey: "TOTAL") },

        // ---- Prices & money -----------------------------------------------
        Eurostat("inflation", "prices", "Inflation", ChartKind.Line, Frequency.Monthly,
            "Consumer prices (HICP), % change on the same month a year earlier",
            "How much more a typical basket of goods and services costs than a year ago. The central bank aims for about 2.5%; well above that, money loses value quickly and interest rates stay high.",
            "prc_hicp_minr", F(("unit", "RCH_A"), ("coicop18", "TOTAL"))) with
        { Suffix = "%", IsHeadline = true },

        Eurostat("inflation-by-category", "prices", "What is getting more expensive", ChartKind.Breakdown, Frequency.Monthly,
            "Consumer prices by category, % change on a year earlier",
            "Inflation is an average. This breaks it down so you can see whether rising prices come from food, energy and housing, or from services.",
            "prc_hicp_minr", F(("unit", "RCH_A"), ("sinceTimePeriod", "2024-01")),
            "coicop18", PriceCategories) with
        { Suffix = "%" },

        new IndicatorDefinition
        {
            Id = "eur-ron",
            Section = "prices",
            Title = "Euro exchange rate",
            Unit = "Lei per euro, official BNR daily reference rate",
            Description = "How many lei one euro costs. A higher number means a weaker leu: imports, fuel and euro-denominated rents and loans get more expensive, while exporters earn more in lei.",
            Chart = ChartKind.Line,
            Frequency = Frequency.Daily,
            Decimals = 4,
            Suffix = " lei",
            Delta = DeltaKind.Percent,
            IsHeadline = true,
            Query = new BnrFxQuery("EUR", Years: 5),
            SourceName = "BNR",
            SourceUrl = new Uri("https://www.bnr.ro/"),
        },

        Eurostat("interest-rate", "prices", "Interest rates", ChartKind.Line, Frequency.Monthly,
            "3-month interbank rate, % per year",
            "What banks charge each other for short-term loans in lei. It follows the central bank's policy rate and feeds into variable-rate loans and deposits.",
            "irt_st_m", F(("int_rt", "IRT_M3"))) with
        { Suffix = "%", Decimals = 2 },

        Eurostat("house-prices", "prices", "House prices", ChartKind.Bar, Frequency.Quarterly,
            "House price index, % change on the same quarter a year earlier",
            "How fast the prices of homes are changing. Compare with wage growth: when homes rise faster than pay, housing becomes less affordable.",
            "prc_hpi_q", F(("purchase", "TOTAL"), ("unit", "RCH_A"))) with
        { Suffix = "%" },

        // ---- Trade --------------------------------------------------------
        Eurostat("trade-goods", "trade", "Exports and imports of goods", ChartKind.Line, Frequency.Monthly,
            "Goods trade with the world, € billion per month",
            "The value of goods sold abroad and bought from abroad each month. Romania persistently imports more than it exports; the gap between the lines is the trade deficit.",
            "ei_eteu27_2020_m", F(("unit", "MIO-EUR-NSA"), ("partner", "WORLD"), ("indic", "ET-T")),
            "stk_flow", [new("EXP", "Exports"), new("IMP", "Imports")]) with
        { Suffix = " € bn", Scale = 0.001, Decimals = 2, Delta = DeltaKind.Percent, CompareLag = 12 },

        Eurostat("exports-by-product", "trade", "What Romania sells", ChartKind.Breakdown, Frequency.Annual,
            "Goods exports by product group, € billion per year",
            "The mix of goods exported. Machinery and vehicles dominate: car plants, parts and electrical equipment tie Romanian exports closely to European industry.",
            "ext_lt_intertrd", F(("partner", "WORLD"), ("indic_et", "MIO_EXP_VAL"), ("sinceTimePeriod", "2015")),
            "sitc06", ProductGroups) with
        { Suffix = " € bn", Scale = 0.001 },

        Eurostat("imports-by-product", "trade", "What Romania buys", ChartKind.Breakdown, Frequency.Annual,
            "Goods imports by product group, € billion per year",
            "The mix of goods imported. Compare with exports group by group to see where the trade deficit comes from, such as chemicals, food and energy.",
            "ext_lt_intertrd", F(("partner", "WORLD"), ("indic_et", "MIO_IMP_VAL"), ("sinceTimePeriod", "2015")),
            "sitc06", ProductGroups) with
        { Suffix = " € bn", Scale = 0.001 },

        Eurostat("current-account", "trade", "Current account balance", ChartKind.Bar, Frequency.Quarterly,
            "Current account balance, % of GDP",
            "The country's overall balance with the rest of the world: trade in goods and services plus income and transfers. A large deficit means the economy spends more than it earns and relies on foreign money to fill the gap.",
            "bop_gdp6_q", F(("bop_item", "CA"), ("stk_flow", "BAL"), ("partner", "WRL_REST"), ("s_adj", "NSA"), ("unit", "PC_GDP"))) with
        { Suffix = "%", IsHeadline = true },

        // ---- Public finances ----------------------------------------------
        Eurostat("gov-debt", "public-finances", "Government debt", ChartKind.Line, Frequency.Quarterly,
            "General government gross debt, % of GDP",
            "Everything the state owes, compared with the size of the economy. EU rules set a reference limit of 60%; beyond it, a country is expected to bring debt down.",
            "gov_10q_ggdebt", F(("unit", "PC_GDP"), ("sector", "S13"), ("na_item", "GD"))) with
        { Suffix = "%", IsHeadline = true },

        Eurostat("gov-balance", "public-finances", "Budget balance", ChartKind.Bar, Frequency.Annual,
            "General government surplus (+) or deficit (−), % of GDP",
            "The gap between what the state collects and what it spends in a year. Negative means a deficit that has to be borrowed. EU rules set a limit of −3%; Romania has been under an excessive-deficit procedure since 2020.",
            "gov_10dd_edpt1", F(("unit", "PC_GDP"), ("sector", "S13"), ("na_item", "B9"))) with
        { Suffix = "%", IsHeadline = true },

        // ---- Business activity --------------------------------------------
        Eurostat("industrial-production", "activity", "Industrial production", ChartKind.Line, Frequency.Monthly,
            "Volume index, 2021 = 100, seasonally and calendar adjusted",
            "How much factories, mines and utilities are producing. Industry is the largest single sector and most of what Romania exports, so this is an early read on the wider economy.",
            "sts_inpr_m", F(("s_adj", "SCA"), ("unit", "I21"), ("nace_r2", "B-D"), ("indic_bt", "PRD"))) with
        { Delta = DeltaKind.Percent },

        Eurostat("retail-trade", "activity", "Retail sales", ChartKind.Line, Frequency.Monthly,
            "Volume index, 2021 = 100, seasonally and calendar adjusted",
            "How much people are buying in shops, with price changes stripped out. Household consumption is the main engine of Romanian growth, so falling retail volumes are an early warning.",
            "sts_trtu_m", F(("s_adj", "SCA"), ("unit", "I21"), ("nace_r2", "G47"), ("indic_bt", "VOL_SLS"))) with
        { Delta = DeltaKind.Percent },

        Eurostat("construction", "activity", "Construction output", ChartKind.Line, Frequency.Monthly,
            "Volume index, 2021 = 100, seasonally and calendar adjusted",
            "The amount of building work done, from homes to motorways. It swings with public investment and EU-funded projects.",
            "sts_copr_m", F(("s_adj", "SCA"), ("unit", "I21"), ("nace_r2", "F"), ("indic_bt", "PRD"))) with
        { Delta = DeltaKind.Percent },

        Eurostat("sentiment", "activity", "Economic sentiment", ChartKind.Line, Frequency.Monthly,
            "Economic sentiment indicator, long-term average = 100",
            "A survey-based mood gauge that combines the confidence of businesses and consumers. Above 100 means more optimism than usual; it tends to turn before the hard data does.",
            "ei_bssi_m_r2", F(("s_adj", "SA"), ("indic", "BS-ESI-I"))) with
        { Delta = DeltaKind.Percent },
    ];

    private static readonly Dictionary<string, IndicatorDefinition> ById =
        All.ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);

    public static IndicatorDefinition? Find(string id) => ById.GetValueOrDefault(id);

    public static Section? FindSection(string slug) =>
        Sections.FirstOrDefault(s => string.Equals(s.Slug, slug, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<IndicatorDefinition> InSection(string slug) =>
        All.Where(d => string.Equals(d.Section, slug, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<IndicatorDefinition> Headlines => All.Where(d => d.IsHeadline);

    private static Dictionary<string, string> F(params (string Key, string Value)[] filters) =>
        filters.ToDictionary(f => f.Key, f => f.Value, StringComparer.Ordinal);

    private static IndicatorDefinition Eurostat(
        string id,
        string section,
        string title,
        ChartKind chart,
        Frequency frequency,
        string unit,
        string description,
        string dataset,
        Dictionary<string, string> filters,
        string? seriesDimension = null,
        IReadOnlyList<SeriesSpec>? series = null) => new()
        {
            Id = id,
            Section = section,
            Title = title,
            Chart = chart,
            Frequency = frequency,
            Unit = unit,
            Description = description,
            Query = new EurostatQuery(dataset, filters, seriesDimension, series),
            SourceName = "Eurostat",
            SourceUrl = new Uri($"https://ec.europa.eu/eurostat/databrowser/view/{dataset}/default/table?lang=en"),
        };
}
