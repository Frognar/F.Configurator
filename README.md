# F.Configurator

An industry-agnostic product configurator. Product knowledge (features, options, rules, prices) lives in human-readable configuration that non-technical people can read and write, not in code.

The wizard guides the user through choosing product features and produces:

- a **product index**: a code that identifies the configuration,
- the **list of chosen features** with their values,
- the **price**, calculated from a price list,
- a **visualization** of the product.

> **Status:** early development. Expressions and the catalog model are done, the engine (sequential mode) is in progress. The DSL, pricing, persistence, API and UI are still ahead (see [Roadmap](#roadmap)).

## How it works

```
 .kfg files (DSL) ──▶ Catalog ──▶ Engine ──▶ Step result ──▶ Pricing
                      (features,  (configuration  (values,        (default and
                       rules,      state, rules)   symbols,        dedicated
                       tables)                     attributes)     price lists)
```

- The **catalog** describes products: features (choice, number, text, computed), options with index symbols, collections split into stages, rules and tables of allowed combinations.
- The **engine** holds an immutable configuration state. Every change returns a new state with the visible features, the available options and the reasons why other options are unavailable.
- **Pricing** is a separate layer on top of the engine: the engine knows nothing about prices, and a price list prices the step result.
- **Context** (for example, the customer) is an input declared in the catalog. Rules may use it, but the product index never depends on it.

An example catalog in the DSL (keywords are Polish and may be written with or without diacritics):

```
cecha Rama : wybór
  opcja ALU
    symbol "A"
  opcja KARBON
    symbol "K"

cecha Kolor : wybór
  opcje z "dane/kolory.csv"

kolekcja Rowery
  nazwa "Rowery szosowe"
  tryb kolejny

  etap "Rama"
    cechy Rama, Rozmiar
  etap "Wygląd"
    cechy Kolor, Naklejki

  reguła KarbonBezPolysku
    opis "Carbon frames come in matte colors only."
    gdy Rama = KARBON
    wtedy zabroń Kolor [CZERWONY_POLYSK, CZARNY_POLYSK]
```

Until the parser exists, catalogs are built in code:

```csharp
using static F.Configurator.Expressions.Expression;

var catalog = CatalogBuilder.Create("Bikes")
    .Choice("Frame", feature => feature.Option("ALU", "Aluminium").Option("CARBON", "Carbon"))
    .Choice("Color", feature => feature.Option("RED_GLOSS", "Red gloss").Option("BLACK_MATTE", "Black matte"))
    .Number("Size", feature => feature.Unit("cm"))
    .Collection("Road", collection => collection
        .Stage("Frame", "Frame", "Size")
        .Stage("Look", "Color"))
    .Rule("CarbonIsMatte", rule => rule
        .When(Equal(Reference("Frame"), Option("CARBON")))
        .Forbid("Color", "RED_GLOSS"))
    .Build();

var configuration = Configuration.Start(catalog, "Road");
var next = configuration.Set("Frame", Value.Option("CARBON")); // a new state; the previous one is unchanged
```

## Repository layout

| Project | Role | State |
|---------|------|-------|
| `F.Configurator.Expressions` | Values and expressions (logic, arithmetic, comparisons, functions, tables) | done |
| `F.Configurator.Catalog` | Catalog model and builder: features, collections, stages, rules, tables | done |
| `F.Configurator.Engine` | Engine: configuration state, visibility, rules, default values | in progress |
| `F.Configurator.StepResult` | Step result passed to pricing and the UI | skeleton |
| `F.Configurator.Pricing` | Price lists and pricing | skeleton |
| `F.Configurator.Dsl` | Parser for `.kfg` files | skeleton |
| `F.Configurator.Validation` | Catalog validation and golden tests | skeleton |
| `F.Configurator.Persistence` | Storage (EF Core, PostgreSQL first) | skeleton |

Tests live in `tests/`, one project per module, plus `F.Configurator.Architecture.Tests`, which guard two rules:

- modules depend only on the modules they are allowed to;
- core modules (`Expressions`, `Catalog`, `Engine`, `StepResult`, `Pricing`) use no I/O, no ambient clock and no ambient culture, so they are deterministic and fully unit-testable.

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet build F.Configurator.slnx
dotnet test F.Configurator.slnx
```

The first `dotnet build` in a clone sets `core.hooksPath` to `.githooks`, so the `pre-commit` hook works without extra setup.

## Conventions

- **TDD.** Every behavior gets a test before the production code is written.
- **Tests:** xUnit v3, property-based tests with CsCheck or FsCheck, no assertion libraries. Package versions are managed centrally in `Directory.Packages.props`.
- **Code style** is defined in `.editorconfig` and checked on build: warnings are errors, and a style violation fails the build. Before each commit, the hook checks whitespace in the staged `.cs` files. If anything fails, run:
  ```bash
  dotnet format F.Configurator.slnx
  ```
- **Language:** identifiers, comments and commit messages in English; user-facing texts (and DSL keywords) in Polish.

CI (GitHub Actions) runs on every pull request and every push to `main`: it builds the solution, checks formatting (`dotnet format --verify-no-changes`), runs the tests and publishes a code coverage summary.

## Roadmap

| Stage | Scope | State |
|-------|-------|-------|
| E0 | Analysis, a reference catalog written in the DSL, golden tests | ✅ |
| E1 | Repository, solution, CI, conventions | ✅ |
| E2 | Expressions | ✅ |
| E3 | Catalog and builder | ✅ |
| E4 | Engine in sequential mode, step result, product index | 🚧 |
| E5 | DSL, validator and CLI | |
| E6 | Pricing and price lists | |
| E7 | Persistence, API, reference UI, 2D visualization | |
| E8 | Components, related products, independent mode | |
| E9 | Deployment (Docker), CSV adapter, DSL documentation | |

## License

Private project with no license. All rights reserved.
