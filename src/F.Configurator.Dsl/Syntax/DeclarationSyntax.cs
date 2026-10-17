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
