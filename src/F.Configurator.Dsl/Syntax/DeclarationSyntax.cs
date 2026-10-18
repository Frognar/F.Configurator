namespace F.Configurator.Dsl.Syntax;

public sealed record FileSyntax(IReadOnlyList<DeclarationSyntax> Declarations);

public abstract record DeclarationSyntax(string Name, SourceSpan Span);

public sealed record CatalogSyntax(string Name, IReadOnlyList<PropertySyntax> Properties, SourceSpan Span)
    : DeclarationSyntax(Name, Span);

public sealed record FeatureSyntax(
    string Name,
    string Type,
    IReadOnlyList<PropertySyntax> Properties,
    IReadOnlyList<AttributeSyntax> Attributes,
    IReadOnlyList<string> OptionFiles,
    IReadOnlyList<OptionSyntax> Options,
    SourceSpan Span) : DeclarationSyntax(Name, Span);

public sealed record ComputedSyntax(string Name, ExpressionSyntax Expression, SourceSpan Span)
    : DeclarationSyntax(Name, Span);

public sealed record TableSyntax(
    string Name,
    string? File,
    IReadOnlyList<string> Keys,
    IReadOnlyList<string> Values,
    string? Allowed,
    IReadOnlyList<TableRowSyntax> Rows,
    SourceSpan Span) : DeclarationSyntax(Name, Span);

public sealed record ContextSyntax(
    string Name,
    IReadOnlyList<AttributeSyntax> Attributes,
    string? ValuesFile,
    SourceSpan Span) : DeclarationSyntax(Name, Span);

public sealed record PropertySyntax(string Name, IReadOnlyList<ExpressionSyntax> Values, SourceSpan Span);

public sealed record AttributeSyntax(string Name, string Type, ExpressionSyntax? Default, SourceSpan Span);

public sealed record OptionSyntax(string Id, string? Label, IReadOnlyList<PropertySyntax> Properties, SourceSpan Span);

// One list of values per cell; an empty cell matches any value.
public sealed record TableRowSyntax(IReadOnlyList<IReadOnlyList<ExpressionSyntax>> Cells, SourceSpan Span);

public sealed record CollectionSyntax(
    string Name,
    IReadOnlyList<PropertySyntax> Properties,
    IReadOnlyList<StageSyntax> Stages,
    IReadOnlyList<CollectionFeatureSyntax> Features,
    IReadOnlyList<ComponentSyntax> Components,
    IReadOnlyList<string> Shared,
    IReadOnlyList<string> Own,
    ExpressionSyntax? Index,
    IReadOnlyList<LayerSyntax>? Visualization,
    IReadOnlyList<DeclarationSyntax> Declarations,
    SourceSpan Span) : DeclarationSyntax(Name, Span);

public sealed record StageSyntax(string Title, IReadOnlyList<CollectionFeatureSyntax> Features, SourceSpan Span);

// `ZmianaPolozeniaZamka widoczna gdy ZamekDolny ≠ BRAK`
public sealed record CollectionFeatureSyntax(string Name, ExpressionSyntax? VisibleWhen, SourceSpan Span);

// `warstwa "basic/{Model}.png" gdy …` with its `tekstura` and `odbij gdy`
public sealed record LayerSyntax(
    string Image,
    ExpressionSyntax? When,
    ExpressionSyntax? Texture,
    ExpressionSyntax? MirrorWhen,
    SourceSpan Span);

// `komponent Czynne : Basic`
public sealed record ComponentSyntax(string Name, string Collection, SourceSpan Span);

public sealed record RuleSyntax(
    string Name,
    IReadOnlyList<PropertySyntax> Properties,
    ExpressionSyntax? Condition,
    IReadOnlyList<EffectSyntax> Effects,
    ReactionSyntax? Reaction,
    SourceSpan Span) : DeclarationSyntax(Name, Span);

// `ukryj A, B`, `tylko Norma [PL]`, `tylko Model z DozwoloneModel`, `maks SzerokoscMM = 1044`, `błąd "…"`
public sealed record EffectSyntax(
    string Kind,
    IReadOnlyList<ExpressionSyntax> Targets,
    ExpressionSyntax? Value,
    string? Table,
    SourceSpan Span);

// `przy naruszeniu koryguj "…"`
public sealed record ReactionSyntax(string Kind, string Message, SourceSpan Span);

public sealed record TestSyntax(
    string Name,
    string? Collection,
    IReadOnlyList<AssignmentSyntax> Data,
    IReadOnlyList<ExpectationSyntax> Expectations,
    SourceSpan Span) : DeclarationSyntax(Name, Span);

// `Model = STANDARD_01` after `dane`
public sealed record AssignmentSyntax(ExpressionSyntax Target, ExpressionSyntax Value, SourceSpan Span);

// `SzerokoscMM = 844`, `indeks zawiera "…"`, `niedostępne Kierunek [L]`, `komunikat MaxSzer`, `cena = 600` after `oczekuj`
public sealed record ExpectationSyntax(string Kind, ExpressionSyntax? Target, ExpressionSyntax? Value, SourceSpan Span);

public sealed record LinkSyntax(
    string Name,
    string From,
    string To,
    ExpressionSyntax? ProposeWhen,
    IReadOnlyList<TakeoverSyntax> Takeovers,
    SourceSpan Span) : DeclarationSyntax(Name, Span);

// `przejmij Kierunek, SzerokoscMM jako zablokowane`; `As` is `domyślne`, `zablokowane` or the name of a feature.
public sealed record TakeoverSyntax(IReadOnlyList<ExpressionSyntax> Features, string As, SourceSpan Span);

public sealed record PriceListSyntax(
    string Name,
    string? Extends,
    bool IsDefault,
    IReadOnlyList<PropertySyntax> Properties,
    BaseSyntax? Base,
    IReadOnlyList<string> OptionFiles,
    IReadOnlyList<SurchargeSyntax> Surcharges,
    IReadOnlyList<string> Disabled,
    IReadOnlyList<OverrideSyntax> Overrides,
    IReadOnlyList<AdjustmentSyntax> Adjustments,
    IReadOnlyList<DeclarationSyntax> Declarations,
    SourceSpan Span) : DeclarationSyntax(Name, Span);

// `baza z "dane/ceny.csv" klucz Model, Kolor.GrupaCenowa`, or `baza` with its key and rows below
public sealed record BaseSyntax(
    string? File,
    IReadOnlyList<ExpressionSyntax> Keys,
    IReadOnlyList<TableRowSyntax> Rows,
    SourceSpan Span);

// `dopłata W5 "węższe do 5 mm"` with its `gdy` and `kwota`
public sealed record SurchargeSyntax(
    string Id,
    string Label,
    ExpressionSyntax? Condition,
    ExpressionSyntax Amount,
    SourceSpan Span);

// `nadpisz Ramiak kwota 30`
public sealed record OverrideSyntax(string Id, ExpressionSyntax Amount, SourceSpan Span);

// `rabat 5%`, `narzut 2.5%`, `zaokrąglij 1`, applied in the order written (G8)
public sealed record AdjustmentSyntax(string Kind, decimal Value, SourceSpan Span);

// `przypisz Promocja do kontrahenta "13-DOORS4U"`; the name is the price list's.
public sealed record PriceListAssignmentSyntax(
    string Name,
    IReadOnlyList<string> Customers,
    string? CustomersFile,
    SourceSpan Span) : DeclarationSyntax(Name, Span);
