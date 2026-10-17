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

    private const string Effects =
        "oczekiwano skutku (ukryj, zablokuj, tylko, zabroń, min, maks, krok, długość, znaki, ustaw, domyślnie, błąd, "
        + "ostrzeż, informuj)";

    private const string Takeover = "po „jako” oczekiwano „domyślne”, „zablokowane” albo nazwy cechy";

    private const string Customers = "po „kontrahentów” oczekiwano kontrahentów w cudzysłowie albo „z” i nazwy pliku";

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
    [InlineData("kolekcja K\n  etap", 2, 7, "oczekiwano tekstu w cudzysłowie")]
    [InlineData("kolekcja K\n  cechy", 2, 8, "oczekiwano nazwy")]
    [InlineData("kolekcja K\n  cechy A widoczna", 2, 19, "po „widoczna” oczekiwano „gdy”")]
    [InlineData("kolekcja K\n  komponent C", 2, 14, "brak „:”")]
    [InlineData("kolekcja K\n  indeks", 2, 1, "pod „indeks” oczekiwano wyrażenia we wciętych wierszach")]
    [InlineData("reguła R\n  gdy", 2, 6, "oczekiwano wyrażenia")]
    [InlineData("reguła R\n  wtedy", 2, 8, Effects)]
    [InlineData("reguła R\n  wtedy ukryj A\n  przy", 3, 7, "po „przy” oczekiwano „naruszeniu”")]
    [InlineData("reguła R\n  wtedy ukryj A\n  przy naruszeniu koryguj", 3, 26, "oczekiwano tekstu w cudzysłowie")]
    [InlineData("test", 1, 5, "oczekiwano tekstu w cudzysłowie")]
    [InlineData("test \"T\"\n  kolekcja", 2, 11, "oczekiwano nazwy")]
    [InlineData("test \"T\"\n  dane A =", 2, 11, "oczekiwano wyrażenia")]
    [InlineData("test \"T\"\n  oczekuj indeks", 2, 17, "po „indeks” oczekiwano „zawiera” albo „=”")]
    [InlineData("test \"T\"\n  oczekuj niedostępne A", 2, 24, "oczekiwano wyrażenia")]
    [InlineData("kolekcja K\n  wizualizacja\n    warstwa", 3, 12, "oczekiwano tekstu w cudzysłowie")]
    [InlineData("kolekcja K\n  wizualizacja\n    warstwa \"a.png\" gdy", 3, 24, "oczekiwano wyrażenia")]
    [InlineData("kolekcja K\n  wizualizacja\n    warstwa \"a.png\"\n      odbij", 4, 12, "po „odbij” oczekiwano „gdy”")]
    [InlineData("kolekcja K\n  wizualizacja\n    warstwa \"a.png\"\n      tekstura", 4, 15, "oczekiwano nazwy")]
    [InlineData("powiązanie P\n  z", 2, 4, "oczekiwano nazwy")]
    [InlineData("powiązanie P\n  z A", 2, 6, "brak „do”")]
    [InlineData("powiązanie P\n  z A do B\n  proponuj", 3, 11, "po „proponuj” oczekiwano „gdy”")]
    [InlineData("powiązanie P\n  z A do B\n  przejmij A", 3, 13, "brak „jako”")]
    [InlineData("powiązanie P\n  z A do B\n  przejmij A jako", 3, 18, Takeover)]
    [InlineData("cennik C rozszerza", 1, 19, "oczekiwano nazwy")]
    [InlineData("cennik C\n  baza z", 2, 9, "oczekiwano tekstu w cudzysłowie")]
    [InlineData("cennik C\n  baza z \"c.csv\" klucz", 2, 23, "oczekiwano nazwy")]
    [InlineData("cennik C\n  opcje", 2, 8, "brak „z”")]
    [InlineData("cennik C\n  dopłata D", 2, 12, "oczekiwano tekstu w cudzysłowie")]
    [InlineData("cennik C\n  dopłata D \"d\"\n    kwota", 3, 10, "oczekiwano wyrażenia")]
    [InlineData("cennik C\n  rabat", 2, 8, "po „rabat” oczekiwano procentu, np. 5%")]
    [InlineData("cennik C\n  zaokrąglij", 2, 13, "po „zaokrąglij” oczekiwano liczby")]
    [InlineData("cennik C\n  nadpisz D", 2, 12, "brak „kwota”")]
    [InlineData("cennik C\n  wyłącz", 2, 9, "oczekiwano nazwy")]
    [InlineData("przypisz", 1, 9, "oczekiwano nazwy")]
    [InlineData("przypisz C", 1, 11, "brak „do”")]
    [InlineData("przypisz C do", 1, 14, "oczekiwano „kontrahenta” albo „kontrahentów”")]
    [InlineData("przypisz C do kontrahenta", 1, 26, "oczekiwano tekstu w cudzysłowie")]
    [InlineData("przypisz C do kontrahentów", 1, 27, Customers)]
    [InlineData("przypisz C do kontrahentów z", 1, 29, "oczekiwano tekstu w cudzysłowie")]
    [InlineData("przypisz C do kontrahentów [\"A\",", 1, 33, "oczekiwano tekstu w cudzysłowie")]
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

    [Fact]
    public void Unknown_effect_is_an_error()
    {
        Assert.Equal([Error(At(2, 9, 5), Effects)], Diagnostics("reguła R\n  wtedy pokaż A"));
    }

    // The problem shows only at the end of the rule; the next declaration is still read.
    [Fact]
    public void Rule_without_effects_is_an_error()
    {
        const string Source = """
            reguła R
              gdy A = B
            reguła S
              wtedy ukryj A
            """;

        var result = ParseFile(Source);

        Assert.Equal([Error(At(1, 8, 1), "reguła bez „wtedy”")], result.Diagnostics);
        Assert.Equal(["S"], result.Syntax!.Declarations.Select(declaration => declaration.Name));
    }

    [Fact]
    public void Limit_without_equals_is_an_error()
    {
        Assert.Equal([Error(At(2, 16, 1), "brak „=”")], Diagnostics("reguła R\n  wtedy maks A 5"));
    }

    [Fact]
    public void Options_effect_needs_a_list_or_a_table()
    {
        Assert.Equal(
            [Error(At(2, 21, 2), "oczekiwano listy opcji w nawiasach [ ] albo „z” i nazwy tabeli")],
            Diagnostics("reguła R\n  wtedy tylko Norma PL"));
    }

    [Fact]
    public void Unknown_reaction_is_an_error()
    {
        const string Source = """
            reguła R
              wtedy ukryj A
              przy naruszeniu popraw "x"
            """;

        Assert.Equal([Error(At(3, 19, 6), "oczekiwano reakcji (koryguj, błąd, ostrzeż)")], Diagnostics(Source));
    }

    [Fact]
    public void Index_outside_a_collection_is_an_error()
    {
        Assert.Equal([Error(At(1, 1, 6), "„indeks” może stać tylko w kolekcji")], Diagnostics("indeks\n  A + B"));
    }

    [Fact]
    public void Second_index_of_a_collection_is_an_error()
    {
        const string Source = """
            kolekcja K
              indeks
                A
              indeks
                B
            """;

        Assert.Equal([Error(At(4, 3, 6), "kolekcja ma już indeks")], Diagnostics(Source));
    }

    [Fact]
    public void Index_is_one_expression_only()
    {
        const string Source = """
            kolekcja K
              indeks
                Model + Norma
                Kolor
            """;

        Assert.Equal(
            [Error(At(4, 5, 5), "indeks to jedno wyrażenie, dalszy ciąg zacznij od „+”")],
            Diagnostics(Source));
    }

    [Fact]
    public void Unknown_line_in_a_test_is_an_error()
    {
        Assert.Equal(
            [Error(At(2, 3, 7), "oczekiwano „kolekcja”, „dane” albo „oczekuj”")],
            Diagnostics("test \"T\"\n  sprawdź A = 1"));
    }

    [Fact]
    public void Expectation_without_equals_is_an_error()
    {
        Assert.Equal([Error(At(2, 13, 1), "brak „=”")], Diagnostics("test \"T\"\n  oczekuj A 1"));
    }

    [Fact]
    public void Visualization_outside_a_collection_is_an_error()
    {
        Assert.Equal(
            [Error(At(1, 1, 12), "„wizualizacja” może stać tylko w kolekcji")],
            Diagnostics("wizualizacja\n  warstwa \"a.png\""));
    }

    [Fact]
    public void Second_visualization_of_a_collection_is_an_error()
    {
        const string Source = """
            kolekcja K
              wizualizacja
                warstwa "a.png"
              wizualizacja
                warstwa "b.png"
            """;

        Assert.Equal([Error(At(4, 3, 12), "kolekcja ma już wizualizację")], Diagnostics(Source));
    }

    [Fact]
    public void Unknown_line_in_a_visualization_is_an_error()
    {
        const string Source = """
            kolekcja K
              wizualizacja
                obraz "a.png"
            """;

        Assert.Equal([Error(At(3, 5, 5), "oczekiwano „warstwa”")], Diagnostics(Source));
    }

    [Fact]
    public void Unknown_line_in_a_layer_is_an_error()
    {
        const string Source = """
            kolekcja K
              wizualizacja
                warstwa "a.png"
                  obróć gdy A = B
            """;

        Assert.Equal([Error(At(4, 7, 5), "oczekiwano „tekstura” albo „odbij gdy”")], Diagnostics(Source));
    }

    [Fact]
    public void Link_without_its_collections_is_an_error()
    {
        Assert.Equal(
            [Error(At(1, 12, 1), "powiązanie bez „z … do …”")],
            Diagnostics("powiązanie P\n  przejmij A jako domyślne"));
    }

    [Fact]
    public void Unknown_line_in_a_link_is_an_error()
    {
        Assert.Equal(
            [Error(At(3, 3, 6), "oczekiwano „z”, „proponuj” albo „przejmij”")],
            Diagnostics("powiązanie P\n  z A do B\n  kopiuj A"));
    }

    [Fact]
    public void Surcharge_without_an_amount_is_an_error()
    {
        const string Source = """
            cennik C
              dopłata D "d"
                gdy A = B
            """;

        Assert.Equal([Error(At(2, 11, 1), "dopłata bez „kwota”")], Diagnostics(Source));
    }

    [Fact]
    public void Unknown_line_in_a_surcharge_is_an_error()
    {
        const string Source = """
            cennik C
              dopłata D "d"
                cena 5
            """;

        Assert.Equal([Error(At(3, 5, 4), "oczekiwano „gdy” albo „kwota”")], Diagnostics(Source));
    }

    [Fact]
    public void Discount_is_given_in_percent()
    {
        Assert.Equal([Error(At(2, 10, 0), "po „rabat” oczekiwano procentu, np. 5%")], Diagnostics("cennik C\n  rabat 5"));
    }

    [Fact]
    public void Unknown_line_in_a_base_price_is_an_error()
    {
        const string Source = """
            cennik C
              baza
                wartości Kwota
            """;

        Assert.Equal([Error(At(3, 5, 8), "oczekiwano „klucz” albo wiersza tabeli")], Diagnostics(Source));
    }
}
