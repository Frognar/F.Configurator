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
    IReadOnlyList<DeclarationSyntax> Declarations,
    SourceSpan Span) : DeclarationSyntax(Name, Span);

public sealed record StageSyntax(string Title, IReadOnlyList<CollectionFeatureSyntax> Features, SourceSpan Span);

// `ZmianaPolozeniaZamka widoczna gdy ZamekDolny ≠ BRAK`
public sealed record CollectionFeatureSyntax(string Name, ExpressionSyntax? VisibleWhen, SourceSpan Span);

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
