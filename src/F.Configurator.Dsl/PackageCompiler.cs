using F.Configurator.Catalog;
using F.Configurator.Dsl.Syntax;

namespace F.Configurator.Dsl;

internal sealed partial class PackageCompiler(IReadOnlyDictionary<string, string> files)
{
    private readonly List<Diagnostic> _diagnostics = [];

    public Compilation Compile()
    {
        var parsed = files
            .Where(file => file.Key.EndsWith(".kfg", StringComparison.Ordinal))
            .OrderBy(file => file.Key, StringComparer.Ordinal)
            .Select(file => Parser.ParseFile(file.Key, file.Value))
            .ToList();
        _diagnostics.AddRange(parsed.SelectMany(result => result.Diagnostics));
        if (_diagnostics.Count > 0)
        {
            return new Compilation(null, _diagnostics);
        }

        var declarations = parsed.SelectMany(result => result.Syntax!.Declarations).ToList();
        var header = declarations.OfType<CatalogSyntax>().FirstOrDefault();
        if (header is null)
        {
            return new Compilation(null, [Error(new SourceSpan("", 1, 1, 0), "pakiet nie ma deklaracji „katalog”")]);
        }

        foreach (var other in declarations.OfType<CatalogSyntax>().Skip(1))
        {
            Report(other.Span, $"pakiet ma już katalog „{header.Name}”");
        }

        var catalog = Features(Header(header), declarations.OfType<FeatureSyntax>());
        catalog = declarations.OfType<ContextSyntax>().Aggregate(catalog, Context);
        return new Compilation(
            _diagnostics.Any(diagnostic => diagnostic.Severity == Severity.Error) ? null : catalog.Build(), _diagnostics);
    }

    private CatalogBuilder Header(CatalogSyntax header) =>
        header.Properties.Aggregate(CatalogBuilder.Create(header.Name), (catalog, property) => property.Name switch
        {
            "wersja" => Single<TextSyntax>(property, "oczekiwano tekstu w cudzysłowie") is { } version
                ? catalog.Version(version.Value)
                : catalog,
            "język" => Single<NameSyntax>(property, "oczekiwano kodu języka, np. pl") is { } language
                ? catalog.Language(language.Name)
                : catalog,
            "reakcja" => Reaction(property) is { } reaction ? catalog.DefaultReaction(reaction) : catalog,
            _ => Reported(catalog, property.Span, $"„{property.Name}” nie jest właściwością katalogu (wersja, język, reakcja)"),
        });

    private Reaction? Reaction(PropertySyntax property)
    {
        Reaction? reaction = property.Values is [NameSyntax name]
            ? name.Name switch
            {
                "koryguj" => Catalog.Reaction.Correct,
                "ostrzeż" => Catalog.Reaction.Warning,
                "błąd" => Catalog.Reaction.Error,
                _ => null,
            }
            : null;
        return reaction ?? Reported<Reaction?>(null, property.Values[0].Span, "oczekiwano reakcji (koryguj, błąd, ostrzeż)");
    }

    // Decision E5 (2026-10-14): the caller gives the values of a context, so its file is not read.
    private static CatalogBuilder Context(CatalogBuilder catalog, ContextSyntax context) =>
        catalog.Context(context.Name, builder => context.Attributes.Aggregate(
            builder, (contextBuilder, attribute) => contextBuilder.Attribute(attribute.Name, TypeOf(attribute))));

    private T? Single<T>(PropertySyntax property, string message)
        where T : ExpressionSyntax =>
        property.Values is [T value] ? value : Reported<T?>(null, property.Values[0].Span, message);

    private T Reported<T>(T result, SourceSpan span, string message)
    {
        Report(span, message);
        return result;
    }

    private void Report(SourceSpan span, string message) => _diagnostics.Add(Error(span, message));

    private void Warn(SourceSpan span, string message) => _diagnostics.Add(new Diagnostic(Severity.Warning, span, message, null));

    private static Diagnostic Error(SourceSpan span, string message) => new(Severity.Error, span, message, null);
}
