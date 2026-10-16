using F.Configurator.Dsl.Syntax;

namespace F.Configurator.Dsl.Tests;

internal static class Declarations
{
    public static ParseResult<FileSyntax> ParseFile(string source) => Parser.ParseFile(Parsing.File, source);

    // Each declaration written as nested lists, keyword first, like the expression trees in Parsing:
    // `cecha Kierunek : wybór` with `opcja L "Lewe"` under it is `(cecha Kierunek wybór (opcja L "Lewe"))`.
    public static IReadOnlyList<string> Trees(string source)
    {
        var result = ParseFile(source);
        Assert.Empty(result.Diagnostics);
        return [.. result.Syntax!.Declarations.Select(Print)];
    }

    public static string Tree(string source) => Assert.Single(Trees(source));

    private static string Print(DeclarationSyntax declaration) =>
        declaration switch
        {
            CatalogSyntax catalog => Group(["katalog", Quote(catalog.Name), .. catalog.Properties.Select(Print)]),
            FeatureSyntax feature => Group(
            [
                "cecha", feature.Name, feature.Type,
                .. feature.Properties.Select(Print),
                .. feature.Attributes.Select(Print),
                .. feature.OptionFiles.Select(file => $"(opcje {Quote(file)})"),
                .. feature.Options.Select(Print),
            ]),
            ComputedSyntax computed => Group(["wyliczana", computed.Name, Parsing.Print(computed.Expression)]),
            TableSyntax table => Group(
            [
                "tabela", table.Name, Quote(table.File),
                table.Keys.Count == 0 ? null : Group(["klucz", .. table.Keys]),
                table.Values.Count == 0 ? null : Group(["wartości", .. table.Values]),
                table.Allowed is null ? null : $"(dozwolone {table.Allowed})",
                .. table.Rows.Select(Print),
            ]),
            ContextSyntax context => Group(
            [
                "kontekst", context.Name,
                .. context.Attributes.Select(Print),
                context.ValuesFile is null ? null : $"(wartości {Quote(context.ValuesFile)})",
            ]),
            _ => throw new ArgumentOutOfRangeException(nameof(declaration), declaration, null),
        };

    private static string Print(PropertySyntax property) =>
        Group([property.Name, .. property.Values.Select(Parsing.Print)]);

    private static string Print(AttributeSyntax attribute) =>
        Group(["atrybut", attribute.Name, attribute.Type, attribute.Default is null ? null : Parsing.Print(attribute.Default)]);

    private static string Print(OptionSyntax option) =>
        Group(["opcja", option.Id, Quote(option.Label), .. option.Properties.Select(Print)]);

    // A cell with one value shows the value; an empty cell or one with several values shows a list.
    private static string Print(TableRowSyntax row) =>
        Group(
        [
            "wiersz",
            .. row.Cells.Select(cell => cell.Count == 1 ? Parsing.Print(cell[0]) : $"[{string.Join(" ", cell.Select(Parsing.Print))}]"),
        ]);

    private static string Group(IEnumerable<string?> parts) => $"({string.Join(" ", parts.OfType<string>())})";

    private static string? Quote(string? text) => text is null ? null : $"\"{text}\"";
}
