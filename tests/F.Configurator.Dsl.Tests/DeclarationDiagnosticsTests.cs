using F.Configurator.Dsl.Syntax;
using static F.Configurator.Dsl.Tests.Declarations;
using static F.Configurator.Dsl.Tests.Parsing;

namespace F.Configurator.Dsl.Tests;

// Grammar 7: a declaration that cannot be read gives a message in Polish that points at the place. The parser then
// skips to the next declaration, so one run shows the problems of the whole file.
public class DeclarationDiagnosticsTests
{
    private static Diagnostic Error(SourceSpan span, string message, string? hint = null) =>
        new(Severity.Error, span, message, hint);

    private static IReadOnlyList<Diagnostic> Diagnostics(string source) => ParseFile(source).Diagnostics;

    [Fact]
    public void Unknown_declaration_is_an_error()
    {
        Assert.Equal([Error(At(1, 1, 4), "oczekiwano deklaracji")], Diagnostics("ceha Model : wybór"));
    }

    [Theory]
    [InlineData("cecha tabela : liczba", 7)]
    [InlineData("wyliczana tabela = 1", 11)]
    [InlineData("tabela tabela", 8)]
    [InlineData("kontekst tabela", 10)]
    public void Keyword_is_not_a_name(string source, int column)
    {
        Assert.Equal(
            [Error(At(1, column, 6), "„tabela” to słowo kluczowe i nie może być nazwą")],
            Diagnostics(source));
    }

    [Fact]
    public void Declaration_needs_a_name()
    {
        Assert.Equal([Error(At(1, 7, 1), "oczekiwano nazwy")], Diagnostics("cecha : liczba"));
    }

    [Fact]
    public void Feature_without_a_colon_is_an_error()
    {
        Assert.Equal([Error(At(1, 13, 6), "brak „:”")], Diagnostics("cecha Model liczba"));
    }

    [Fact]
    public void Unknown_feature_type_is_an_error()
    {
        Assert.Equal(
            [Error(At(1, 15, 5), "oczekiwano typu cechy (wybór, wielokrotny wybór, tak/nie, liczba, tekst)")],
            Diagnostics("cecha Model : kolor"));
    }

    [Fact]
    public void Unknown_attribute_type_is_an_error()
    {
        const string Source = """
            cecha Model : wybór
              atrybut Seria : wybór
            """;

        Assert.Equal([Error(At(2, 19, 5), "oczekiwano typu atrybutu (tekst, liczba, tak/nie)")], Diagnostics(Source));
    }

    // A file cut off in the middle of a line, as while it is being written, gives a message, never a crash.
    [Theory]
    [InlineData("katalog", 1, 8, "oczekiwano tekstu w cudzysłowie")]
    [InlineData("cecha X :", 1, 10, "oczekiwano typu cechy (wybór, wielokrotny wybór, tak/nie, liczba, tekst)")]
    [InlineData("cecha X : wielokrotny", 1, 22, "po „wielokrotny” oczekiwano „wybór”")]
    [InlineData("cecha X : wybór\n  opcje", 2, 8, "brak „z”")]
    [InlineData("cecha X : wybór\n  opcje z", 2, 10, "oczekiwano tekstu w cudzysłowie")]
    [InlineData("cecha X : wybór\n  opcja", 2, 8, "oczekiwano identyfikatora opcji")]
    [InlineData("cecha X : wybór\n  atrybut", 2, 10, "oczekiwano nazwy")]
    [InlineData("cecha X : wybór\n  atrybut Seria", 2, 16, "brak „:”")]
    [InlineData("cecha X : wybór\n  atrybut Seria :", 2, 18, "oczekiwano typu atrybutu (tekst, liczba, tak/nie)")]
    [InlineData("cecha X : wybór\n  atrybut Seria : tekst domyślnie", 2, 34, "oczekiwano wyrażenia")]
    [InlineData("wyliczana X =", 1, 14, "oczekiwano wyrażenia")]
    [InlineData("tabela T z", 1, 11, "oczekiwano tekstu w cudzysłowie")]
    [InlineData("tabela T\n  klucz", 2, 8, "oczekiwano nazwy")]
    [InlineData("tabela T\n  klucz A,", 2, 11, "oczekiwano nazwy")]
    [InlineData("tabela T\n  dozwolone", 2, 12, "oczekiwano nazwy")]
    [InlineData("tabela T\n  | PL", 2, 7, "brak „|” na końcu wiersza tabeli")]
    [InlineData("kontekst K\n  wartości", 2, 11, "brak „z”")]
    [InlineData("kontekst K\n  wartości z", 2, 13, "oczekiwano tekstu w cudzysłowie")]
    public void Line_cut_off_is_an_error(string source, int line, int column, string message)
    {
        Assert.Equal([Error(At(line, column, 0), message)], Diagnostics(source));
    }

    [Theory]
    [InlineData("cecha X : liczba mm", 1, 18, "mm")]
    [InlineData("katalog \"Drzwi\" pl", 1, 17, "pl")]
    [InlineData("wyliczana X = 1 2", 1, 17, "2")]
    [InlineData("tabela T z \"t.csv\" X", 1, 20, "X")]
    [InlineData("cecha X : liczba\n  jednostka mm cm", 2, 16, "cm")]
    [InlineData("cecha X : wybór\n  opcja A \"A\" B", 2, 15, "B")]
    public void Text_after_a_whole_line_is_an_error(string source, int line, int column, string text)
    {
        Assert.Equal([Error(At(line, column, text.Length), $"nieoczekiwane „{text}”")], Diagnostics(source));
    }

    [Fact]
    public void Property_without_a_value_is_an_error()
    {
        const string Source = """
            cecha X : liczba
              krok
            """;

        Assert.Equal([Error(At(2, 7, 0), "po „krok” oczekiwano wartości")], Diagnostics(Source));
    }

    [Fact]
    public void Option_id_starting_with_a_digit_must_be_in_quotes()
    {
        const string Source = """
            cecha Szerokosc : wybór
              opcja 80 "80 cm"
            """;

        Assert.Equal(
            [Error(At(2, 9, 2), "identyfikator opcji zaczynający się od cyfry wpisz w cudzysłowie", "\"80\"")],
            Diagnostics(Source));
    }

    [Fact]
    public void Computed_feature_without_equals_is_an_error()
    {
        Assert.Equal([Error(At(1, 13, 11), "brak „=”")], Diagnostics("wyliczana X SzerokoscMM"));
    }

    [Fact]
    public void Table_row_without_a_closing_bar_is_an_error()
    {
        const string Source = """
            tabela T
              klucz Norma
              | PL | 80
            """;

        Assert.Equal([Error(At(3, 12, 0), "brak „|” na końcu wiersza tabeli")], Diagnostics(Source));
    }

    [Fact]
    public void Errors_in_two_declarations_are_both_reported_and_the_rest_is_read()
    {
        const string Source = """
            cecha Model wybór
              opcja STANDARD_01
            cecha Kierunek : wybór
              opcja L "Lewe"
            wyliczana X SzerokoscMM
            """;

        var result = ParseFile(Source);

        Assert.Equal([Error(At(1, 13, 5), "brak „:”"), Error(At(5, 13, 11), "brak „=”")], result.Diagnostics);
        Assert.Equal(["Kierunek"], result.Syntax!.Declarations.Select(declaration => declaration.Name));
    }

    [Fact]
    public void Lexer_errors_are_reported_and_the_file_is_still_read()
    {
        const string Source = """
            cecha X : liczba
              krok 0,5
            """;

        var result = ParseFile(Source);

        Assert.Equal([new Diagnostic(Severity.Error, At(2, 8, 3), "liczba „0,5” – użyj kropki", "0.5")], result.Diagnostics);
        Assert.IsType<FeatureSyntax>(Assert.Single(result.Syntax!.Declarations));
    }
}
