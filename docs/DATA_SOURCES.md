# Data sources

Survey of public data for Romanian macro indicators (checked November 2026).

## In use

| Source | Access | Used for | Reuse terms |
|---|---|---|---|
| **Eurostat** dissemination API | `https://ec.europa.eu/eurostat/api/dissemination/statistics/1.0/data/{dataset}?format=JSON&geo=RO&…` (JSON-stat 2.0, no key) | Almost everything: national accounts, prices, labour, trade, public finance, short-term indicators. The figures originate from INS and BNR and are harmonised across the EU. | Free reuse with attribution (Commission Decision 2011/833/EU) |
| **BNR** (National Bank of Romania) | `https://curs.bnr.ro/nbrfxrates.xml`, `https://curs.bnr.ro/files/xml/years/nbrfxrates{yyyy}.xml` (XML, no key) | Official daily EUR/RON reference rate | Free reuse with attribution |

### Eurostat datasets in the catalog

| Indicator | Dataset | Frequency |
|---|---|---|
| Real GDP growth, nominal GDP | `namq_10_gdp` | Quarterly |
| GDP per capita vs EU (PPS, EU=100) | `tec00114` | Annual |
| Gross value added by sector | `namq_10_a10` | Quarterly |
| Employment by sector | `namq_10_a10_e` | Quarterly |
| Unemployment rate | `une_rt_m` | Monthly |
| Employment rate (20–64) | `lfsi_emp_q` | Quarterly |
| Wage growth (labour cost index, wages) | `lc_lci_r2_q` | Quarterly |
| Inflation (HICP), total and by category | `prc_hicp_minr` (replaced `prc_hicp_manr` in 2026) | Monthly |
| House prices | `prc_hpi_q` | Quarterly |
| 3-month interbank rate | `irt_st_m` | Monthly |
| Exports and imports of goods | `ei_eteu27_2020_m` | Monthly |
| Exports / imports by product group (SITC) | `ext_lt_intertrd` | Annual |
| Current account balance (% of GDP) | `bop_gdp6_q` | Quarterly |
| Government debt (% of GDP) | `gov_10q_ggdebt` | Quarterly |
| Government balance (% of GDP) | `gov_10dd_edpt1` | Annual |
| Industrial production, retail trade, construction | `sts_inpr_m`, `sts_trtu_m`, `sts_copr_m` | Monthly |
| Economic sentiment | `ei_bssi_m_r2` | Monthly |

## Evaluated, not yet used

| Source | What it offers | Notes |
|---|---|---|
| **INS Tempo Online** | ~1,900 tables, including **county-level** data (GDP, wages, unemployment, population by județ) that Eurostat does not carry | Unofficial JSON API at `http://statistici.insse.ro:8077/tempo-ins/`: `GET /matrix/matrices` (list), `GET /matrix/{code}` (dimensions), `POST /pivot` (CSV data). HTTP only, undocumented, may change. Best next source: enables regional maps. |
| **data.gov.ro** | CKAN API (`/api/3/action/package_search`). Useful: ANOFM registered unemployment (monthly, by county), INS trade files, budget execution from individual institutions | Mostly XLS/CSV file drops with inconsistent layouts and irregular updates; each dataset needs its own parser. Good for niche drill-downs, poor as a backbone. |
| **World Bank** API | Long annual series, easy cross-country comparison | `https://api.worldbank.org/v2/country/ROU/indicator/{id}?format=json`. Annual only and lags by a year. |
| **Ministry of Finance** (mfinante.gov.ro) | Monthly budget execution | PDF/XLS only. |
| **ONRC** | Company registrations and dissolutions | Monthly PDF/XLS, some on data.gov.ro. |
| **ECB Data Portal**, **IMF** | Rates, forecasts | ECB API timed out and IMF blocks non-browser clients during testing; Eurostat covers the same ground. |

## Adding a source

1. Implement `IIndicatorSource` (HTTP) and a pure parser.
2. Capture a trimmed real response into `tests/RomaniaPulse.Tests/Fixtures/` and test the parser against it.
3. Add the indicators to `IndicatorCatalog` and to the tables above, with reuse terms.
